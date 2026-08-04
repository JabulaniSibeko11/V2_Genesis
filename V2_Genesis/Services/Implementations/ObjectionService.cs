using Dapper;
using GenesisV2.Services.PropertySearch;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using V2_Genesis.Helpers;
using V2_Genesis.Models.Objections;
using V2_Genesis.Models.Results;
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

            Sector = r.sector?.ToString(),

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
    private string GetConnectionStringForDuplicateCheck(
    string? rollSource,
    string? sourceTable)
    {
        rollSource = NormalizeRollSource(rollSource);
        sourceTable = NormalizeSourceTable(sourceTable);

        var connectionKey = GetConnectionKeyFromRollSource(rollSource);

        if (!string.IsNullOrWhiteSpace(sourceTable) &&
            _sourceMap.TryGetValue(sourceTable, out var cfg))
        {
            connectionKey = cfg.ConnKey;
        }

        var connectionString =
            _config.GetConnectionString(connectionKey)
            ?? _config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionKey}' was not found.");
        }

        return connectionString;
    }
    public async Task<DuplicateLodgementResult> CheckDuplicateLodgementAsync(
    string rollSource,
    string sourceTable,
    string? unitKey,
    string? valuationKey,
    string? propertyDesc,
    bool isAppeal)
    {
        unitKey = FloatKeyHelper.Normalize(unitKey);
        valuationKey = FloatKeyHelper.Normalize(valuationKey);

        propertyDesc = propertyDesc?.Trim();

        if (string.IsNullOrWhiteSpace(unitKey) &&
            string.IsNullOrWhiteSpace(valuationKey) &&
            string.IsNullOrWhiteSpace(propertyDesc))
        {
            return new DuplicateLodgementResult
            {
                Exists = false,
                IsAppeal = isAppeal
            };
        }

        var connectionString = GetConnectionStringForDuplicateCheck(
     rollSource,
     sourceTable);

        await using var db = new ObjectionReadDbContext(connectionString);

        if (isAppeal)
        {
            var row = await db.Appeals
                .AsNoTracking()
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(valuationKey) &&
                     x.ValuationKey == valuationKey) ||
                    (!string.IsNullOrWhiteSpace(unitKey) &&
                     x.UnitKey == unitKey) ||
                    (!string.IsNullOrWhiteSpace(propertyDesc) &&
                     (x.PropertyDescription ?? string.Empty).Trim() == propertyDesc))
                .OrderByDescending(x => x.AppealId)
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return new DuplicateLodgementResult
                {
                    Exists = false,
                    IsAppeal = true
                };
            }

            return new DuplicateLodgementResult
            {
                Exists = true,
                IsAppeal = true,
                ReferenceNo = row.AppealNo,
                Status = row.AppealStatus,
                PropertyDescription = row.PropertyDescription
            };
        }

        var objRow = await db.Objections
            .AsNoTracking()
            .Where(x =>
                (!string.IsNullOrWhiteSpace(valuationKey) &&
                 x.ValuationKey == valuationKey) ||
                (!string.IsNullOrWhiteSpace(unitKey) &&
                 x.UnitKey == unitKey) ||
                (!string.IsNullOrWhiteSpace(propertyDesc) &&
                 (x.PropertyDescription ?? string.Empty).Trim() == propertyDesc))
            .OrderByDescending(x => x.ObjectionId)
            .FirstOrDefaultAsync();

        if (objRow == null)
        {
            return new DuplicateLodgementResult
            {
                Exists = false,
                IsAppeal = false
            };
        }

        return new DuplicateLodgementResult
        {
            Exists = true,
            IsAppeal = false,
            ReferenceNo = objRow.ObjectionNo,
            Status = objRow.ObjectionStatus,
            PropertyDescription = objRow.PropertyDescription
        };
    }
    private static DateTime TodaySa()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
        }
        catch
        {
            return DateTime.Today;
        }
    }

    public Task<LodgementWindowResult> CheckObjectionWindowAsync(
    string rollSource,
    string sourceTable)
    {
        rollSource = NormalizeRollSource(rollSource);
        sourceTable = NormalizeSourceTable(sourceTable);

        var today = TodaySa();

        var openDateText =
            _config[$"RollDates:{rollSource}:OpenDate"]
            ?? _config[$"RollDates:{sourceTable}:OpenDate"];

        var closeDateText =
            _config[$"RollDates:{rollSource}:VisibleUntil"]
            ?? _config[$"RollDates:{sourceTable}:VisibleUntil"];

        if (string.IsNullOrWhiteSpace(openDateText) ||
            string.IsNullOrWhiteSpace(closeDateText) ||
            !DateTime.TryParse(openDateText, out var openDate) ||
            !DateTime.TryParse(closeDateText, out var closeDate))
        {
            return Task.FromResult(new LodgementWindowResult
            {
                Exists = false,
                IsOpen = false,
                Type = "Objection"
            });
        }

        var isOpen =
            today >= openDate.Date &&
            today <= closeDate.Date;

        return Task.FromResult(new LodgementWindowResult
        {
            Exists = true,
            IsOpen = isOpen,
            Type = "Objection",
            StartDate = openDate,
            CloseDate = closeDate
        });
    }

    public async Task<AppealEligibilityResult> CheckAppealEligibilityAsync(
        string rollSource,
        string objectionNo,
        string? unitKey,
        string? valuationKey,
        string? propertyDesc)
    {
        rollSource = NormalizeRollSource(rollSource);
        objectionNo = objectionNo?.Trim() ?? string.Empty;
        unitKey = FloatKeyHelper.Normalize(unitKey);
        valuationKey = FloatKeyHelper.Normalize(valuationKey);
        propertyDesc = propertyDesc?.Trim();

        if (string.IsNullOrWhiteSpace(objectionNo))
        {
            return new AppealEligibilityResult();
        }

        var connectionKey = GetConnectionKeyFromRollSource(rollSource);
        var connectionString =
            _config.GetConnectionString(connectionKey)
            ?? _config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionKey}' was not found.");
        }

        await using var db = new ObjectionReadDbContext(connectionString);

        var objection = await db.Objections
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                (x.ObjectionNo ?? string.Empty).Trim() == objectionNo);

        AppealEligibilityRow? row = null;

        if (objection is not null)
        {
            var mvd = await db.MvdRevised
                .AsNoTracking()
                .Where(x =>
                    (x.ObjectionNo ?? string.Empty).Trim() == objectionNo)
                .OrderByDescending(x => x.BatchDate)
                .FirstOrDefaultAsync();

            var existing = await db.Appeals
                .AsNoTracking()
                .Where(x =>
                    (x.ObjectReference ?? string.Empty).Trim() == objectionNo ||
                    (!string.IsNullOrWhiteSpace(valuationKey) &&
                     x.ValuationKey == valuationKey) ||
                    (!string.IsNullOrWhiteSpace(unitKey) &&
                     x.UnitKey == unitKey) ||
                    (!string.IsNullOrWhiteSpace(propertyDesc) &&
                     (x.PropertyDescription ?? string.Empty).Trim() == propertyDesc))
                .OrderByDescending(x => x.AppealId)
                .FirstOrDefaultAsync();

            row = new AppealEligibilityRow
            {
                ObjectionNo = objection.ObjectionNo,
                ObjectionStatus = objection.ObjectionStatus,
                PropertyDescription = objection.PropertyDescription,
                AppealStartDate = mvd?.AppealStartDate,
                AppealCloseDate = mvd?.AppealCloseDate,
                RevisedAppealStartDate = mvd?.RevisedAppealStartDate,
                RevisedAppealCloseDate = mvd?.RevisedAppealCloseDate,
                ReviseMvd = mvd?.ReviseMvd,
                ExistingAppealNo = existing?.AppealNo,
                ExistingAppealStatus = existing?.AppealStatus
            };
        }

        if (row is null)
        {
            return new AppealEligibilityResult
            {
                ObjectionNumber = objectionNo
            };
        }

        var status = row.ObjectionStatus?.Trim() ?? string.Empty;
        var reviseText = row.ReviseMvd?.Trim() ?? string.Empty;

        var usesRevisedDates =
            reviseText.Equals("True", StringComparison.OrdinalIgnoreCase)
            || reviseText.Equals("Yes", StringComparison.OrdinalIgnoreCase)
            || reviseText.Equals("1", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(
                row.RevisedAppealCloseDate?.ToString());

        DateTime? startDate = null;
        DateTime? closeDate = null;

        if (usesRevisedDates)
        {
            startDate = row.RevisedAppealStartDate;
            closeDate = row.RevisedAppealCloseDate;
        }

        startDate ??= row.AppealStartDate;
        closeDate ??= row.AppealCloseDate;

        var today = TodaySa();
        var periodExists = startDate.HasValue && closeDate.HasValue;
        var periodOpen =
            periodExists
            && today >= startDate!.Value.Date
            && today <= closeDate!.Value.Date;

        var existingAppealNumber =
            row.ExistingAppealNo?.Trim() ?? string.Empty;

        var existingAppealStatus =
            row.ExistingAppealStatus?.Trim() ?? string.Empty;

        return new AppealEligibilityResult
        {
            ObjectionExists = true,
            HasNoticeSentStatus = status.Equals(
                "Notice-Sent",
                StringComparison.OrdinalIgnoreCase),
            AppealPeriodExists = periodExists,
            IsAppealPeriodOpen = periodOpen,
            ExistingAppealFound =
                !string.IsNullOrWhiteSpace(existingAppealNumber)
                || !string.IsNullOrWhiteSpace(existingAppealStatus),
            UsesRevisedMvdDates = usesRevisedDates,
            ObjectionNumber = row.ObjectionNo?.Trim() ?? objectionNo,
            ObjectionStatus = status,
            PropertyDescription = row.PropertyDescription?.Trim() ?? string.Empty,
            AppealStartDate = startDate,
            AppealCloseDate = closeDate,
            ExistingAppealNumber = existingAppealNumber,
            ExistingAppealStatus = existingAppealStatus
        };
    }

    public async Task<LodgementWindowResult> CheckAppealWindowAsync(
    string rollSource,
    string? objectionNo,
    string? unitKey,
    string? valuationKey,
    string? propertyDesc)
    {
        rollSource = NormalizeRollSource(rollSource);

        unitKey = FloatKeyHelper.Normalize(unitKey);
        valuationKey = FloatKeyHelper.Normalize(valuationKey);
        propertyDesc = propertyDesc?.Trim();

        var connectionKey = GetConnectionKeyFromRollSource(rollSource);

        var connString =
            _config.GetConnectionString(connectionKey)
            ?? _config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionKey}' was not found.");
        }

        await using var db = new ObjectionReadDbContext(connString);

        var row = await db.MvdNotices
            .AsNoTracking()
            .Where(x =>
                (!string.IsNullOrWhiteSpace(objectionNo) &&
                 x.ObjectionNo == objectionNo) ||
                (!string.IsNullOrWhiteSpace(valuationKey) &&
                 x.ValuationKey == valuationKey) ||
                (!string.IsNullOrWhiteSpace(unitKey) &&
                 x.UnitKey == unitKey) ||
                (!string.IsNullOrWhiteSpace(propertyDesc) &&
                 (x.PropertyDescription ?? string.Empty).Trim() == propertyDesc))
            .OrderByDescending(x => x.BatchDate)
            .FirstOrDefaultAsync();

        if (row == null)
        {
            return new LodgementWindowResult
            {
                Exists = false,
                IsOpen = false,
                Type = "Appeal"
            };
        }

        var reviseText = row.ReviseMvd ?? "";

        var isRevised =
            reviseText.Equals("True", StringComparison.OrdinalIgnoreCase)
            || reviseText.Equals("Yes", StringComparison.OrdinalIgnoreCase)
            || reviseText.Equals("1", StringComparison.OrdinalIgnoreCase)
            || row.RevisedAppealCloseDate.HasValue;

        DateTime? startDate = null;
        DateTime? closeDate = null;

        if (isRevised)
        {
            startDate = row.RevisedAppealStartDate;
            closeDate = row.RevisedAppealCloseDate;
        }

        if (!startDate.HasValue)
            startDate = row.AppealStartDate;

        if (!closeDate.HasValue)
            closeDate = row.AppealCloseDate;

        var today = TodaySa();

        var isOpen =
            startDate.HasValue &&
            closeDate.HasValue &&
            today >= startDate.Value.Date &&
            today <= closeDate.Value.Date;

        return new LodgementWindowResult
        {
            Exists = true,
            IsOpen = isOpen,
            Type = "Appeal",
            StartDate = startDate,
            CloseDate = closeDate,
            ReferenceNo = row.ObjectionNo
        };
    }

    private sealed class ObjectionReadDbContext : DbContext
    {
        private readonly string _connectionString;

        public ObjectionReadDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbSet<ObjectionReadEntity> Objections => Set<ObjectionReadEntity>();
        public DbSet<AppealReadEntity> Appeals => Set<AppealReadEntity>();
        public DbSet<MvdReadEntity> MvdNotices => Set<MvdReadEntity>();
        public DbSet<MvdRevisedReadEntity> MvdRevised => Set<MvdRevisedReadEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                _connectionString,
                sqlServer => sqlServer.CommandTimeout(60));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ObjectionReadEntity>(entity =>
            {
                entity.HasKey(x => x.ObjectionId);
                entity.ToTable("Obj_Property_Info", "dbo");
                entity.Property(x => x.ObjectionId).HasColumnName("Objection_ID");
                entity.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
                entity.Property(x => x.ObjectionStatus).HasColumnName("objection_Status");
                entity.Property(x => x.PropertyDescription).HasColumnName("Property_Desc");
                entity.Property(x => x.UnitKey).HasColumnName("Unit_key");
                entity.Property(x => x.ValuationKey).HasColumnName("Valuation_Key");
            });

            modelBuilder.Entity<AppealReadEntity>(entity =>
            {
                entity.HasKey(x => x.AppealId);
                entity.ToTable("Obj_Property_Info_Appeal", "dbo");
                entity.Property(x => x.AppealId).HasColumnName("Appeal_ID");
                entity.Property(x => x.AppealNo).HasColumnName("Appeal_No");
                entity.Property(x => x.AppealStatus).HasColumnName("Appeal_Status");
                entity.Property(x => x.ObjectReference).HasColumnName("Obj_Ref");
                entity.Property(x => x.PropertyDescription).HasColumnName("A_Property_Desc");
                entity.Property(x => x.UnitKey).HasColumnName("A_Unit_key");
                entity.Property(x => x.ValuationKey).HasColumnName("A_Valuation_Key");
            });

            var mvd = modelBuilder.Entity<MvdReadEntity>();
            mvd.HasNoKey();
            mvd.ToTable("Objection_MVD", "dbo");
            mvd.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
            mvd.Property(x => x.AppealStartDate).HasColumnName("Appeal_Start_Date");
            mvd.Property(x => x.AppealCloseDate).HasColumnName("Appeal_Close_Date");
            mvd.Property(x => x.RevisedAppealStartDate).HasColumnName("Appeal_Start_Date_ReviseMVD");
            mvd.Property(x => x.RevisedAppealCloseDate).HasColumnName("Appeal_Close_Date_ReviseMVD");
            mvd.Property(x => x.ReviseMvd).HasColumnName("Revise_MVD");
            mvd.Property(x => x.UnitKey).HasColumnName("Unit_Key");
            mvd.Property(x => x.ValuationKey).HasColumnName("valuation_Key");
            mvd.Property(x => x.PropertyDescription).HasColumnName("Property_desc");
            mvd.Property(x => x.BatchDate).HasColumnName("Batch_Date");

            var revised = modelBuilder.Entity<MvdRevisedReadEntity>();
            revised.HasNoKey();
            revised.ToTable("Objection_MVD1", "dbo");
            revised.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
            revised.Property(x => x.AppealStartDate).HasColumnName("Appeal_Start_Date");
            revised.Property(x => x.AppealCloseDate).HasColumnName("Appeal_Close_Date");
            revised.Property(x => x.RevisedAppealStartDate).HasColumnName("Appeal_Start_Date_ReviseMVD");
            revised.Property(x => x.RevisedAppealCloseDate).HasColumnName("Appeal_Close_Date_ReviseMVD");
            revised.Property(x => x.ReviseMvd).HasColumnName("Revise_MVD");
            revised.Property(x => x.UnitKey).HasColumnName("Unit_Key");
            revised.Property(x => x.ValuationKey).HasColumnName("valuation_Key");
            revised.Property(x => x.PropertyDescription).HasColumnName("Property_desc");
            revised.Property(x => x.BatchDate).HasColumnName("Batch_Date");
        }
    }

    private sealed class ObjectionReadEntity
    {
        public long ObjectionId { get; set; }
        public string? ObjectionNo { get; set; }
        public string? ObjectionStatus { get; set; }
        public string? PropertyDescription { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
    }

    private sealed class AppealReadEntity
    {
        public long AppealId { get; set; }
        public string? AppealNo { get; set; }
        public string? AppealStatus { get; set; }
        public string? ObjectReference { get; set; }
        public string? PropertyDescription { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
    }

    private sealed class MvdReadEntity
    {
        public string? ObjectionNo { get; set; }
        public DateTime? AppealStartDate { get; set; }
        public DateTime? AppealCloseDate { get; set; }
        public DateTime? RevisedAppealStartDate { get; set; }
        public DateTime? RevisedAppealCloseDate { get; set; }
        public string? ReviseMvd { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PropertyDescription { get; set; }
        public DateTime? BatchDate { get; set; }
    }

    private sealed class MvdRevisedReadEntity
    {
        public string? ObjectionNo { get; set; }
        public DateTime? AppealStartDate { get; set; }
        public DateTime? AppealCloseDate { get; set; }
        public DateTime? RevisedAppealStartDate { get; set; }
        public DateTime? RevisedAppealCloseDate { get; set; }
        public string? ReviseMvd { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PropertyDescription { get; set; }
        public DateTime? BatchDate { get; set; }
    }

    private sealed class AppealEligibilityRow
    {
        public string? ObjectionNo { get; set; }
        public string? ObjectionStatus { get; set; }
        public string? PropertyDescription { get; set; }
        public DateTime? AppealStartDate { get; set; }
        public DateTime? AppealCloseDate { get; set; }
        public DateTime? RevisedAppealStartDate { get; set; }
        public DateTime? RevisedAppealCloseDate { get; set; }
        public string? ReviseMvd { get; set; }
        public string? ExistingAppealNo { get; set; }
        public string? ExistingAppealStatus { get; set; }
    }
}
