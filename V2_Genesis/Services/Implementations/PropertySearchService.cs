using Dapper;
using GenesisV2.Services.PropertySearch;
using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Results;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Services.Implementations;

public class PropertySearchService : IPropertySearchService
{
    private readonly IConfiguration _config;

    // DefaultConnection — used ONLY for shared township/scheme SPs
    private readonly string _defaultConn;

    // Township + Scheme SPs live in Objection DB (shared across all rolls)
    private const string SP_TOWNSHIPS = "Objection.dbo.propertyDetailsTown";
    private const string SP_SCHEMES = "Objection.dbo.propertyDetailsScheme";

    public PropertySearchService(IConfiguration config)
    {
        _config = config;
        _defaultConn = config.GetConnectionString("DefaultConnection")!;
    }

    // ── Helper — resolves connection string by roll ConnectionKey ─────
    private string GetRollConnection(RollSearchConfig config) =>
        _config.GetConnectionString(config.ConnectionKey) ?? _defaultConn;

    // ── Townships (always from Objection DB — shared) ─────────────────
    public async Task<List<string>> GetTownshipsAsync()
    {
        await using var conn = new SqlConnection(_defaultConn);
        var rows = await conn.QueryAsync<string>(
            SP_TOWNSHIPS,
            commandType: CommandType.StoredProcedure);
        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .OrderBy(r => r)
            .ToList();
    }

    // ── Schemes (always from Objection DB — shared) ───────────────────
    public async Task<List<string>> GetSchemesAsync()
    {
        await using var conn = new SqlConnection(_defaultConn);
        var rows = await conn.QueryAsync<string>(
            SP_SCHEMES,
            commandType: CommandType.StoredProcedure);
        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .OrderBy(r => r)
            .ToList();
    }

    // ── Search — uses roll-specific DB connection ─────────────────────
    public async Task<List<PropertySearchResult>> SearchAsync(
        string rollSource,
        PropertySearchParams @params)
    {
        if (!RollSearchRegistry.Configs.TryGetValue(rollSource, out var config))
            return new List<PropertySearchResult>();

        var sp = ResolveSp(config, @params);
        var args = BuildParams(@params);
        var connString = GetRollConnection(config);   // ← roll-specific DB

        await using var conn = new SqlConnection(connString);

        var results = await conn.QueryAsync<PropertySearchResult>(
            sp,
            args,
            commandType: CommandType.StoredProcedure);

        return results.ToList();
    }

    // ── Property Detail — uses roll-specific DB connection ────────────
    public async Task<List<PropertyDetailResult>> GetPropertyDetailsAsync(
        string rollSource,
        string unitKey,
        string valuationKey)
    {
        if (!RollSearchRegistry.Configs.TryGetValue(rollSource, out var config))
            return new List<PropertyDetailResult>();

        var connString = GetRollConnection(config);   // ← roll-specific DB

        await using var conn = new SqlConnection(connString);

        var results = await conn.QueryAsync<PropertyDetailResult>(
            config.DetailSp,
            new { UnitKey = unitKey, ValuationKey = valuationKey },
            commandType: CommandType.StoredProcedure);

        return results.ToList();
    }

    // ── SP selection (mirrors V1 if/else logic) ───────────────────────
    private static string ResolveSp(RollSearchConfig cfg, PropertySearchParams p)
    {
        if (p.HasStand && !p.HasAddress && !p.HasScheme) return cfg.SpStand;
        if (p.HasStand && p.HasAddress && !p.HasScheme) return cfg.SpStandAddress;
        if (!p.HasStand && !p.HasAddress && p.HasScheme && !p.HasUnit) return cfg.SpScheme;
        if (!p.HasStand && p.HasAddress && !p.HasScheme) return cfg.SpAddress;
        if (!p.HasStand && !p.HasAddress && !p.HasScheme && p.HasUnit) return cfg.SpUnit;
        if (p.HasScheme && p.HasUnit) return cfg.SpSchemeUnit;
        if (p.HasStand && !p.HasAddress && p.HasScheme) return cfg.SpStandScheme;
        if (!p.HasStand && p.HasAddress && p.HasScheme) return cfg.SpAddressScheme;
        return cfg.SpTown;
    }

    // ── Dapper params (wildcards match V1 pattern) ────────────────────
    private static DynamicParameters BuildParams(PropertySearchParams p)
    {
        var dp = new DynamicParameters();
        dp.Add("@SearchTownName", $"%{p.TownName.Trim()}%");
        if (p.HasStand) dp.Add("@SearchStand", $"%{p.Stand!.Trim()}%");
        if (p.HasAddress) dp.Add("@SearchAddress", $"%{p.Address!.Trim()}%");
        if (p.HasScheme) dp.Add("@SearchScheme", $"%{p.Scheme!.Trim()}%");
        if (p.HasUnit) dp.Add("@SearchUnit", $"%{p.Unit!.Trim()}%");
        return dp;
    }

    public async Task<LinkResult> LinkPropertyAsync(
    string rollSource,
    string idProperty,
    string userId,
    string propertyFrom)
    {
        // Validate roll exists in registry
        if (!RollSearchRegistry.Configs.TryGetValue(rollSource, out var config))
            return LinkResult.Fail($"Unknown roll source '{rollSource}'.");

        // Resolve the roll's own DB connection
        var connString = _config.GetConnectionString(config.ConnectionKey)
                         ?? _defaultConn;

        try
        {
            await using var conn = new SqlConnection(connString);

            await conn.ExecuteAsync(
                "InsertLinkedProperty",
                new
                {
                    IDProperty = idProperty,
                    UserID = userId,
                    PropertyFrom = propertyFrom
                },
                commandType: CommandType.StoredProcedure);

            return LinkResult.Ok();
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            // Unique constraint violation — already linked
            return LinkResult.Duplicate();
        }
        catch (Exception ex)
        {
            // Rethrow with context so controller can log it
            throw new ApplicationException(
                $"Error linking property '{idProperty}' for user '{userId}' on roll '{rollSource}'.",
                ex);
        }
    }
}