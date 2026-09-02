using Dapper;
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


    // ── GenesisAttributes SP names ────────────────────────────────
    // These SPs exist in the GenesisAttributes DB and query LIS_20260116
    private const string SP_TOWN = "SearchTown";
    private const string SP_STAND = "SearchTownStandNumber";
    private const string SP_STAND_ADDRESS = "SearchTownStandNumberAddress";
    private const string SP_ADDRESS = "SearchTownAddress";
    private const string SP_SCHEME = "SearchTownScheme";
    private const string SP_UNIT = "SearchTownUnit";
    private const string SP_SCHEME_UNIT = "SearchTownSchemeUnit";
    private const string SP_STAND_SCHEME = "SearchTownErfScheme";
    private const string SP_ADDRESS_SCHEME = "SearchTownAddressScheme";

    // Township/Scheme SPs — same pattern as Objection DB
    private const string SP_TOWNSHIPS = "propertyDetailsTown";
    private const string SP_SCHEMES = "propertyDetailsScheme";

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

    // ── Search — GenesisAttributes DB, LIS_20260116 ───────────────
    public async Task<List<PropertySearchResult>> SearchAsync(
        PropertySearchParams p)
    {
        var sp = ResolveSp(p);
        var args = BuildParams(p);

        await using var conn = new SqlConnection(_attrConn);

        var results = await conn.QueryAsync<PropertySearchResult>(
            sp, args, commandType: CommandType.StoredProcedure);

        return results.ToList();
    }

    // ── Townships — GenesisAttributes DB ─────────────────────────
    public async Task<List<string>> GetTownshipsAsync()
    {
        try
        {
            await using var conn = new SqlConnection(_attrConn);

            var towns = await conn.QueryAsync<string>(
                SP_TOWNSHIPS,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            return towns.ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Attributes] GetTownships failed: {ex.Message}");
            return new();
        }
    }

    // ── Schemes — GenesisAttributes DB ───────────────────────────
    public async Task<List<string>> GetSchemesAsync()
    {
        try
        {
            await using var conn = new SqlConnection(_attrConn);

            var schemes = await conn.QueryAsync<string>(
                SP_SCHEMES,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            return schemes.ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Attributes] GetSchemes failed: {ex.Message}");
            return new();
        }
    }

    // ── Property detail — LIS_20260116 + SAP_Contact0126 ─────────
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

    // ── Link property — EF Core, LinkedProperties_Attr ───────────

    public async Task<bool> VerifyAccountStatementPinAsync(
     string unitKey,
     string accountNumber,
     string statementPin,
     CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(unitKey) ||
            string.IsNullOrWhiteSpace(accountNumber) ||
            string.IsNullOrWhiteSpace(statementPin))
        {
            return false;
        }

        await using var conn = new SqlConnection(_attrConn);

        var verified = await conn.QueryFirstOrDefaultAsync<bool?>(
            new CommandDefinition(
                "Attr_VerifyAccountStatementPin",
                new
                {
                    UnitKey = unitKey.Trim(),
                    AccountNumber = accountNumber.Trim(),
                    StatementPin = statementPin.Trim()
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return verified == true;
    }

    public async Task<LinkResult> LinkPropertyAsync(
        string idProperty, string userId, string propertyFrom, string? verifiedAccountNumber = null)
    {
        var exists = await _attrDb.LinkedProperties
            .AnyAsync(p => p.IDProperty == idProperty && p.UserID == userId);

        if (exists) return LinkResult.Duplicate();

        _attrDb.LinkedProperties.Add(new LinkedPropertyAttr
        {
            IDProperty = idProperty,
            UserID = userId,
            PropertyFrom = propertyFrom,
            VerifiedAccountNumber = verifiedAccountNumber,
            AccountVerifiedAt = DateTime.UtcNow,
            VerificationMethod = "AccountStatementPin"
        });

        await _attrDb.SaveChangesAsync();
        return LinkResult.Ok();
    }

    // ── SP resolver ───────────────────────────────────────────────
    private static string ResolveSp(PropertySearchParams p)
    {
        if (p.HasStand && !p.HasAddress && !p.HasScheme) return SP_STAND;
        if (p.HasStand && p.HasAddress && !p.HasScheme) return SP_STAND_ADDRESS;
        if (!p.HasStand && !p.HasAddress && p.HasScheme && !p.HasUnit) return SP_SCHEME;
        if (!p.HasStand && p.HasAddress && !p.HasScheme) return SP_ADDRESS;
        if (!p.HasStand && !p.HasAddress && !p.HasScheme && p.HasUnit) return SP_UNIT;
        if (p.HasScheme && p.HasUnit) return SP_SCHEME_UNIT;
        if (p.HasStand && !p.HasAddress && p.HasScheme) return SP_STAND_SCHEME;
        if (!p.HasStand && p.HasAddress && p.HasScheme) return SP_ADDRESS_SCHEME;
        return SP_TOWN;
    }

    // ── Dapper params ─────────────────────────────────────────────
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

