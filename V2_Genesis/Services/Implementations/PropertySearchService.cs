using Dapper;

using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Models;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Services.Implementations;

public class PropertySearchService : IPropertySearchService
{
    private readonly string _connString;

    // ── Shared township SPs (same source for all rolls) ──────────────
    private const string SP_TOWNSHIPS = "Objection.dbo.propertyDetailsTown";
    private const string SP_SCHEMES = "Objection.dbo.propertyDetailsScheme";

    public PropertySearchService(IConfiguration config)
        => _connString = config.GetConnectionString("DefaultConnection")!;

    // ── Townships ─────────────────────────────────────────────────────
    public async Task<List<string>> GetTownshipsAsync()
    {
        await using var conn = new SqlConnection(_connString);
        var rows = await conn.QueryAsync<string>(
            SP_TOWNSHIPS,
            commandType: CommandType.StoredProcedure);
        return rows.Where(r => !string.IsNullOrWhiteSpace(r)).OrderBy(r => r).ToList();
    }

    // ── Schemes ───────────────────────────────────────────────────────
    public async Task<List<string>> GetSchemesAsync()
    {
        await using var conn = new SqlConnection(_connString);
        var rows = await conn.QueryAsync<string>(
            SP_SCHEMES,
            commandType: CommandType.StoredProcedure);
        return rows.Where(r => !string.IsNullOrWhiteSpace(r)).OrderBy(r => r).ToList();
    }

    // ── Search ────────────────────────────────────────────────────────
    public async Task<List<PropertySearchResult>> SearchAsync(
        string rollSource,
        PropertySearchParams @params)
    {
        if (!RollSearchRegistry.Configs.TryGetValue(rollSource, out var config))
            return new List<PropertySearchResult>();

        var sp = ResolveSp(config, @params);
        var args = BuildParams(@params);

        await using var conn = new SqlConnection(_connString);

        var results = await conn.QueryAsync<PropertySearchResult>(
            sp,
            args,
            commandType: CommandType.StoredProcedure);

        return results.ToList();
    }

    // ── SP selection — mirrors V1 if/else logic exactly ──────────────
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
        return cfg.SpTown; // town only (fallback)
    }

    // ── Dapper params — all values wrapped with wildcards like V1 ─────
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
}