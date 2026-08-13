using Dapper;
using GenesisV2.Services.PropertySearch;
using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.Results.Section78;
using V2_Genesis.Services;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Omission;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Services.Implementations;

public class PropertySearchService : IPropertySearchService
{
    private readonly IConfiguration _config;
    private readonly ILogger<PropertySearchService> _logger;

    // Used only for shared township and scheme procedures.
    private readonly string _defaultConn;

    private const string SP_TOWNSHIPS =
        "Objection.dbo.propertyDetailsTown";

    private const string SP_SCHEMES =
        "Objection.dbo.propertyDetailsScheme";

    private const string SP_LINK_PROPERTY =
        "InsertLinkedProperty";

    public PropertySearchService(
        IConfiguration config,
        ILogger<PropertySearchService> logger)
    {
        _config = config;
        _logger = logger;

        _defaultConn =
            config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is missing from configuration.");
    }

    // ─────────────────────────────────────────────────────────────
    // Connection handling
    // ─────────────────────────────────────────────────────────────

    private string GetRollConnection(
        RollSearchConfig config)
    {
        return _config.GetConnectionString(
                   config.ConnectionKey)
               ?? _defaultConn;
    }

    private static bool IsQueryRoll(
        string rollSource,
        RollSearchConfig config)
    {
        return config.IsQuery ||
               rollSource.Equals(
                   "Query",
                   StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────────────────────
    // Shared search lists
    // ─────────────────────────────────────────────────────────────

    public async Task<List<string>> GetTownshipsAsync(string? rollSource = null)
    {
        var connectionString = _defaultConn;
        var townshipProcedure = SP_TOWNSHIPS;

        // GV, LIS and all callers without a supplementary roll use the
        // complete township list. Supplementary rolls must only display
        // townships available in that roll's own database.
        if (!string.IsNullOrWhiteSpace(rollSource) &&
            rollSource.StartsWith(
                "Objection_Supp",
                StringComparison.OrdinalIgnoreCase) &&
            OmissionRollRegistry.Build().TryGetValue(
                rollSource,
                out var supplementaryRoll))
        {
            connectionString =
                _config.GetConnectionString(supplementaryRoll.ConnectionKey)
                ?? throw new InvalidOperationException(
                    $"Connection string '{supplementaryRoll.ConnectionKey}' is missing.");

            townshipProcedure = supplementaryRoll.TownSp;
        }

        await using var conn =
            new SqlConnection(connectionString);

        var rows = await conn.QueryAsync<string>(
            townshipProcedure,
            commandType: CommandType.StoredProcedure,
            commandTimeout: 60);

        return rows
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    public async Task<List<string>> GetSchemesAsync()
    {
        await using var conn =
            new SqlConnection(_defaultConn);

        var rows = await conn.QueryAsync<string>(
            SP_SCHEMES,
            commandType: CommandType.StoredProcedure,
            commandTimeout: 60);

        return rows
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    // ─────────────────────────────────────────────────────────────
    // Property search
    // ─────────────────────────────────────────────────────────────

    public async Task<List<PropertySearchResult>> SearchAsync(
        string rollSource,
        PropertySearchParams searchParams,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rollSource))
            return new List<PropertySearchResult>();

        if (!RollSearchRegistry.Configs.TryGetValue(
                rollSource,
                out var config))
        {
            _logger.LogWarning(
                "Unknown property-search roll source: {RollSource}",
                rollSource);

            return new List<PropertySearchResult>();
        }

        ArgumentNullException.ThrowIfNull(searchParams);

        var storedProcedure =
            ResolveSp(config, searchParams);

        var parameters =
            BuildParams(searchParams);

        var connectionString =
            GetRollConnection(config);

        try
        {
            await using var conn =
                new SqlConnection(connectionString);

            var results = await conn.QueryAsync<PropertySearchResult>(
                new CommandDefinition(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 15,
                    cancellationToken: cancellationToken));

            /*
             * For the Query roll, the search stored procedure must
             * return Review_Close_Date.
             *
             * Dapper maps it directly to:
             * PropertySearchResult.Review_Close_Date
             */
            return results.ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Property search failed. Roll: {RollSource}, SP: {StoredProcedure}",
                rollSource,
                storedProcedure);

            throw new ApplicationException(
                $"Property search failed for roll '{rollSource}'.",
                ex);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Property details
    // ─────────────────────────────────────────────────────────────

    public async Task<List<PropertyDetailResult>>
        GetPropertyDetailsAsync(
            string rollSource,
            string unitKey,
            string valuationKey,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rollSource))
            return new List<PropertyDetailResult>();

        if (!RollSearchRegistry.Configs.TryGetValue(
                rollSource,
                out var config))
        {
            _logger.LogWarning(
                "Unknown property-detail roll source: {RollSource}",
                rollSource);

            return new List<PropertyDetailResult>();
        }

        if (string.IsNullOrWhiteSpace(unitKey) &&
            string.IsNullOrWhiteSpace(valuationKey))
        {
            return new List<PropertyDetailResult>();
        }

        var connectionString =
            GetRollConnection(config);

        try
        {
            await using var conn =
                new SqlConnection(connectionString);

            var results = await conn.QueryAsync<PropertyDetailResult>(
                new CommandDefinition(
                    config.DetailSp,
                    new
                    {
                        UnitKey = unitKey,
                        ValuationKey = valuationKey
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 15,
                    cancellationToken: cancellationToken));

            /*
             * For the Query roll, the detail stored procedure must
             * return Review_Close_Date.
             *
             * Dapper maps it directly to:
             * PropertyDetailResult.Review_Close_Date
             */
            return DeduplicatePropertyDetails(
                results.ToList());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Property-detail lookup failed. Roll: {RollSource}, UnitKey: {UnitKey}, ValuationKey: {ValuationKey}",
                rollSource,
                unitKey,
                valuationKey);

            throw new ApplicationException(
                $"Property details could not be loaded for roll '{rollSource}'.",
                ex);
        }
    }

    private static List<PropertyDetailResult>
        DeduplicatePropertyDetails(
            List<PropertyDetailResult> rows)
    {
        return rows
            .GroupBy(x => new
            {
                UnitKey =
                    Clean(x.UnitKey),

                ValuationKey =
                    Clean(x.ValuationKey),

                PropertyId =
                    Clean(x.PropertyId),

                Category =
                    Clean(x.CatDesc),

                Extent =
                    Clean(x.RateableArea),

                MarketValue =
                    CleanMoney(x.MarketValue),

                WefDate =
                    Clean(x.WefDate),

                /*
                 * Preserve separate records when the review
                 * closing date differs.
                 */
                ReviewCloseDate =
                    x.Review_Close_Date
            })
            .Select(group => group.First())
            .ToList();
    }

    private static string Clean(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private static string CleanMoney(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(
            value.Where(char.IsDigit).ToArray());
    }

    // ─────────────────────────────────────────────────────────────
    // Stored procedure selection
    // ─────────────────────────────────────────────────────────────

    private static string ResolveSp(
        RollSearchConfig config,
        PropertySearchParams searchParams)
    {
        if (searchParams.HasStand &&
            !searchParams.HasAddress &&
            !searchParams.HasScheme)
        {
            return config.SpStand;
        }

        if (searchParams.HasStand &&
            searchParams.HasAddress &&
            !searchParams.HasScheme)
        {
            return config.SpStandAddress;
        }

        if (!searchParams.HasStand &&
            !searchParams.HasAddress &&
            searchParams.HasScheme &&
            !searchParams.HasUnit)
        {
            return config.SpScheme;
        }

        if (!searchParams.HasStand &&
            searchParams.HasAddress &&
            !searchParams.HasScheme)
        {
            return config.SpAddress;
        }

        if (!searchParams.HasStand &&
            !searchParams.HasAddress &&
            !searchParams.HasScheme &&
            searchParams.HasUnit)
        {
            return config.SpUnit;
        }

        if (searchParams.HasScheme &&
            searchParams.HasUnit)
        {
            return config.SpSchemeUnit;
        }

        if (searchParams.HasStand &&
            !searchParams.HasAddress &&
            searchParams.HasScheme)
        {
            return config.SpStandScheme;
        }

        if (!searchParams.HasStand &&
            searchParams.HasAddress &&
            searchParams.HasScheme)
        {
            return config.SpAddressScheme;
        }

        return config.SpTown;
    }

    private static DynamicParameters BuildParams(
        PropertySearchParams searchParams)
    {
        var parameters =
            new DynamicParameters();

        parameters.Add(
            "@SearchTownName",
            $"%{searchParams.TownName.Trim()}%");

        if (searchParams.HasStand)
        {
            parameters.Add(
                "@SearchStand",
                $"%{searchParams.Stand!.Trim()}%");
        }

        if (searchParams.HasAddress)
        {
            parameters.Add(
                "@SearchAddress",
                $"%{searchParams.Address!.Trim()}%");
        }

        if (searchParams.HasScheme)
        {
            parameters.Add(
                "@SearchScheme",
                $"%{searchParams.Scheme!.Trim()}%");
        }

        if (searchParams.HasUnit)
        {
            parameters.Add(
                "@SearchUnit",
                $"%{searchParams.Unit!.Trim()}%");
        }

        return parameters;
    }

    // ─────────────────────────────────────────────────────────────
    // Link property
    // ─────────────────────────────────────────────────────────────

    public async Task<LinkResult> LinkPropertyAsync(
        string rollSource,
        string idProperty,
        string userId,
        string propertyFrom)
    {
        if (string.IsNullOrWhiteSpace(rollSource))
        {
            return LinkResult.Fail(
                "A valuation roll must be supplied.");
        }

        if (string.IsNullOrWhiteSpace(idProperty))
        {
            return LinkResult.Fail(
                "A property must be selected.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return LinkResult.Fail(
                "The current user could not be identified.");
        }

        if (!RollSearchRegistry.Configs.TryGetValue(
                rollSource,
                out var config))
        {
            return LinkResult.Fail(
                $"Unknown roll source '{rollSource}'.");
        }

        var connectionString =
            GetRollConnection(config);

        var isQueryRoll =
            IsQueryRoll(rollSource, config);

        /*
         * Query linked properties historically do not use
         * PropertyFrom. Normal objection/appeal rolls retain it.
         */
        string? resolvedPropertyFrom =
            isQueryRoll
                ? null
                : propertyFrom;

        try
        {
            await using var conn =
                new SqlConnection(connectionString);

            await conn.OpenAsync();

            if (isQueryRoll)
            {
                /*
                 * InsertLinkedProperty for Objection_Query now returns:
                 *
                 * NewID
                 * Review_Status
                 * Review_Close_Date
                 */
                var linkedResult =
                    await conn.QuerySingleOrDefaultAsync<
                        LinkSection78PropertyResult>(
                        SP_LINK_PROPERTY,
                        new
                        {
                            IDProperty = idProperty,
                            UserID = userId,
                            PropertyFrom =
                                resolvedPropertyFrom
                        },
                        commandType:
                            CommandType.StoredProcedure,
                        commandTimeout: 60);

                if (linkedResult is null)
                {
                    return LinkResult.Fail(
                        "The property could not be linked because the database returned no result.");
                }

                return LinkResult.Ok(
                    linkedPropertyId:
                        linkedResult.NewID,

                    reviewStatus:
                        linkedResult.Review_Status,

                    reviewCloseDate:
                        linkedResult.Review_Close_Date);
            }

            /*
             * Existing non-Query rolls may still return only an
             * identity value or may not return a result set.
             */
            await conn.ExecuteAsync(
                SP_LINK_PROPERTY,
                new
                {
                    IDProperty = idProperty,
                    UserID = userId,
                    PropertyFrom =
                        resolvedPropertyFrom
                },
                commandType:
                    CommandType.StoredProcedure,
                commandTimeout: 60);

            return LinkResult.Ok();
        }
        catch (SqlException ex)
            when (ex.Number == 2627 ||
                  ex.Number == 2601)
        {
            return LinkResult.Duplicate();
        }
        catch (SqlException ex)
            when (ex.Number == 50001)
        {
            _logger.LogWarning(
                ex,
                "The selected Query LIS property was not found. Property ID: {PropertyId}",
                idProperty);

            return LinkResult.Fail(
                ex.Message);
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "SQL error linking property {PropertyId} for user {UserId} on roll {RollSource}",
                idProperty,
                userId,
                rollSource);

            return LinkResult.Fail(
                "The property could not be linked because of a database error.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error linking property {PropertyId} for user {UserId} on roll {RollSource}",
                idProperty,
                userId,
                rollSource);

            throw new ApplicationException(
                $"Error linking property '{idProperty}' for user '{userId}' on roll '{rollSource}'.",
                ex);
        }
    }
}
