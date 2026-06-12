using Dapper;
using GenesisV2.Services.PropertySearch;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using V2_Genesis.Helpers;
using V2_Genesis.Models.Objections;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Lis;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Services.Implementations;

public class ObjectionService : IObjectionService
{
    private readonly IConfiguration _config;

    // ── sourceTable → (ConnectionKey, SP name) ────────────────────────
    private static readonly Dictionary<string, (string ConnKey, string Sp)> _sourceMap = new()
    {
        ["GV23-SUP5"] = ("Sup5Connection", "CheckPropertyFromSup5"),
        ["GV23-SUP4"] = ("Sup4Connection", "CheckPropertyFromSup4"),
        ["GV23-SUP3"] = ("Sup3Connection", "CheckPropertyFromSup3"),
        ["GV23-SUP2"] = ("Sup2Connection", "CheckPropertyFromSup2"),
        ["GV23-SUP1"] = ("Sup1Connection", "CheckPropertyFromSup1"),
        ["GV23"] = ("DefaultConnection", "CheckProperty")
        
    };

    // ── sourceTable → MVC controller name ─────────────────────────────
    public static readonly Dictionary<string, string> SourceToController = new()
    {
        ["GV23-SUP5"] = "Sup5",
        ["GV23-SUP4"] = "Sup4",
        ["GV23-SUP3"] = "Sup3",
        ["GV23-SUP2"] = "Sup2",
        ["GV23-SUP1"] = "Sup1",
        ["GV23"] = "Objection",
       
    };

    // ── rollSource → MVC controller name ──────────────────────────────
    public static readonly Dictionary<string, string> RollSourceToController = new()
    {
        ["Objection"] = "Objection",
        ["Objection_Supp1"] = "Sup1",
        ["Objection_Supp2"] = "Sup2",
        ["Objection_Supp3"] = "Sup3",
        ["Objection_Supp4"] = "Sup4",
        ["Objection_Supp5"] = "Sup5",
    };

    // ── rollSource → sourceTable ──────────────────────────────────────
    public static readonly Dictionary<string, string> RollSourceToSourceTable = new()
    {
        ["Objection"] = "GV23",
        ["Objection_Supp1"] = "GV23-SUP1",
        ["Objection_Supp2"] = "GV23-SUP2",
        ["Objection_Supp3"] = "GV23-SUP3",
        ["Objection_Supp4"] = "GV23-SUP4",
        ["Objection_Supp5"] = "GV23-SUP5",
    };

    // ── sourceTable → rollSource ──────────────────────────────────────
    // This is the important reverse map for saving.
    public static readonly Dictionary<string, string> SourceTableToRollSource = new()
    {
        ["GV23"] = "Objection",
        ["GV23-SUP1"] = "Objection_Supp1",
        ["GV23-SUP2"] = "Objection_Supp2",
        ["GV23-SUP3"] = "Objection_Supp3",
        ["GV23-SUP4"] = "Objection_Supp4",
        ["GV23-SUP5"] = "Objection_Supp5",
    };

    private const string SP_APPEAL = "IndexAppeal";

    public ObjectionService(IConfiguration config)
        => _config = config;

    // ── Convert anything into rollSource for saving ───────────────────
    public static string NormalizeRollSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "Objection_Supp3";

        source = source.Trim();

        if (SourceTableToRollSource.TryGetValue(source, out var rollSource))
            return rollSource;

        if (RollSourceToSourceTable.ContainsKey(source))
            return source;

        return source switch
        {
            "Sup1" => "Objection_Supp1",
            "Sup2" => "Objection_Supp2",
            "Sup3" => "Objection_Supp3",
            "Sup4" => "Objection_Supp4",
            "Sup5" => "Objection_Supp5",

            "SUP1" => "Objection_Supp1",
            "SUP2" => "Objection_Supp2",
            "SUP3" => "Objection_Supp3",
            "SUP4" => "Objection_Supp4",
            "SUP5" => "Objection_Supp5",

            _ => source
        };
    }

    // ── Convert rollSource into sourceTable for searching/display ──────
    public static string NormalizeSourceTable(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "GV23-SUP3";

        source = source.Trim();

        if (RollSourceToSourceTable.TryGetValue(source, out var sourceTable))
            return sourceTable;

        if (_sourceMap.ContainsKey(source))
            return source;

        return source switch
        {
            "Sup1" => "GV23-SUP1",
            "Sup2" => "GV23-SUP2",
            "Sup3" => "GV23-SUP3",
            "Sup4" => "GV23-SUP4",
            "Sup5" => "GV23-SUP5",

            "SUP1" => "GV23-SUP1",
            "SUP2" => "GV23-SUP2",
            "SUP3" => "GV23-SUP3",
            "SUP4" => "GV23-SUP4",
            "SUP5" => "GV23-SUP5",

            _ => source
        };
    }

    // ── Get connection key from rollSource ─────────────────────────────
    public static string GetConnectionKeyFromRollSource(string? rollSource)
    {
        rollSource = NormalizeRollSource(rollSource);

        return rollSource switch
        {
            "Objection_Supp5" => "Sup5Connection",
            "Objection_Supp4" => "Sup4Connection",
            "Objection_Supp3" => "Sup3Connection",
            "Objection_Supp2" => "Sup2Connection",
            "Objection_Supp1" => "Sup1Connection",
            "Objection" => "DefaultConnection",
            _ => "DefaultConnection"
        };
    }

    // ── Normal objection property fetch ───────────────────────────────
    public async Task<List<CheckPropertyResult>> GetPropertyForObjectionAsync(
        string sourceTable,
        string? unitKey,
        string? valuationKey)
    {
        sourceTable = NormalizeSourceTable(sourceTable);

        if (!_sourceMap.TryGetValue(sourceTable, out var cfg))
            return new List<CheckPropertyResult>();

        var connString = _config.GetConnectionString(cfg.ConnKey)
                         ?? _config.GetConnectionString("DefaultConnection")!;

        unitKey = FloatKeyHelper.Normalize(unitKey);
        valuationKey = FloatKeyHelper.Normalize(valuationKey);

        await using var conn = new SqlConnection(connString);

        var parameters = new DynamicParameters();
        parameters.Add("@UnitKey", unitKey, DbType.String);
        parameters.Add("@ValuationKey", valuationKey, DbType.String);

        var results = await conn.QueryAsync<CheckPropertyResult>(
            cfg.Sp,
            parameters,
            commandType: CommandType.StoredProcedure);

        return results.ToList();
    }

    // ── Appeal property fetch ─────────────────────────────────────────
    public async Task<List<CheckPropertyResult>> GetPropertyForAppealAsync(
        string rollSource,
        string objectionNo)
    {
        rollSource = NormalizeRollSource(rollSource);

        if (!RollSearchRegistry.Configs.TryGetValue(rollSource, out var rollCfg))
            return new List<CheckPropertyResult>();

        var connString = _config.GetConnectionString(rollCfg.ConnectionKey)
                         ?? _config.GetConnectionString(GetConnectionKeyFromRollSource(rollSource))
                         ?? _config.GetConnectionString("DefaultConnection")!;

        await using var conn = new SqlConnection(connString);

        var raw = await conn.QueryAsync(
            SP_APPEAL,
            new { Objection_No = objectionNo },
            commandType: CommandType.StoredProcedure);

        return raw.Select(r => new CheckPropertyResult
        {
            PremiseId = r.Premise_id?.ToString(),
            UnitKey = NormalizeKey(r.Unit_key),
            PropertyId = r.Property_id?.ToString(),
            ValuationKey = NormalizeKey(r.Valuation_Key),
            Sector = r.Sector?.ToString(),
            TownNameDesc = r.Town_Name?.ToString(),
            MarketValue = r.New_Market_Value_MVD?.ToString(),
            RateableArea = r.New_Extent_MVD?.ToString(),
            LisStreetAddress = r.New_Address_MVD?.ToString(),
            CatDesc = r.New_Category_MVD?.ToString(),
            PropertyDesc = r.New_Property_Description_MVD?.ToString(),
            OwnerName = r.New_Owner_MVD?.ToString(),

            Re = r.New3_Market_Value_MVD?.ToString(),
            Reason = r.New3_Extent_MVD?.ToString(),
            ValuationDate = r.New3_Category_MVD?.ToString(),
            SchemeYear = r.New2_Extent_MVD?.ToString(),
            SchemeNumber = r.New2_Category_MVD?.ToString(),
            SchemeName = r.Property_Desc?.ToString(),
        }).ToList();
    }

    private static string NormalizeKey(object? value)
    {
        if (value == null)
            return string.Empty;

        var key = value.ToString()?.Trim();

        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        key = Regex.Replace(
            key,
            @"([eE])\s+([+-]?\d+)",
            "$1+$2");

        if (decimal.TryParse(
            key,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var decimalValue))
        {
            return Math.Round(decimalValue, 0)
                .ToString("0", CultureInfo.InvariantCulture);
        }

        return key;
    }
    public async Task<List<CheckPropertyResult>> GetPropertyForLisAsync(
    string rollSource,
    string? unitKey,
    string? valuationKey)
    {
        rollSource = NormalizeRollSource(rollSource);

        unitKey = FloatKeyHelper.Normalize(unitKey);
        valuationKey = FloatKeyHelper.Normalize(valuationKey);

        if (string.IsNullOrWhiteSpace(unitKey) &&
            string.IsNullOrWhiteSpace(valuationKey))
        {
            return new List<CheckPropertyResult>();
        }

        var lisRegistry = LisRollRegistry.Build();

        if (!lisRegistry.TryGetValue(rollSource, out var lisCfg))
            return new List<CheckPropertyResult>();

        var connString = _config.GetConnectionString(lisCfg.ConnectionKey)
                         ?? _config.GetConnectionString(GetConnectionKeyFromRollSource(rollSource))
                         ?? _config.GetConnectionString("DefaultConnection")!;

        await using var conn = new SqlConnection(connString);

        var parameters = new DynamicParameters();
        parameters.Add("@UnitKey", unitKey, DbType.String);
        parameters.Add("@ValuationKey", valuationKey, DbType.String);

        var raw = await conn.QueryAsync(
            lisCfg.DetailSp, // IndexObjectionLIS
            parameters,
            commandType: CommandType.StoredProcedure);

        return raw.Select(r => new CheckPropertyResult
        {
            TownNameDesc = r.Town_Name_Description?.ToString(),
            OwnerName = r.Owner_Name?.ToString(),

            Erf = ToInt(r.Erf_Number),
            Ptn = ToInt(r.Portion_Number),
            Re = r.Property_Remainder_Indicator?.ToString(),

            LisStreetAddress = r.LisStreetAddress?.ToString(),
            CatDesc = r.CAT_Description?.ToString(),
            RateableArea = r.Rateable_Area?.ToString(),
            MarketValue = r.Market_Value?.ToString(),

            ValuationDate = FormatDate(r.Valuation_Effective_Date_Wef_Date),
            WefDate = FormatDate(r.Valuation_Effective_Date_Wef_Date),

            Reason = r.Reason?.ToString(),
            SchemeName = r.Scheme_Name?.ToString(),
            SchemeNumber = r.Scheme_Number?.ToString(),
            SchemeYear = r.Scheme_Year?.ToString(),

            UnitNo = ToInt(r.Unit_Number),
            PropertyDesc = r.Property_Desc?.ToString(),

            PremiseId = r.Premise_ID?.ToString(),
            UnitKey = NormalizeKey(r.Unit_Key),
            PropertyId = r.Property_ID?.ToString(),
            ValuationKey = NormalizeKey(r.Valuation_Key),

            Sector = r.sector?.ToString() ,

            // Use SAP address as extra owner/postal address if you need it later
           
        }).ToList();
    }
    private static int ToInt(object? value)
    {
        if (value == null)
            return 0;

        var text = value.ToString();

        if (int.TryParse(text, out var i))
            return i;

        if (double.TryParse(text, out var d))
            return Convert.ToInt32(d);

        return 0;
    }

    private static string FormatDate(object? value)
    {
        if (value == null)
            return "";

        var text = value.ToString();

        if (string.IsNullOrWhiteSpace(text))
            return "";

        return DateTime.TryParse(text, out var date)
            ? date.ToString("dd MMMM yyyy")
            : text;
    }

    private static string? BuildAddress(
        string? addr1,
        string? addr2,
        string? addr3,
        string? addr4,
        string? addr5)
    {
        var parts = new[]
        {
        addr1,
        addr2,
        addr3,
        addr4,
        addr5
    }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!.Trim())
        .ToList();

        return parts.Any()
            ? string.Join(", ", parts)
            : null;
    }
}