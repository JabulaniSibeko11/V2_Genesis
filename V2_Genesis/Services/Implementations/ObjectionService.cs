using Dapper;
using GenesisV2.Services.PropertySearch;
using Microsoft.Data.SqlClient;
using System.Data;

using V2_Genesis.Models.Objections;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Services.Implementations;

public class ObjectionService : IObjectionService
{
    private readonly IConfiguration _config;

    // ── sourceTable → (ConnectionKey, SP name) ────────────────────────
    private static readonly Dictionary<string, (string ConnKey, string Sp)> _sourceMap = new()
    {
        ["GV23-SUP3"] = ("Sup3Connection", "CheckPropertyFromSup3"),
        ["GV23-SUP2"] = ("Sup2Connection", "CheckPropertyFromSup2"),
        ["GV23-SUP1"] = ("Sup1Connection", "CheckPropertyFromSup1"),
        ["GV23"] = ("DefaultConnection", "CheckPropertyFromGV"),
        ["LIS"] = ("LISConnection", "CheckPropertyFromLIS"),
    };

    // ── sourceTable → MVC controller name (for Lodge button routing) ──
    public static readonly Dictionary<string, string> SourceToController = new()
    {
        ["GV23-SUP3"] = "Sup3",
        ["GV23-SUP2"] = "Sup2",
        ["GV23-SUP1"] = "Sup1",
        ["GV23"] = "Objection",
        ["LIS"] = "LIS",
    };

    private const string SP_APPEAL = "IndexAppeal";

    public ObjectionService(IConfiguration config)
        => _config = config;

    // ── Normal objection property fetch ───────────────────────────────
    public async Task<List<CheckPropertyResult>> GetPropertyForObjectionAsync(
        string sourceTable,
        string unitKey,
        string valuationKey)
    {
        if (!_sourceMap.TryGetValue(sourceTable, out var cfg))
            return new List<CheckPropertyResult>();

        var connString = _config.GetConnectionString(cfg.ConnKey)
                         ?? _config.GetConnectionString("DefaultConnection")!;

        await using var conn = new SqlConnection(connString);

        var results = await conn.QueryAsync<CheckPropertyResult>(
            cfg.Sp,
            new { UnitKey = unitKey, ValuationKey = valuationKey },
            commandType: CommandType.StoredProcedure);

        return results.ToList();
    }

    // ── Appeal property fetch ─────────────────────────────────────────
    public async Task<List<CheckPropertyResult>> GetPropertyForAppealAsync(
        string rollSource,
        string objectionNo)
    {
        // Use the roll's connection from the search registry
        if (!RollSearchRegistry.Configs.TryGetValue(rollSource, out var rollCfg))
            return new List<CheckPropertyResult>();

        var connString = _config.GetConnectionString(rollCfg.ConnectionKey)
                         ?? _config.GetConnectionString("DefaultConnection")!;

        await using var conn = new SqlConnection(connString);

        // IndexAppeal returns differently-named columns — map manually
        var raw = await conn.QueryAsync(
            SP_APPEAL,
            new { Objection_No = objectionNo },
            commandType: CommandType.StoredProcedure);

        return raw.Select(r => new CheckPropertyResult
        {
            PremiseId = r.Premise_id?.ToString(),
            UnitKey = r.Unit_key?.ToString(),
            PropertyId = r.Property_id?.ToString(),
            ValuationKey = r.Valuation_Key?.ToString(),
            Sector = r.Sector?.ToString(),
            TownNameDesc = r.Town_Name?.ToString(),
            MarketValue = r.New_Market_Value_MVD?.ToString(),
            RateableArea = r.New_Extent_MVD?.ToString(),
            LisStreetAddress = r.New_Address_MVD?.ToString(),
            CatDesc = r.New_Category_MVD?.ToString(),
            PropertyDesc = r.New_Property_Description_MVD?.ToString(),
            OwnerName = r.New_Owner_MVD?.ToString(),
            // Map extra appeal columns into spare fields
            Re = r.New3_Market_Value_MVD?.ToString(),
            Reason = r.New3_Extent_MVD?.ToString(),
            ValuationDate = r.New3_Category_MVD?.ToString(),
            SchemeYear = r.New2_Extent_MVD?.ToString(),
            SchemeNumber = r.New2_Category_MVD?.ToString(),
            SchemeName = r.Property_Desc?.ToString(),
        }).ToList();
    }
}