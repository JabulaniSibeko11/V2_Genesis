using Dapper;
using GenesisV2.Services.PropertySearch;
using System.Data;
using System.Data.SqlClient;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Services;
using V2_Genesis.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class AttributesSearchService : IAttributesSearchService
{
    private readonly IConfiguration _config;
    private readonly AttributesDbContext _attrDb;
    private readonly string _attrConn;

    private static readonly RollSearchConfig GvConfig =
        RollSearchRegistry.Configs["Objection"];

    public AttributesSearchService(
        IConfiguration config,
        AttributesDbContext attrDb)
    {
        _config = config;
        _attrDb = attrDb;
        _attrConn = config.GetConnectionString("AttributesConnection")
            ?? throw new InvalidOperationException(
                "AttributesConnection missing from appsettings");
    }

    // ── Get full property detail from LIS + SAP ───────────────────
    public async Task<LisPropertyDetail?> GetPropertyDetailAsync(string unitKey)
    {
        try
        {
            await using var conn = new SqlConnection(_attrConn);

            return await conn.QueryFirstOrDefaultAsync<LisPropertyDetail>(
                "Attr_GetPropertyForCheck",
                new { UnitKey = unitKey },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Attributes] GetPropertyDetail failed for {unitKey}: {ex.Message}");
            return null;
        }
    }

    // ── Search ────────────────────────────────────────────────────
    public async Task<List<PropertySearchResult>> SearchAsync(PropertySearchParams p)
    {
        var sp = ResolveSp(GvConfig, p);
        var args = BuildParams(p);

        await using var conn = new SqlConnection(_attrConn);

        var results = await conn.QueryAsync<PropertySearchResult>(
            sp, args, commandType: CommandType.StoredProcedure);

        return results.ToList();
    }

    // ── Link ──────────────────────────────────────────────────────
    public async Task<LinkResult> LinkPropertyAsync(
        string idProperty, string userId, string propertyFrom)
    {
        var exists = await _attrDb.LinkedProperties
            .AnyAsync(p => p.IDProperty == idProperty && p.UserID == userId);

        if (exists) return LinkResult.Duplicate();

        _attrDb.LinkedProperties.Add(new LinkedPropertyAttr
        {
            IDProperty = idProperty,
            UserID = userId,
            PropertyFrom = propertyFrom
        });

        await _attrDb.SaveChangesAsync();
        return LinkResult.Ok();
    }

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