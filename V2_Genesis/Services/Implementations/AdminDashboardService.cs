// ═══════════════════════════════════════════════════════════════
//  Services/Implementations/AdminDashboardService.cs  — REPLACE FULL FILE
// ═══════════════════════════════════════════════════════════════
using Dapper;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using Microsoft.Data.SqlClient;
using System.Data;
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
            var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
            await using var conn = new SqlConnection(connStr);

            // ── All objections (no UserID filter) ──────────────────
            var objRows = await conn.QueryAsync(
                @"SELECT TOP 500
                    Objection_No,
                    Property_Desc,
                    Old_Category,
                    Town_Name,
                    Old_Market_Value,
                    objection_Status,
                    Sub_typ,
                    Unit_key,
                    Valuation_Key,
                    Property_Type,
                    PropertyFrom
                  FROM dbo.Obj_Property_Info
                  ORDER BY Objection_No DESC");

            var objProps = objRows.Select(r => new ObjectedPropertyResult
            {
                Objection_No = r.Objection_No?.ToString(),
                Property_Desc = r.Property_Desc?.ToString(),
                Old_Category = r.Old_Category?.ToString(),
                Town_Name = r.Town_Name?.ToString(),
                Old_Market_Value = r.Old_Market_Value?.ToString(),
                objection_Status = r.objection_Status?.ToString(),
                Sub_typ = Convert.ToInt32(r.Sub_typ ?? 0),
                Unit_key = r.Unit_key?.ToString(),
                Valuation_Key = r.Valuation_Key?.ToString(),
                Property_Type = r.Property_Type?.ToString(),
                PropertyFrom = r.PropertyFrom?.ToString(),
            }).ToList();

            // ── All appeals (no UserID filter) ─────────────────────
            var appRows = await conn.QueryAsync(
                @"SELECT TOP 200
                    Appeal_No,
                    A_Property_Desc,
                    Town_Name,
                    Old_Market_Value,
                    Old_Category,
                    A_Unit_key,
                    A_Valuation_Key,
                    A_Property_Type,
                    Appeal_Status
                  FROM dbo.Obj_Property_Info_Appeal
                  ORDER BY Appeal_No DESC");

            var appeals = appRows.Select(r => new AppealResult
            {
                Appeal_No = r.Appeal_No?.ToString(),
                A_Property_Desc = r.A_Property_Desc?.ToString(),
                Town_Name = r.Town_Name?.ToString(),
                Old_Market_Value = r.Old_Market_Value?.ToString(),
                Old_Category = r.Old_Category?.ToString(),
                A_Unit_key = r.A_Unit_key?.ToString(),
                A_Valuation_Key = r.A_Valuation_Key?.ToString(),
                A_Property_Type = r.A_Property_Type?.ToString(),
                Appeal_Status = r.Appeal_Status?.ToString(),
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
                var connStr = _config.GetConnectionString(cfg.ConnectionKey);

                if (string.IsNullOrWhiteSpace(connStr))
                    continue;

                await using var conn = new SqlConnection(connStr);

                if (isAppeal)
                {
                    var aRow = await conn.QueryFirstOrDefaultAsync(
                        """
                    SELECT TOP 1
                        Appeal_No,
                        Objection_No,
                        A_Property_Desc,
                        A_Property_Type,
                        Town_Name,
                        Old_Market_Value,
                        Old_Category,
                        Appeal_Status,
                        A_Unit_key,
                        A_Valuation_Key,
                        PremiseID,
                        A_UserID,
                        Obj_Ref,
                        Objector_Type
                    FROM dbo.Obj_Property_Info_Appeal
                    WHERE Appeal_No = @Ref
                    """,
                        new { Ref = refNo });

                    if (aRow is null)
                        continue;

                    var match = new AdminRefMatch
                    {
                        RollSource = roll,
                        RollName = RollName(roll),
                        SourceTable = RollSourceToSourceTable(roll),

                        RefType = "Appeal",
                        ReferenceNo = aRow.Appeal_No?.ToString(),
                        Appeal_No = aRow.Appeal_No?.ToString(),
                        Objection_No = aRow.Objection_No?.ToString(),

                        CurrentStatus = aRow.Appeal_Status?.ToString(),

                        Property_Desc = aRow.A_Property_Desc?.ToString(),
                        Property_Type = aRow.A_Property_Type?.ToString(),
                        Town_Name = aRow.Town_Name?.ToString(),
                        Old_Category = aRow.Old_Category?.ToString(),
                        Old_Market_Value = aRow.Old_Market_Value?.ToString(),

                        Unit_key = aRow.A_Unit_key?.ToString(),
                        Valuation_Key = aRow.A_Valuation_Key?.ToString(),
                        PremiseId = aRow.PremiseID?.ToString(),
                        UserId = FirstNonEmptyDynamic(
                            aRow.A_UserID,
                            await ResolveObjectionUserIdAsync(
                                conn,
                                aRow.Obj_Ref?.ToString()
                                ?? aRow.Objection_No?.ToString())),

                        IsThirdParty = aRow.Objector_Type?.ToString()
                            ?.Contains("Third", StringComparison.OrdinalIgnoreCase) == true,

                        IsRepresentative = aRow.Objector_Type?.ToString()
                            ?.Contains("Representative", StringComparison.OrdinalIgnoreCase) == true
                    };

                    match.Notices = BuildNoticeOptions(match);
                    await PopulateClientAccountAsync(match);

                    result.RefMatches.Add(match);
                }
                else
                {
                    var oRow = await conn.QueryFirstOrDefaultAsync(
                        """
                    SELECT TOP 1
                        Objection_No,
                        Property_Desc,
                        Property_Type,
                       
                        Obj_Section6.Old_Category,
                        Obj_Section6.Old_Market_Value,
                        objection_Status,
                        Unit_key,
                        Valuation_Key,
                        Premise_id,
                        PropertyFrom,
                        UserID,
                        Objector_Type
                    FROM dbo.Obj_Property_Info a
                    inner join Obj_Section6 on a.Objection_No = Obj_Section6.[Objection_Ref_S6]

                    WHERE Objection_No = @Ref
                    """,
                        new { Ref = refNo });

                    if (oRow is null)
                        continue;

                    var match = new AdminRefMatch
                    {
                        RollSource = roll,
                        RollName = RollName(roll),
                        SourceTable = RollSourceToSourceTable(roll),

                        RefType = "Objection",
                        ReferenceNo = oRow.Objection_No?.ToString(),
                        Objection_No = oRow.Objection_No?.ToString(),

                        CurrentStatus = oRow.objection_Status?.ToString(),

                        Property_Desc = oRow.Property_Desc?.ToString(),
                        Property_Type = oRow.Property_Type?.ToString(),
                        Town_Name = oRow.Town_Name?.ToString(),
                        Old_Category = oRow.Old_Category?.ToString(),
                        Old_Market_Value = oRow.Old_Market_Value?.ToString(),

                        Unit_key = oRow.Unit_key?.ToString(),
                        Valuation_Key = oRow.Valuation_Key?.ToString(),
                        PremiseId = oRow.Premise_id?.ToString(),
                        PropertyFrom = oRow.PropertyFrom?.ToString(),
                        UserId = oRow.UserID?.ToString(),

                        IsThirdParty = oRow.Objector_Type?.ToString()
                            ?.Contains("Third", StringComparison.OrdinalIgnoreCase) == true,

                        IsRepresentative = oRow.Objector_Type?.ToString()
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
                var connStr = _config.GetConnectionString(cfg.ConnectionKey);

                if (string.IsNullOrWhiteSpace(connStr))
                {
                    _logger.LogWarning(
                        "[AdminSearch] Connection string missing for {Roll}. ConnKey={ConnKey}",
                        roll,
                        cfg.ConnectionKey);

                    continue;
                }

                await using var conn = new SqlConnection(connStr);

                var conditions = new List<string> { "1=1" };
                var parms = new DynamicParameters();

                var searchPatterns = BuildPropertyDescriptionSearchPatterns(
                    town,
                    stand,
                    address,
                    scheme,
                    unit);

                if (searchPatterns.Any())
                {
                    var descConditions = new List<string>();

                    for (var i = 0; i < searchPatterns.Count; i++)
                    {
                        var paramName = $"PropDesc{i}";
                        descConditions.Add($"a.Property_Desc LIKE @{paramName}");
                        parms.Add(paramName, $"%{searchPatterns[i]}%");
                    }

                    conditions.Add("(" + string.Join(" OR ", descConditions) + ")");
                }

                var sql = $@"
                select TOP 100 * 
                         
                  FROM dbo.Obj_Property_Info a
                 inner join Obj_Section6 b on a.Objection_No=b.Objection_Ref_S6
                WHERE {string.Join(" AND ", conditions)}
                ORDER BY a.Objection_No DESC;";

                var rows = await conn.QueryAsync(sql, parms);

                foreach (var r in rows)
                {
                    var propertyMatch = new AdminPropMatch
                    {
                        RollSource = roll,
                        RollName = RollName(roll),
                        Objection_No = r.Objection_No?.ToString(),
                        Property_Desc = r.Property_Desc?.ToString(),

                        // We do not have Town_Name column. Pull town from Property_Desc.
                        Town_Name = ExtractTownFromPropertyDesc(r.Property_Desc?.ToString()),

                        Old_Category = r.Old_Category?.ToString(),
                        Old_Market_Value = r.Old_Market_Value?.ToString(),
                        objection_Status = r.objection_Status?.ToString(),
                        Sub_typ = Convert.ToInt32(r.Sub_typ ?? 0),
                        Unit_key = r.Unit_key?.ToString(),
                        Valuation_Key = r.Valuation_Key?.ToString(),
                        PropertyFrom = r.PropertyFrom?.ToString(),
                        UserId = r.UserID?.ToString(),
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
            ?? match.Review_No
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
        SqlConnection connection, string? objectionReference)
    {
        if (string.IsNullOrWhiteSpace(objectionReference)) return string.Empty;

        return (await connection.QueryFirstOrDefaultAsync<string>(
            """
            SELECT TOP 1 UserID
            FROM dbo.Obj_Property_Info
            WHERE LTRIM(RTRIM(Objection_No)) = LTRIM(RTRIM(@Reference))
            """,
            new { Reference = objectionReference.Trim() }))?.Trim() ?? string.Empty;
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
            var connStr = _config.GetConnectionString("QueryConnection");

            if (string.IsNullOrWhiteSpace(connStr))
                return;

            await using var conn = new SqlConnection(connStr);

            /*
             This assumes Que_Property_Info has Query_No and Review_No.
             If your actual review column is different, send me the table script
             and we adjust it.
            */
            var sql = isReview
                ? """
              SELECT TOP 1
                  Query_No,
                  Review_No,
                  Property_Desc,
                  Property_Type,
                  Town_Name,
                  Old_Category,
                  Old_Market_Value,
                  Query_Status,
                  Unit_key,
                  Valuation_Key,
                  Premise_id,
                  UserID
              FROM dbo.Que_Property_Info
              WHERE Review_No = @Ref
              """
                : """
              SELECT TOP 1
                  Query_No,
                  Review_No,
                  Property_Desc,
                  Property_Type,
                  Town_Name,
                  Old_Category,
                  Old_Market_Value,
                  Query_Status,
                  Unit_key,
                  Valuation_Key,
                  Premise_id,
                  UserID
              FROM dbo.Que_Property_Info
              WHERE Query_No = @Ref
              """;

            var qRow = await conn.QueryFirstOrDefaultAsync(sql, new { Ref = refNo });

            if (qRow is null)
                return;

            var match = new AdminRefMatch
            {
                RollSource = "Objection_Query",
                RollName = "Section 78 Query / Review",
                SourceTable = "Query",

                RefType = isReview ? "Review" : "Query",
                ReferenceNo = isReview
                    ? qRow.Review_No?.ToString()
                    : qRow.Query_No?.ToString(),

                Query_No = qRow.Query_No?.ToString(),
                Review_No = qRow.Review_No?.ToString(),

                CurrentStatus = qRow.Query_Status?.ToString(),

                Property_Desc = qRow.Property_Desc?.ToString(),
                Property_Type = qRow.Property_Type?.ToString(),
                Town_Name = qRow.Town_Name?.ToString(),
                Old_Category = qRow.Old_Category?.ToString(),
                Old_Market_Value = qRow.Old_Market_Value?.ToString(),

                Unit_key = qRow.Unit_key?.ToString(),
                Valuation_Key = qRow.Valuation_Key?.ToString(),
                PremiseId = qRow.Premise_id?.ToString(),
                UserId = qRow.UserID?.ToString()
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
}