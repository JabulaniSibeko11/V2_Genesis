using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Services.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

/// <summary>
/// Resolves one exact submission reference to a canonical property and then
/// performs narrow key-based lookups for the same property on the other rolls.
/// It deliberately does not perform property-description contains searches.
/// </summary>
public sealed class AdminReferenceResolver : IAdminReferenceResolver
{
    private const string QuerySource = "Objection_Query";
    private const string AttributeSource = "Attributes";

    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminReferenceResolver> _logger;
    private readonly int _commandTimeoutSeconds;

    public AdminReferenceResolver(
        IConfiguration configuration,
        ILogger<AdminReferenceResolver> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _commandTimeoutSeconds = Math.Clamp(
            configuration.GetValue("AdminSearch:CommandTimeoutSeconds", 8),
            3,
            30);
    }

    public async Task<AdminEnquiryFoundation?> ResolveAsync(
        string referenceNumber,
        string? rollSource,
        CancellationToken cancellationToken = default)
    {
        var reference = referenceNumber?.Trim();
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        var stopwatch = Stopwatch.StartNew();
        var typeHint = DetectType(reference);
        ReferenceSeed? seed;

        if (typeHint == ReferenceKind.Attribute)
        {
            seed = await ResolveAttributeAsync(reference, cancellationToken);
        }
        else if (typeHint is ReferenceKind.Query or ReferenceKind.Review)
        {
            seed = await ResolveQueryAsync(
                reference,
                typeHint == ReferenceKind.Review,
                cancellationToken);
        }
        else
        {
            seed = await ResolveRollReferenceAsync(
                reference,
                rollSource,
                typeHint,
                cancellationToken);
        }

        // Unknown legacy formats get bounded exact fallbacks. This is still
        // much cheaper than searching every table by partial property text.
        if (seed is null && typeHint == ReferenceKind.Unknown)
        {
            seed = await ResolveQueryAsync(reference, false, cancellationToken)
                ?? await ResolveAttributeAsync(reference, cancellationToken);
        }

        if (seed is null)
            return null;

        var occurrences = seed.Property.HasStableIdentity
            ? await FindRollOccurrencesAsync(seed.Property, cancellationToken)
            : new List<AdminRollOccurrence>();

        EnsureSourceOccurrence(seed, occurrences);

        if (string.IsNullOrWhiteSpace(seed.Property.PropertyFrom))
        {
            seed.Property.PropertyFrom = occurrences
                .FirstOrDefault(x => x.RollSource.Equals(
                    seed.Reference.RollSource,
                    StringComparison.OrdinalIgnoreCase))
                ?.PropertyFrom?.Trim() ?? string.Empty;
        }

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > 3000)
        {
            _logger.LogWarning(
                "[AdminReferenceResolver] Slow reference resolution. Ref={Reference}, ElapsedMs={ElapsedMs}",
                reference,
                stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation(
                "[AdminReferenceResolver] Reference resolved. Ref={Reference}, Type={ReferenceType}, Rolls={RollCount}, ElapsedMs={ElapsedMs}",
                reference,
                seed.Reference.ReferenceType,
                occurrences.Count,
                stopwatch.ElapsedMilliseconds);
        }

        return new AdminEnquiryFoundation
        {
            Reference = seed.Reference,
            Property = seed.Property,
            RollOccurrences = occurrences
                .GroupBy(
                    x => string.Join('|',
                        x.RollSource,
                        x.PremiseId,
                        x.UnitKey,
                        x.ValuationKey),
                    StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => RollOrder(x.RollSource))
                .ToList(),
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<ReferenceSeed?> ResolveRollReferenceAsync(
        string reference,
        string? requestedRoll,
        ReferenceKind typeHint,
        CancellationToken cancellationToken)
    {
        var rolls = SearchableRolls(requestedRoll).ToList();
        if (rolls.Count == 0)
            return null;

        var tasks = rolls.Select(x => ResolveInRollAsync(
            x.Key,
            x.Value,
            reference,
            typeHint,
            cancellationToken));

        var matches = await Task.WhenAll(tasks);
        return matches.FirstOrDefault(x => x is not null);
    }

    private async Task<ReferenceSeed?> ResolveInRollAsync(
        string rollSource,
        AdminRollConfig config,
        string reference,
        ReferenceKind typeHint,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = CreateConnection(config.ConnectionKey);

            if (typeHint != ReferenceKind.Appeal)
            {
                const string objectionSql = """
                    SELECT TOP (1)
                        Objection_No AS ReferenceNumber,
                        objection_Status AS Status,
                        Property_Desc AS PropertyDescription,
                        Property_Type AS PropertyType,
                        Premise_id AS PremiseId,
                        Unit_key AS UnitKey,
                        Valuation_Key AS ValuationKey,
                        PropertyFrom,
                        UserID AS UserId,
                        Objector_Type AS ObjectorType,
                        Capturer AS CapturerSapNumber,
                        Objection_Start_DateTime AS SubmittedAt
                    FROM dbo.Obj_Property_Info
                    WHERE Objection_No = @Reference;
                    """;

                var objection = await connection.QuerySingleOrDefaultAsync<ReferenceRow>(
                    Command(objectionSql, new { Reference = reference }, cancellationToken));

                if (objection is not null)
                    return ToSeed(objection, "Objection", rollSource);
            }

            if (typeHint != ReferenceKind.Objection)
            {
                const string appealSql = """
                    SELECT TOP (1)
                        Appeal_No AS ReferenceNumber,
                        Appeal_Status AS Status,
                        A_Property_Desc AS PropertyDescription,
                        A_Property_Type AS PropertyType,
                        A_Premise_id AS PremiseId,
                        A_Unit_key AS UnitKey,
                        A_Valuation_Key AS ValuationKey,
                        A_UserID AS UserId,
                        Appeal_Type AS ObjectorType,
                        Obj_Ref AS RelatedReferenceNumber,
                        Capturer AS CapturerSapNumber,
                        Appeal_Start_DateTime AS SubmittedAt
                    FROM dbo.Obj_Property_Info_Appeal
                    WHERE Appeal_No = @Reference;
                    """;

                var appeal = await connection.QuerySingleOrDefaultAsync<ReferenceRow>(
                    Command(appealSql, new { Reference = reference }, cancellationToken));

                if (appeal is not null)
                    return ToSeed(appeal, "Appeal", rollSource);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AdminReferenceResolver] Exact lookup failed. Roll={RollSource}, Ref={Reference}",
                rollSource,
                reference);
        }

        return null;
    }

    private async Task<ReferenceSeed?> ResolveQueryAsync(
        string reference,
        bool isReview,
        CancellationToken cancellationToken)
    {
        try
        {
            var storedReference = isReview
                && reference.EndsWith("-R", StringComparison.OrdinalIgnoreCase)
                    ? reference[..^2]
                    : reference;

            const string sql = """
                SELECT TOP (1)
                    QUERY_No AS ReferenceNumber,
                    QUERY_Status AS Status,
                    Property_Desc AS PropertyDescription,
                    Property_Type AS PropertyType,
                    Premise_id AS PremiseId,
                    Unit_key AS UnitKey,
                    Valuation_Key AS ValuationKey,
                    Property_id AS PropertyId,
                    UserID AS UserId,
                    Query_Type AS ObjectorType,
                    Capturer AS CapturerSapNumber,
                    QUERY_Start_DateTime AS SubmittedAt,
                    Sub_typ AS SubType
                FROM dbo.QUE_Property_Info
                WHERE QUERY_No = @StoredReference
                  AND (@IsReview = 0 OR Sub_typ = 1)
                ORDER BY Sub_typ DESC;
                """;

            await using var connection = CreateConnection("QueryConnection");
            var row = await connection.QuerySingleOrDefaultAsync<ReferenceRow>(
                Command(
                    sql,
                    new { StoredReference = storedReference, IsReview = isReview },
                    cancellationToken));

            if (row is null)
                return null;

            var resolvedAsReview = isReview || row.SubType == 1;
            row.ReferenceNumber = resolvedAsReview
                ? EnsureReviewSuffix(row.ReferenceNumber)
                : row.ReferenceNumber;
            row.PropertyFrom = QuerySource;

            return ToSeed(
                row,
                resolvedAsReview ? "Review" : "Query",
                QuerySource);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AdminReferenceResolver] Query lookup failed. Ref={Reference}",
                reference);
            return null;
        }
    }

    private async Task<ReferenceSeed?> ResolveAttributeAsync(
        string reference,
        CancellationToken cancellationToken)
    {
        try
        {
            const string sql = """
                SELECT TOP (1)
                    Attr_No AS ReferenceNumber,
                    Attr_Status AS Status,
                    Property_Desc AS PropertyDescription,
                    Property_Type AS PropertyType,
                    Premise_id AS PremiseId,
                    Unit_key AS UnitKey,
                    Valuation_Key AS ValuationKey,
                    Property_id AS PropertyId,
                    SubmittedByUserId AS UserId,
                    Objector_Type AS ObjectorType,
                    RollDescription,
                    Capturer AS CapturerSapNumber,
                    SubmittedByName,
                    SubmittedByEmail,
                    SubmittedByPhone,
                    SubmissionSource,
                    SubmissionDateTime AS SubmittedAt
                FROM dbo.Attr_Property_Info
                WHERE Attr_No = @Reference;
                """;

            await using var connection = CreateConnection("AttributesConnection");
            var row = await connection.QuerySingleOrDefaultAsync<ReferenceRow>(
                Command(sql, new { Reference = reference }, cancellationToken));

            if (row is null)
                return null;

            row.PropertyFrom = AttributeSource;
            var seed = ToSeed(row, "Attributes", AttributeSource);
            if (!string.IsNullOrWhiteSpace(row.RollDescription))
                seed.Reference.RollName = row.RollDescription.Trim();
            return seed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AdminReferenceResolver] Attribute lookup failed. Ref={Reference}",
                reference);
            return null;
        }
    }

    private async Task<List<AdminRollOccurrence>> FindRollOccurrencesAsync(
        AdminCanonicalProperty property,
        CancellationToken cancellationToken)
    {
        var tasks = SearchableRolls(null).Select(x =>
            FindOccurrencesInRollAsync(
                x.Key,
                x.Value,
                property,
                cancellationToken));

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x).ToList();
    }

    private async Task<IReadOnlyCollection<AdminRollOccurrence>> FindOccurrencesInRollAsync(
        string rollSource,
        AdminRollConfig config,
        AdminCanonicalProperty property,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (20)
                Property_Desc AS PropertyDescription,
                Premise_id AS PremiseId,
                Unit_key AS UnitKey,
                Valuation_Key AS ValuationKey,
                PropertyFrom,
                Objection_No AS ExistingReference,
                objection_Status AS ExistingStatus
            FROM dbo.Obj_Property_Info
            WHERE
                (@PremiseId <> '' AND Premise_id = @PremiseId)
                OR
                (@PremiseId = ''
                 AND @UnitKey <> ''
                 AND @ValuationKey <> ''
                 AND Unit_key = @UnitKey
                 AND Valuation_Key = @ValuationKey)
            ORDER BY Objection_ID DESC;
            """;

        try
        {
            await using var connection = CreateConnection(config.ConnectionKey);
            var rows = await connection.QueryAsync<AdminRollOccurrence>(
                Command(
                    sql,
                    new
                    {
                        property.PremiseId,
                        property.UnitKey,
                        property.ValuationKey
                    },
                    cancellationToken));

            foreach (var row in rows)
            {
                row.RollSource = rollSource;
                row.RollName = RollName(rollSource);
            }

            return rows.ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AdminReferenceResolver] Cross-roll lookup failed. Roll={RollSource}",
                rollSource);
            return Array.Empty<AdminRollOccurrence>();
        }
    }

    private IEnumerable<KeyValuePair<string, AdminRollConfig>> SearchableRolls(
        string? requestedRoll)
    {
        // Genesis currently exposes GV + Supplementary Rolls 1-4. Supp5 is
        // intentionally excluded from client and Admin searches.
        return AdminRollRegistry.Configs
            .Where(x => !x.Key.Equals(
                "Objection_Supp5",
                StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(requestedRoll)
                || x.Key.Equals(requestedRoll, StringComparison.OrdinalIgnoreCase));
    }

    private SqlConnection CreateConnection(string connectionKey)
    {
        var connectionString = _configuration.GetConnectionString(connectionKey)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionKey}' was not found.");
        return new SqlConnection(connectionString);
    }

    private CommandDefinition Command(
        string sql,
        object parameters,
        CancellationToken cancellationToken) =>
        new(
            sql,
            parameters,
            commandTimeout: _commandTimeoutSeconds,
            cancellationToken: cancellationToken);

    private static ReferenceSeed ToSeed(
        ReferenceRow row,
        string referenceType,
        string rollSource) =>
        new()
        {
            Reference = new AdminResolvedReference
            {
                ReferenceNumber = Clean(row.ReferenceNumber),
                ReferenceType = referenceType,
                RollSource = rollSource,
                RollName = RollName(rollSource),
                Status = Clean(row.Status),
                UserId = Clean(row.UserId),
                ObjectorType = Clean(row.ObjectorType),
                RelatedReferenceNumber = Clean(row.RelatedReferenceNumber),
                CapturerSapNumber = Clean(row.CapturerSapNumber),
                SubmittedByName = Clean(row.SubmittedByName),
                SubmittedByEmail = Clean(row.SubmittedByEmail),
                SubmittedByPhone = Clean(row.SubmittedByPhone),
                SubmissionSource = Clean(row.SubmissionSource),
                SubmittedAt = row.SubmittedAt
            },
            Property = new AdminCanonicalProperty
            {
                PremiseId = Clean(row.PremiseId),
                UnitKey = Clean(row.UnitKey),
                ValuationKey = Clean(row.ValuationKey),
                PropertyId = Clean(row.PropertyId),
                PropertyDescription = Clean(row.PropertyDescription),
                PropertyType = Clean(row.PropertyType),
                PropertyFrom = Clean(row.PropertyFrom)
            }
        };

    private static void EnsureSourceOccurrence(
        ReferenceSeed seed,
        ICollection<AdminRollOccurrence> occurrences)
    {
        if (occurrences.Any(x => x.RollSource.Equals(
                seed.Reference.RollSource,
                StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        occurrences.Add(new AdminRollOccurrence
        {
            RollSource = seed.Reference.RollSource,
            RollName = seed.Reference.RollName,
            PropertyDescription = seed.Property.PropertyDescription,
            PremiseId = seed.Property.PremiseId,
            UnitKey = seed.Property.UnitKey,
            ValuationKey = seed.Property.ValuationKey,
            PropertyFrom = seed.Property.PropertyFrom,
            ExistingReference = seed.Reference.ReferenceNumber,
            ExistingStatus = seed.Reference.Status
        });
    }

    private static ReferenceKind DetectType(string reference)
    {
        var value = reference.Trim().ToUpperInvariant();
        if (value.StartsWith("ATTR-")) return ReferenceKind.Attribute;
        if (value.EndsWith("-R")) return ReferenceKind.Review;
        if (value.StartsWith("REV") || value.StartsWith("REVIEW")) return ReferenceKind.Review;
        if (value.StartsWith("QUE") || value.Contains("-QUE")) return ReferenceKind.Query;
        if (value.StartsWith("APP")) return ReferenceKind.Appeal;
        if (value.StartsWith("OBJ") || value.StartsWith("GV")) return ReferenceKind.Objection;
        return ReferenceKind.Unknown;
    }

    private static string EnsureReviewSuffix(string? reference)
    {
        var value = Clean(reference);
        return value.EndsWith("-R", StringComparison.OrdinalIgnoreCase)
            ? value
            : value + "-R";
    }

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

    private static string RollName(string rollSource) => rollSource switch
    {
        "Objection" => "GV 2023",
        "Objection_Supp1" => "Supplementary Roll 1",
        "Objection_Supp2" => "Supplementary Roll 2",
        "Objection_Supp3" => "Supplementary Roll 3",
        "Objection_Supp4" => "Supplementary Roll 4",
        QuerySource => "Section 78 Query / Review",
        AttributeSource => "Property Attributes",
        _ => rollSource
    };

    private static int RollOrder(string rollSource) => rollSource switch
    {
        "Objection" => 0,
        "Objection_Supp1" => 1,
        "Objection_Supp2" => 2,
        "Objection_Supp3" => 3,
        "Objection_Supp4" => 4,
        QuerySource => 10,
        AttributeSource => 11,
        _ => 99
    };

    private enum ReferenceKind
    {
        Unknown,
        Objection,
        Appeal,
        Query,
        Review,
        Attribute
    }

    private sealed class ReferenceSeed
    {
        public AdminResolvedReference Reference { get; init; } = new();
        public AdminCanonicalProperty Property { get; init; } = new();
    }

    private sealed class ReferenceRow
    {
        public string? ReferenceNumber { get; set; }
        public string? Status { get; set; }
        public string? PropertyDescription { get; set; }
        public string? PropertyType { get; set; }
        public string? PremiseId { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PropertyId { get; set; }
        public string? PropertyFrom { get; set; }
        public string? UserId { get; set; }
        public string? ObjectorType { get; set; }
        public string? RollDescription { get; set; }
        public string? RelatedReferenceNumber { get; set; }
        public string? CapturerSapNumber { get; set; }
        public string? SubmittedByName { get; set; }
        public string? SubmittedByEmail { get; set; }
        public string? SubmittedByPhone { get; set; }
        public string? SubmissionSource { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int SubType { get; set; }
    }
}
