// ═══════════════════════════════════════════════════════════════
//  Services/Implementations/AdminDashboardService.cs  — REPLACE FULL FILE
// ═══════════════════════════════════════════════════════════════
using Dapper;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq.Expressions;
using V2_Genesis.Models.Admin;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AdminDashboardService> _logger;
    private readonly ApplicationDbContext _db;

    public AdminDashboardService(
        IConfiguration config,
        ILogger<AdminDashboardService> logger,
        ApplicationDbContext db)
    {
        _config = config;
        _logger = logger;
        _db = db;
    }

    private SqlConnection GetConn(string rollSource)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            throw new InvalidOperationException($"Unknown roll: {rollSource}");
        var cs = _config.GetConnectionString(cfg.ConnectionKey)!;
        return new SqlConnection(cs);
    }

    private AdminRollDbContext GetRollDb(string rollSource)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            throw new InvalidOperationException($"Unknown roll: {rollSource}");

        var connectionString = _config.GetConnectionString(cfg.ConnectionKey)
            ?? throw new InvalidOperationException(
                $"Connection string '{cfg.ConnectionKey}' was not found.");

        var options = new DbContextOptionsBuilder<AdminRollDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AdminRollDbContext(options);
    }

    private AdminRollDbContext GetQueryDb()
    {
        var connectionString = _config.GetConnectionString("QueryConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'QueryConnection' was not found.");

        var options = new DbContextOptionsBuilder<AdminRollDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AdminRollDbContext(options);
    }

    // ── Stats ─────────────────────────────────────────────────────────
    public async Task<AdminRollStats> GetStatsAsync(string rollSource)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new AdminRollStats { RollSource = rollSource };

        try
        {
            await using var conn = GetConn(rollSource);
            var result = await conn.QueryFirstOrDefaultAsync<AdminRollStats>(
                cfg.StatsSp,
                commandType: CommandType.StoredProcedure);

            if (result is not null)
            {
                result.RollSource = rollSource;
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] Stats SP failed for {Roll}", rollSource);
        }

        return new AdminRollStats { RollSource = rollSource };
    }

    // ── Search Objections ─────────────────────────────────────────────
    public async Task<List<AdminObjectionResult>> SearchObjectionsAsync(
        string rollSource, string searchValue)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new();

        try
        {
            await using var conn = GetConn(rollSource);
            var results = await conn.QueryAsync<AdminObjectionResult>(
                cfg.ObjSearchSp,
                new { SearchValue = searchValue },
                commandType: CommandType.StoredProcedure);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] Search SP failed for {Roll}", rollSource);
            return new();
        }
    }

    // ── Search Appeals ────────────────────────────────────────────────
    public async Task<List<AdminAppealResult>> SearchAppealsAsync(
        string rollSource, string searchValue)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new();

        bool isAppeal = searchValue.Trim().ToUpper().StartsWith("APP");
        var sp = isAppeal ? cfg.AppSearchSp : cfg.PropSearchSp;

        try
        {
            await using var conn = GetConn(rollSource);
            var results = await conn.QueryAsync<AdminAppealResult>(
                sp,
                new { searchAppealValue = searchValue },
                commandType: CommandType.StoredProcedure);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] Appeal search SP failed for {Roll}", rollSource);
            return new();
        }
    }

    // ── All-users roll data for admin dashboard ───────────────────────
    // FIX 1: removed New_Market_Value_MVD / New_Category_MVD — not on Obj_Property_Info
    // FIX 2: added Appeals query
    public async Task<RollData> GetAllRollDataAsync(string rollSource)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new RollData();

        try
        {
            await using var rollDb = GetRollDb(rollSource);

            var objProps = await rollDb.Objections
                .AsNoTracking()
                .OrderByDescending(x => x.ObjectionNo)
                .Take(500)
                .Select(x => new ObjectedPropertyResult
                {
                    Objection_No = x.ObjectionNo,
                    Property_Desc = x.PropertyDescription,
                    Old_Category = x.OldCategory,
                    Town_Name = x.TownName,
                    Old_Market_Value = x.OldMarketValue,
                    objection_Status = x.ObjectionStatus,
                    Sub_typ = x.SubType,
                    Unit_key = x.UnitKey,
                    Valuation_Key = x.ValuationKey,
                    Property_Type = x.PropertyType,
                    PropertyFrom = x.PropertyFrom
                })
                .ToListAsync();

            var appealRows = await rollDb.Appeals
                .AsNoTracking()
                .OrderByDescending(x => x.AppealId)
                .Take(200)
                .ToListAsync();

            var now = DateTime.Now;
            var appeals = appealRows.Select(x =>
            {
                var status = x.AppealStatus?.Trim();
                var expiresAt = x.AppealStartDateTime?.AddHours(48);

                return new AppealResult
                {
                    Appeal_No = x.AppealNo,
                    A_Property_Desc = x.PropertyDescription,
                    Town_Name = x.TownName,
                    Old_Market_Value = x.OldMarketValue,
                    Old_Category = x.OldCategory,
                    A_Unit_key = x.UnitKey,
                    A_Valuation_Key = x.ValuationKey,
                    A_Property_Type = x.PropertyType,
                    Appeal_Status = status,
                    Appeal_Start_DateTime = x.AppealStartDateTime,
                    Evidence_Expires_At = expiresAt,
                    Evidence_Window_Open = expiresAt.HasValue &&
                        expiresAt.Value >= now &&
                        (string.Equals(status, "App-Lodging", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(status, "App-Unallocated", StringComparison.OrdinalIgnoreCase))
                };
            }).ToList();

            return new RollData
            {
                LinkedProperties = new(),    // admin doesn't link properties
                ObjectedProperties = objProps,
                Appeals = appeals,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] GetAllRollData failed for {Roll} — {Msg}",
                rollSource, ex.Message);
            return new RollData();
        }
    }

    // ── Unified search by reference number ───────────────────────────
    public async Task<AdminSearchResult> SearchByReferenceAsync(
        string refNo,
        string? rollSource)
    {
        var result = new AdminSearchResult
        {
            SearchType = "Reference",
            SearchInput = refNo,
            RollFilter = rollSource
        };

        if (string.IsNullOrWhiteSpace(refNo))
            return result;

        refNo = refNo.Trim();

        var isAppeal = LooksLikeAppeal(refNo);
        var isQuery = LooksLikeQuery(refNo);
        var isReview = LooksLikeReview(refNo);

        // Query / Review lives in QueryConnection
        if (isQuery || isReview)
        {
            await SearchQueryOrReviewReferenceAsync(result, refNo, isReview);
            return result;
        }

        var rolls = AdminRollRegistry.Configs
            .Where(kv => string.IsNullOrWhiteSpace(rollSource) || kv.Key == rollSource)
            .ToList();

        foreach (var (roll, cfg) in rolls)
        {
            try
            {
                await using var rollDb = GetRollDb(roll);

                if (isAppeal)
                {
                    var aRow = await rollDb.Appeals
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.AppealNo == refNo);

                    if (aRow is null)
                        continue;

                    var match = new AdminRefMatch
                    {
                        RollSource = roll,
                        RollName = RollName(roll),
                        SourceTable = RollSourceToSourceTable(roll),

                        RefType = "Appeal",
                        ReferenceNo = aRow.AppealNo,
                        Appeal_No = aRow.AppealNo,
                        Objection_No = aRow.ObjectionNo,

                        CurrentStatus = aRow.AppealStatus,

                        Property_Desc = aRow.PropertyDescription,
                        Property_Type = aRow.PropertyType,
                        Town_Name = aRow.TownName,
                        Old_Category = aRow.OldCategory,
                        Old_Market_Value = aRow.OldMarketValue,

                        Unit_key = aRow.UnitKey,
                        Valuation_Key = aRow.ValuationKey,
                        PremiseId = aRow.PremiseId,
                        UserId = FirstNonEmptyDynamic(
                            aRow.UserId,
                            await ResolveObjectionUserIdAsync(
                                rollDb,
                                aRow.ObjectionReference
                                ?? aRow.ObjectionNo)),

                        IsThirdParty = aRow.ObjectorType
                            ?.Contains("Third", StringComparison.OrdinalIgnoreCase) == true,

                        IsRepresentative = aRow.ObjectorType
                            ?.Contains("Representative", StringComparison.OrdinalIgnoreCase) == true
                    };

                    match.Notices = BuildNoticeOptions(match);
                    await PopulateClientAccountAsync(match);

                    result.RefMatches.Add(match);
                }
                else
                {
                    var oRow = await (
                        from objection in rollDb.Objections.AsNoTracking()
                        join section in rollDb.Section6.AsNoTracking()
                            on objection.ObjectionNo equals section.ObjectionReference
                        where objection.ObjectionNo == refNo
                        select new { objection, section })
                        .FirstOrDefaultAsync();

                    if (oRow is null)
                        continue;

                    var match = new AdminRefMatch
                    {
                        RollSource = roll,
                        RollName = RollName(roll),
                        SourceTable = RollSourceToSourceTable(roll),

                        RefType = "Objection",
                        ReferenceNo = oRow.objection.ObjectionNo,
                        Objection_No = oRow.objection.ObjectionNo,

                        CurrentStatus = oRow.objection.ObjectionStatus,

                        Property_Desc = oRow.objection.PropertyDescription,
                        Property_Type = oRow.objection.PropertyType,
                        Town_Name = oRow.objection.TownName,
                        Old_Category = oRow.section.OldCategory,
                        Old_Market_Value = oRow.section.OldMarketValue,

                        Unit_key = oRow.objection.UnitKey,
                        Valuation_Key = oRow.objection.ValuationKey,
                        PremiseId = oRow.objection.PremiseId,
                        PropertyFrom = oRow.objection.PropertyFrom,
                        UserId = oRow.objection.UserId,

                        IsThirdParty = oRow.objection.ObjectorType
                            ?.Contains("Third", StringComparison.OrdinalIgnoreCase) == true,

                        IsRepresentative = oRow.objection.ObjectorType
                            ?.Contains("Representative", StringComparison.OrdinalIgnoreCase) == true
                    };

                    match.Notices = BuildNoticeOptions(match);
                    await PopulateClientAccountAsync(match);

                    result.RefMatches.Add(match);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AdminSearch] Reference search failed. Roll={Roll}, Ref={Ref}",
                    roll,
                    refNo);
            }
        }

        return result;
    }

    // ── Unified search by property attributes ────────────────────────
    // FIX 3: Town_Name (was TownName — wrong column name)
    public async Task<AdminSearchResult> SearchByPropertyAsync(
      string? town,
      string? stand,
      string? address,
      string? scheme,
      string? unit,
      string? rollSource)
    {
        var result = new AdminSearchResult
        {
            SearchType = "Property",
            SearchInput = string.Join(" ",
                new[] { town, stand, address, scheme, unit }
                    .Where(v => !string.IsNullOrWhiteSpace(v))),
            RollFilter = rollSource
        };

        string RollName(string src) => src switch
        {
            "Objection" => "GV 2023",
            "Objection_Supp1" => "Supp 1",
            "Objection_Supp2" => "Supp 2",
            "Objection_Supp3" => "Supp 3",
            "Objection_Supp4" => "Supp 4",
            "Objection_Supp5" => "Supp 5",
            _ => src
        };

        var rolls = AdminRollRegistry.Configs
            .Where(kv => string.IsNullOrWhiteSpace(rollSource) || kv.Key == rollSource)
            .ToList();

        foreach (var (roll, cfg) in rolls)
        {
            try
            {
                await using var rollDb = GetRollDb(roll);

                var searchPatterns = BuildPropertyDescriptionSearchPatterns(
                    town,
                    stand,
                    address,
                    scheme,
                    unit);

                var predicate = BuildPropertyDescriptionPredicate(searchPatterns);

                var rows = await (
                    from objection in rollDb.Objections.AsNoTracking()
                        .Where(predicate)
                    join section in rollDb.Section6.AsNoTracking()
                        on objection.ObjectionNo equals section.ObjectionReference
                    orderby objection.ObjectionNo descending
                    select new { objection, section })
                    .Take(100)
                    .ToListAsync();

                foreach (var r in rows)
                {
                    var propertyMatch = new AdminPropMatch
                    {
                        RollSource = roll,
                        RollName = RollName(roll),
                        Objection_No = r.objection.ObjectionNo,
                        Property_Desc = r.objection.PropertyDescription,

                        // We do not have Town_Name column. Pull town from Property_Desc.
                        Town_Name = string.IsNullOrWhiteSpace(r.objection.TownName)
                            ? ExtractTownFromPropertyDesc(r.objection.PropertyDescription)
                            : r.objection.TownName,

                        Old_Category = r.section.OldCategory,
                        Old_Market_Value = r.section.OldMarketValue,
                        objection_Status = r.objection.ObjectionStatus,
                        Sub_typ = r.objection.SubType,
                        Unit_key = r.objection.UnitKey,
                        Valuation_Key = r.objection.ValuationKey,
                        PropertyFrom = r.objection.PropertyFrom,
                        UserId = r.objection.UserId,
                    };

                    await PopulateClientAccountAsync(propertyMatch);
                    result.PropMatches.Add(propertyMatch);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AdminSearch] PropSearch failed for {Roll}", roll);
            }
        }

        return result;
    }

    private static Expression<Func<AdminObjectionRow, bool>>
        BuildPropertyDescriptionPredicate(IReadOnlyCollection<string> patterns)
    {
        if (patterns.Count == 0)
            return _ => true;

        var row = Expression.Parameter(typeof(AdminObjectionRow), "row");
        var propertyDescription = Expression.Property(
            row,
            nameof(AdminObjectionRow.PropertyDescription));

        Expression body = Expression.Constant(false);
        var notNull = Expression.NotEqual(
            propertyDescription,
            Expression.Constant(null, typeof(string)));

        var containsMethod = typeof(string).GetMethod(
            nameof(string.Contains),
            new[] { typeof(string) })!;

        foreach (var pattern in patterns)
        {
            var contains = Expression.Call(
                propertyDescription,
                containsMethod,
                Expression.Constant(pattern));

            body = Expression.OrElse(
                body,
                Expression.AndAlso(notNull, contains));
        }

        return Expression.Lambda<Func<AdminObjectionRow, bool>>(body, row);
    }

    private static List<string> BuildPropertyDescriptionSearchPatterns(
       string? town,
       string? stand,
       string? address,
       string? scheme,
       string? unit)
    {
        var patterns = new List<string>();

        town = CleanSearchText(town);
        stand = CleanSearchText(stand);
        address = CleanSearchText(address);
        scheme = CleanSearchText(scheme);
        unit = CleanSearchText(unit);

        /*
            Pattern 1:
            Full Title ERF 334 LINBRO PARK EXT.181
        */
        if (!string.IsNullOrWhiteSpace(stand) &&
            !string.IsNullOrWhiteSpace(town))
        {
            patterns.Add($"FULL TITLE ERF {stand} {town}");
            patterns.Add($"ERF {stand} {town}");
            patterns.Add($"{stand} {town}");
        }

        /*
            Pattern 2:
            PORTION 42 RUIMSIG 265-IQ
        */
        if (!string.IsNullOrWhiteSpace(stand) &&
            !string.IsNullOrWhiteSpace(town))
        {
            patterns.Add($"PORTION {stand} {town}");
            patterns.Add($"PTN {stand} {town}");
        }

        /*
            Pattern 3:
            RE PORTION 3 LANGLAAGTE 224-IQ
        */
        if (!string.IsNullOrWhiteSpace(stand) &&
            !string.IsNullOrWhiteSpace(town))
        {
            patterns.Add($"RE PORTION {stand} {town}");
            patterns.Add($"RE OF PORTION {stand} {town}");
            patterns.Add($"REMAINDER PORTION {stand} {town}");
            patterns.Add($"REMAINDER OF PORTION {stand} {town}");
        }

        /*
            Pattern 4:
            Scheme UNIT 28, MULBARTON GARDENS, (556/2024), BEVERLEY EXT.100
        */
        if (!string.IsNullOrWhiteSpace(unit) &&
            !string.IsNullOrWhiteSpace(scheme) &&
            !string.IsNullOrWhiteSpace(town))
        {
            patterns.Add($"SCHEME UNIT {unit} {scheme} {town}");
            patterns.Add($"UNIT {unit} {scheme} {town}");
            patterns.Add($"{unit} {scheme} {town}");
        }

        if (!string.IsNullOrWhiteSpace(unit) &&
            !string.IsNullOrWhiteSpace(scheme))
        {
            patterns.Add($"SCHEME UNIT {unit} {scheme}");
            patterns.Add($"UNIT {unit} {scheme}");
            patterns.Add($"{unit} {scheme}");
        }

        if (!string.IsNullOrWhiteSpace(scheme) &&
            !string.IsNullOrWhiteSpace(town))
        {
            patterns.Add($"{scheme} {town}");
        }

        /*
            Address fallback
        */
        if (!string.IsNullOrWhiteSpace(address))
        {
            patterns.Add(address);

            if (!string.IsNullOrWhiteSpace(town))
                patterns.Add($"{address} {town}");
        }

        /*
            Town-only fallback
        */
        if (!string.IsNullOrWhiteSpace(town))
        {
            patterns.Add(town);
        }

        /*
            Stand-only fallback
        */
        if (!string.IsNullOrWhiteSpace(stand))
        {
            patterns.Add($"ERF {stand}");
            patterns.Add($"PORTION {stand}");
            patterns.Add($"PTN {stand}");
            patterns.Add($"RE PORTION {stand}");
            patterns.Add(stand);
        }

        return patterns
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string CleanSearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return value
            .Trim()
            .Replace(",", " ")
            .Replace("  ", " ");
    }

    private static string ExtractTownFromPropertyDesc(string? propertyDesc)
    {
        if (string.IsNullOrWhiteSpace(propertyDesc))
            return "";

        var text = propertyDesc.Trim();

        // Example:
        // Scheme UNIT 28, MULBARTON GARDENS, (556/2024), BEVERLEY EXT.100
        if (text.Contains(','))
        {
            var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (parts.Any())
                return parts.Last();
        }

        // Example:
        // Full Title ERF 334 LINBRO PARK EXT.181
        var erfIndex = text.IndexOf("ERF ", StringComparison.OrdinalIgnoreCase);

        if (erfIndex >= 0)
        {
            var afterErf = text[(erfIndex + 4)..].Trim();
            var pieces = afterErf.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (pieces.Count > 1)
                return string.Join(" ", pieces.Skip(1));
        }

        return "";
    }

    private static string RollSourceToSourceTable(string rollSource)
    {
        return rollSource switch
        {
            "Objection" => "GV23",
            "Objection_Supp1" => "GV23-SUP1",
            "Objection_Supp2" => "GV23-SUP2",
            "Objection_Supp3" => "GV23-SUP3",
            "Objection_Supp4" => "GV23-SUP4",
            "Objection_Supp5" => "GV23-SUP5",
            "Objection_Query" => "Query",
            _ => rollSource
        };
    }

    private static string RollName(string rollSource)
    {
        return rollSource switch
        {
            "Objection" => "General Valuation Roll 2023",
            "Objection_Supp1" => "Supplementary Roll 1",
            "Objection_Supp2" => "Supplementary Roll 2",
            "Objection_Supp3" => "Supplementary Roll 3",
            "Objection_Supp4" => "Supplementary Roll 4",
            "Objection_Supp5" => "Supplementary Roll 5",
            "Objection_Query" => "Section 78 Query / Review",
            _ => rollSource
        };
    }

    private static bool LooksLikeAppeal(string refNo)
    {
        var value = refNo.Trim().ToUpperInvariant();
        return value.StartsWith("APP") || value.Contains("APP-");
    }

    private static bool LooksLikeQuery(string refNo)
    {
        var value = refNo.Trim().ToUpperInvariant();
        return value.StartsWith("QUE") ||
               value.StartsWith("QUERY") ||
               value.Contains("QUERY");
    }

    private static bool LooksLikeReview(string refNo)
    {
        var value = refNo.Trim().ToUpperInvariant();
        return value.StartsWith("REV") ||
               value.StartsWith("REVIEW") ||
               value.Contains("REVIEW");
    }
    private static List<AdminNoticeOption> BuildNoticeOptions(AdminRefMatch match)
    {
        var notices = new List<AdminNoticeOption>();

        var refNo = match.ReferenceNo
            ?? match.Objection_No
            ?? match.Appeal_No
            ?? match.Query_No
            ?? "";

        var status = match.CurrentStatus ?? "";

        // View submitted form
        notices.Add(new AdminNoticeOption
        {
            NoticeName = "View Submitted Form",
            Url = match.Property_Type?.Equals("Multi", StringComparison.OrdinalIgnoreCase) == true
                ? $"/objection/multipurpose-details?referenceNo={refNo}&rollSource={match.RollSource}"
                : $"/objection/form-details?referenceNo={refNo}&rollSource={match.RollSource}",
            IsAvailable = true,
            Icon = "fa-eye"
        });

        // Acknowledgement from saved folder
        notices.Add(new AdminNoticeOption
        {
            NoticeName = "Acknowledgement",
            Url = $"/notice/acknowledgement/download?objectionNo={refNo}&rollSource={match.RollSource}",
            IsAvailable = true,
            Icon = "fa-file-pdf"
        });

        // Section 49
        notices.Add(new AdminNoticeOption
        {
            NoticeName = "Section 49 Notice",
            Url = $"/notice/section49/download?rollSource={match.RollSource}&unitKey={match.Unit_key}&valuationKey={match.Valuation_Key}",
            IsAvailable = !string.IsNullOrWhiteSpace(match.Unit_key) ||
                          !string.IsNullOrWhiteSpace(match.Valuation_Key),
            ReasonUnavailable = "Section 49 can only be downloaded when the property is found on the roll.",
            Icon = "fa-file-pdf"
        });

        // Section 51
        notices.Add(new AdminNoticeOption
        {
            NoticeName = "Section 51 Notice",
            Url = $"/section51/download?referenceNo={refNo}&rollSource={match.RollSource}",
            IsAvailable = match.IsThirdParty || match.IsRepresentative,
            ReasonUnavailable = "Section 51 is only applicable where the case requires third-party or representative handling.",
            Icon = "fa-file-pdf"
        });

        // Section 53
        notices.Add(new AdminNoticeOption
        {
            NoticeName = "Section 53 Notice",
            Url = $"/{RollSourceToController(match.RollSource)}/DownloadSection53?ObjectionNum={refNo}",
            IsAvailable = status.Equals("Notice-Sent", StringComparison.OrdinalIgnoreCase) ||
                          status.Equals("Appeal-Closed", StringComparison.OrdinalIgnoreCase),
            ReasonUnavailable = "Section 53 is only available after Notice-Sent.",
            Icon = "fa-file-pdf"
        });

        // Appeal decision
        notices.Add(new AdminNoticeOption
        {
            NoticeName = "Appeal Decision / Section 52",
            Url = $"/{RollSourceToController(match.RollSource)}/DownloadAppeal?ObjectionNum={refNo}",
            IsAvailable = status.Equals("App-Finalized", StringComparison.OrdinalIgnoreCase),
            ReasonUnavailable = "Appeal decision is only available after App-Finalized.",
            Icon = "fa-gavel"
        });

        return notices;
    }

    private static string RollSourceToController(string rollSource)
    {
        return rollSource switch
        {
            "Objection" => "Objection",
            "Objection_Supp1" => "Sup1",
            "Objection_Supp2" => "Sup2",
            "Objection_Supp3" => "Sup3",
            "Objection_Supp4" => "Sup4",
            "Objection_Supp5" => "Sup5",
            "Objection_Query" => "Query",
            _ => "Objection"
        };
    }
    private async Task PopulateClientAccountAsync(AdminRefMatch match)
    {
        var client = await ResolveClientAccountAsync(match.UserId);
        if (client is null) return;

        match.UserId = client.UserId;
        match.ClientDisplayName = client.DisplayName;
        match.ClientEmail = client.Email;
        match.ClientPhoneNumber = client.PhoneNumber;
        match.ClientAccountType = client.AccountType;
        match.ClientAccountResolved = true;
    }

    private async Task PopulateClientAccountAsync(AdminPropMatch match)
    {
        var client = await ResolveClientAccountAsync(match.UserId);
        if (client is null) return;

        match.UserId = client.UserId;
        match.ClientDisplayName = client.DisplayName;
        match.ClientEmail = client.Email;
        match.ClientPhoneNumber = client.PhoneNumber;
        match.ClientAccountType = client.AccountType;
        match.ClientAccountResolved = true;
    }

    private async Task<ResolvedClientAccount?> ResolveClientAccountAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var cleanUserId = userId.Trim();

        var user = await _db.Users.AsNoTracking()
            .Where(x => x.Id == cleanUserId)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.PhoneNumber,
                x.FirstName,
                x.LastName,
                x.CompanyName,
                x.CompanyRegistration
            })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            _logger.LogWarning(
                "[AdminSearch] Submission UserID {UserId} was not found in AspNetUsers.",
                cleanUserId);
            return null;
        }

        var isCompany = !string.IsNullOrWhiteSpace(user.CompanyName)
            || !string.IsNullOrWhiteSpace(user.CompanyRegistration);

        var displayName = isCompany
            ? user.CompanyName?.Trim()
            : string.Join(" ", new[] { user.FirstName, user.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

        return new ResolvedClientAccount(
            user.Id,
            string.IsNullOrWhiteSpace(displayName) ? user.Email ?? "Client" : displayName,
            user.Email ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            isCompany ? "Company" : "Individual");
    }

    private static async Task<string> ResolveObjectionUserIdAsync(
        AdminRollDbContext rollDb, string? objectionReference)
    {
        if (string.IsNullOrWhiteSpace(objectionReference)) return string.Empty;

        var reference = objectionReference.Trim();

        return (await rollDb.Objections
            .AsNoTracking()
            .Where(x => x.ObjectionNo != null &&
                        x.ObjectionNo.Trim() == reference)
            .Select(x => x.UserId)
            .FirstOrDefaultAsync())?.Trim() ?? string.Empty;
    }

    private static string FirstNonEmptyDynamic(params object?[] values) =>
        values.Select(value => value?.ToString()?.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
        ?? string.Empty;

    private sealed record ResolvedClientAccount(
        string UserId, string DisplayName, string Email,
        string PhoneNumber, string AccountType);

    private async Task SearchQueryOrReviewReferenceAsync(
    AdminSearchResult result,
    string refNo,
    bool isReview)
    {
        try
        {
            await using var queryDb = GetQueryDb();

            var qRow = isReview
                ? await queryDb.Queries.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ReviewNo == refNo)
                : await queryDb.Queries.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.QueryNo == refNo);

            if (qRow is null)
                return;

            var match = new AdminRefMatch
            {
                RollSource = "Objection_Query",
                RollName = "Section 78 Query / Review",
                SourceTable = "Query",

                RefType = isReview ? "Review" : "Query",
                ReferenceNo = isReview
                    ? qRow.ReviewNo
                    : qRow.QueryNo,

                Query_No = qRow.QueryNo,
              

                CurrentStatus = qRow.QueryStatus,

                Property_Desc = qRow.PropertyDescription,
                Property_Type = qRow.PropertyType,
                Town_Name = qRow.TownName,
                Old_Category = qRow.OldCategory,
                Old_Market_Value = qRow.OldMarketValue,

                Unit_key = qRow.UnitKey,
                Valuation_Key = qRow.ValuationKey,
                PremiseId = qRow.PremiseId,
                UserId = qRow.UserId
            };

            match.Notices = BuildNoticeOptions(match);
            await PopulateClientAccountAsync(match);

            result.RefMatches.Add(match);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AdminSearch] Query/Review reference search failed. Ref={Ref}",
                refNo);
        }
    }

    // Read-only EF model for roll databases. It is intentionally private so
    // the security hardening does not alter the application's shared entities.
    private sealed class AdminRollDbContext : DbContext
    {
        public AdminRollDbContext(DbContextOptions<AdminRollDbContext> options)
            : base(options) { }

        public DbSet<AdminObjectionRow> Objections => Set<AdminObjectionRow>();
        public DbSet<AdminAppealRow> Appeals => Set<AdminAppealRow>();
        public DbSet<AdminSection6Row> Section6 => Set<AdminSection6Row>();
        public DbSet<AdminQueryRow> Queries => Set<AdminQueryRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AdminObjectionRow>(entity =>
            {
                entity.ToTable("Obj_Property_Info", "dbo");
                entity.HasKey(x => x.ObjectionId);
                entity.Property(x => x.ObjectionId).HasColumnName("Objection_ID");
                entity.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
                entity.Property(x => x.PropertyDescription).HasColumnName("Property_Desc");
                entity.Property(x => x.PropertyType).HasColumnName("Property_Type");
                entity.Property(x => x.TownName).HasColumnName("Town_Name");
                entity.Property(x => x.OldCategory).HasColumnName("Old_Category");
                entity.Property(x => x.OldMarketValue).HasColumnName("Old_Market_Value");
                entity.Property(x => x.ObjectionStatus).HasColumnName("objection_Status");
                entity.Property(x => x.SubType).HasColumnName("Sub_typ");
                entity.Property(x => x.UnitKey).HasColumnName("Unit_key");
                entity.Property(x => x.ValuationKey).HasColumnName("Valuation_Key");
                entity.Property(x => x.PremiseId).HasColumnName("Premise_id");
                entity.Property(x => x.PropertyFrom).HasColumnName("PropertyFrom");
                entity.Property(x => x.UserId).HasColumnName("UserID");
                entity.Property(x => x.ObjectorType).HasColumnName("Objector_Type");
            });

            modelBuilder.Entity<AdminAppealRow>(entity =>
            {
                entity.ToTable("Obj_Property_Info_Appeal", "dbo");
                entity.HasKey(x => x.AppealId);
                entity.Property(x => x.AppealId).HasColumnName("Appeal_ID");
                entity.Property(x => x.AppealNo).HasColumnName("Appeal_No");
                entity.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
                entity.Property(x => x.ObjectionReference).HasColumnName("Obj_Ref");
                entity.Property(x => x.PropertyDescription).HasColumnName("A_Property_Desc");
                entity.Property(x => x.PropertyType).HasColumnName("A_Property_Type");
                entity.Property(x => x.TownName).HasColumnName("Town_Name");
                entity.Property(x => x.OldCategory).HasColumnName("Old_Category");
                entity.Property(x => x.OldMarketValue).HasColumnName("Old_Market_Value");
                entity.Property(x => x.AppealStatus).HasColumnName("Appeal_Status");
                entity.Property(x => x.AppealStartDateTime).HasColumnName("Appeal_Start_DateTime");
                entity.Property(x => x.UnitKey).HasColumnName("A_Unit_key");
                entity.Property(x => x.ValuationKey).HasColumnName("A_Valuation_Key");
                entity.Property(x => x.PremiseId).HasColumnName("PremiseID");
                entity.Property(x => x.UserId).HasColumnName("A_UserID");
                entity.Property(x => x.ObjectorType).HasColumnName("Objector_Type");
            });

            modelBuilder.Entity<AdminSection6Row>(entity =>
            {
                entity.ToTable("Obj_Section6", "dbo");
                entity.HasNoKey();
                entity.Property(x => x.ObjectionReference).HasColumnName("Objection_Ref_S6");
                entity.Property(x => x.OldCategory).HasColumnName("Old_Category");
                entity.Property(x => x.OldMarketValue).HasColumnName("Old_Market_Value");
            });

            modelBuilder.Entity<AdminQueryRow>(entity =>
            {
                entity.ToTable("Que_Property_Info", "dbo");
                entity.HasKey(x => x.QueryId);
                entity.Property(x => x.QueryId).HasColumnName("Query_ID");
                entity.Property(x => x.QueryNo).HasColumnName("Query_No");
          
                entity.Property(x => x.PropertyDescription).HasColumnName("Property_Desc");
                entity.Property(x => x.PropertyType).HasColumnName("Property_Type");
                entity.Property(x => x.TownName).HasColumnName("Town_Name");
                entity.Property(x => x.OldCategory).HasColumnName("Old_Category");
                entity.Property(x => x.OldMarketValue).HasColumnName("Old_Market_Value");
                entity.Property(x => x.QueryStatus).HasColumnName("Query_Status");
                entity.Property(x => x.UnitKey).HasColumnName("Unit_key");
                entity.Property(x => x.ValuationKey).HasColumnName("Valuation_Key");
                entity.Property(x => x.PremiseId).HasColumnName("Premise_id");
                entity.Property(x => x.UserId).HasColumnName("UserID");
            });
        }
    }

    private sealed class AdminObjectionRow
    {
        public long ObjectionId { get; set; }
        public string? ObjectionNo { get; set; }
        public string? PropertyDescription { get; set; }
        public string? PropertyType { get; set; }
        public string? TownName { get; set; }
        public string? OldCategory { get; set; }
        public string? OldMarketValue { get; set; }
        public string? ObjectionStatus { get; set; }
        public int SubType { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PremiseId { get; set; }
        public string? PropertyFrom { get; set; }
        public string? UserId { get; set; }
        public string? ObjectorType { get; set; }
    }

    private sealed class AdminAppealRow
    {
        public long AppealId { get; set; }
        public string? AppealNo { get; set; }
        public string? ObjectionNo { get; set; }
        public string? ObjectionReference { get; set; }
        public string? PropertyDescription { get; set; }
        public string? PropertyType { get; set; }
        public string? TownName { get; set; }
        public string? OldCategory { get; set; }
        public string? OldMarketValue { get; set; }
        public string? AppealStatus { get; set; }
        public DateTime? AppealStartDateTime { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PremiseId { get; set; }
        public string? UserId { get; set; }
        public string? ObjectorType { get; set; }
    }

    private sealed class AdminSection6Row
    {
        public string? ObjectionReference { get; set; }
        public string? OldCategory { get; set; }
        public string? OldMarketValue { get; set; }
    }

    private sealed class AdminQueryRow
    {
        public long QueryId { get; set; }
        public string? QueryNo { get; set; }
        public string? ReviewNo { get; set; }
        public string? PropertyDescription { get; set; }
        public string? PropertyType { get; set; }
        public string? TownName { get; set; }
        public string? OldCategory { get; set; }
        public string? OldMarketValue { get; set; }
        public string? QueryStatus { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PremiseId { get; set; }
        public string? UserId { get; set; }
    }
}
