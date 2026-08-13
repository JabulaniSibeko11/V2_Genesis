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
    private readonly IAdminReferenceResolver _referenceResolver;
    private readonly IAdminAccountInformationService _accountInformationService;
    private readonly IAdminRollInformationService _rollInformationService;
    private readonly IAdminCaseHistoryService _caseHistoryService;
    private readonly IAdminEnquiryNoticeService _enquiryNoticeService;
    private readonly IAdminPropertyLookupService _propertyLookupService;

    public AdminDashboardService(
        IConfiguration config,
        ILogger<AdminDashboardService> logger,
        ApplicationDbContext db,
        IAdminReferenceResolver referenceResolver,
        IAdminAccountInformationService accountInformationService,
        IAdminRollInformationService rollInformationService,
        IAdminCaseHistoryService caseHistoryService,
        IAdminEnquiryNoticeService enquiryNoticeService,
        IAdminPropertyLookupService propertyLookupService)
    {
        _config = config;
        _logger = logger;
        _db = db;
        _referenceResolver = referenceResolver;
        _accountInformationService = accountInformationService;
        _rollInformationService = rollInformationService;
        _caseHistoryService = caseHistoryService;
        _enquiryNoticeService = enquiryNoticeService;
        _propertyLookupService = propertyLookupService;
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

            var objProps = await (
                from objection in rollDb.Objections.AsNoTracking()
                join section in rollDb.Section6.AsNoTracking()
                    on objection.ObjectionNo equals section.ObjectionReference
                    into sectionRows
                from section in sectionRows.DefaultIfEmpty()
                orderby objection.ObjectionNo descending
                select new ObjectedPropertyResult
                {
                    Objection_No = objection.ObjectionNo,
                    Property_Desc = objection.PropertyDescription,
                    Old_Category = section.OldCategory,
                    Old_Market_Value = section.OldMarketValue,
                    objection_Status = objection.ObjectionStatus,
                    Sub_typ = 0,
                    Unit_key = objection.UnitKey,
                    Valuation_Key = objection.ValuationKey,
                    Property_Type = objection.PropertyType,
                    PropertyFrom = objection.PropertyFrom
                })
                .Take(500)
                .ToListAsync();

            foreach (var objection in objProps)
            {
                objection.Town_Name =
                    ExtractTownFromPropertyDesc(objection.Property_Desc);
            }

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
        string? rollSource,
        CancellationToken cancellationToken = default)
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

        // Phase 1 foundation: resolve the reference once, then use stable
        // property identifiers for narrow cross-roll lookups. The legacy code
        // below remains as a safe fallback during the tab-by-tab migration.
        var foundation = await _referenceResolver.ResolveAsync(
            refNo,
            rollSource,
            cancellationToken);

        if (foundation is not null)
        {
            foundation.AccountInformation =
                await _accountInformationService.GetAsync(
                    foundation,
                    cancellationToken);

            var rollInformationTask = _rollInformationService.GetAsync(
                foundation,
                rollSource,
                cancellationToken);
            var caseHistoryTask = _caseHistoryService.GetAsync(
                foundation,
                cancellationToken);

            await Task.WhenAll(rollInformationTask, caseHistoryTask);
            foundation.RollInformation = rollInformationTask.Result;
            foundation.CaseHistory = caseHistoryTask.Result;

            foundation.Notices = _enquiryNoticeService.Build(foundation);

            result.Foundation = foundation;

            var resolved = foundation.Reference;
            var property = foundation.Property;
            var match = new AdminRefMatch
            {
                RollSource = resolved.RollSource,
                RollName = resolved.RollName,
                SourceTable = resolved.RollSource,
                RefType = resolved.ReferenceType,
                ReferenceNo = resolved.ReferenceNumber,
                Objection_No = resolved.ReferenceType == "Objection"
                    ? resolved.ReferenceNumber
                    : null,
                Appeal_No = resolved.ReferenceType == "Appeal"
                    ? resolved.ReferenceNumber
                    : null,
                Query_No = resolved.ReferenceType is "Query" or "Review"
                    ? resolved.ReferenceNumber.EndsWith(
                        "-R",
                        StringComparison.OrdinalIgnoreCase)
                            ? resolved.ReferenceNumber[..^2]
                            : resolved.ReferenceNumber
                    : null,
                Review_No = resolved.ReferenceType == "Review"
                    ? resolved.ReferenceNumber
                    : null,
                CurrentStatus = resolved.Status,
                Property_Desc = property.PropertyDescription,
                Property_Type = property.PropertyType,
                Town_Name = ExtractTownFromPropertyDesc(property.PropertyDescription),
                Unit_key = property.UnitKey,
                Valuation_Key = property.ValuationKey,
                PremiseId = property.PremiseId,
                PropertyFrom = property.PropertyFrom,
                UserId = resolved.UserId,
                IsThirdParty = resolved.ObjectorType.Contains(
                    "Third",
                    StringComparison.OrdinalIgnoreCase),
                IsRepresentative = resolved.ObjectorType.Contains(
                    "Representative",
                    StringComparison.OrdinalIgnoreCase)
            };

            match.Notices = BuildNoticeOptions(match);

            var account = foundation.AccountInformation.SubmittingAccount;
            if (account.Resolved)
            {
                match.UserId = account.UserId;
                match.ClientDisplayName = account.DisplayName;
                match.ClientEmail = account.Email;
                match.ClientPhoneNumber = account.PhoneNumber;
                match.ClientAccountType = account.AccountType;
                match.ClientAccountResolved = true;
            }

            result.RefMatches.Add(match);
            return result;
        }

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
                            into sectionRows
                        from section in sectionRows.DefaultIfEmpty()
                        where objection.ObjectionNo == refNo
                        select new
                        {
                            objection.ObjectionNo,
                            objection.ObjectionStatus,
                            objection.PropertyDescription,
                            objection.PropertyType,
                            objection.UnitKey,
                            objection.ValuationKey,
                            objection.PremiseId,
                            objection.PropertyFrom,
                            objection.UserId,
                            objection.ObjectorType,
                            section.OldCategory,
                            section.OldMarketValue
                        })
                        .FirstOrDefaultAsync();

                    if (oRow is null)
                        continue;

                    var match = new AdminRefMatch
                    {
                        RollSource = roll,
                        RollName = RollName(roll),
                        SourceTable = RollSourceToSourceTable(roll),

                        RefType = "Objection",
                        ReferenceNo = oRow.ObjectionNo,
                        Objection_No = oRow.ObjectionNo,

                        CurrentStatus = oRow.ObjectionStatus,

                        Property_Desc = oRow.PropertyDescription,
                        Property_Type = oRow.PropertyType,
                        Town_Name = ExtractTownFromPropertyDesc(oRow.PropertyDescription),
                        Old_Category = oRow.OldCategory,
                        Old_Market_Value = oRow.OldMarketValue,

                        Unit_key = oRow.UnitKey,
                        Valuation_Key = oRow.ValuationKey,
                        PremiseId = oRow.PremiseId,
                        PropertyFrom = oRow.PropertyFrom,
                        UserId = oRow.UserId,

                        IsThirdParty = oRow.ObjectorType
                            ?.Contains("Third", StringComparison.OrdinalIgnoreCase) == true,

                        IsRepresentative = oRow.ObjectorType
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
      string? rollSource,
      CancellationToken cancellationToken = default)
    {
        return await _propertyLookupService.SearchAsync(
            town,
            stand,
            address,
            scheme,
            unit,
            rollSource,
            cancellationToken);
    }

    public async Task<AdminSearchResult> OpenPropertyAsync(
        string rollSource,
        string propertyFrom,
        string propertyDescription,
        string unitKey,
        string valuationKey,
        CancellationToken cancellationToken = default)
    {
        var provisional = new AdminEnquiryFoundation
        {
            Reference = new AdminResolvedReference
            {
                ReferenceType = "Property",
                ReferenceNumber = "Property Search",
                RollSource = rollSource,
                RollName = RollName(rollSource),
                Status = "Property identified"
            },
            Property = new AdminCanonicalProperty
            {
                UnitKey = unitKey?.Trim() ?? string.Empty,
                ValuationKey = valuationKey?.Trim() ?? string.Empty,
                PropertyDescription = propertyDescription?.Trim() ?? string.Empty,
                PropertyFrom = propertyFrom?.Trim() ?? string.Empty
            }
        };

        var history = await _caseHistoryService.GetAsync(
            provisional,
            cancellationToken);

        var relatedCase = history.Cases
            .Where(x => !string.IsNullOrWhiteSpace(x.ReferenceNumber))
            .OrderBy(x => x.CaseType switch
            {
                "Objection" => 0,
                "Appeal" => 1,
                "Query" => 2,
                "Review" => 3,
                "Attributes" => 4,
                _ => 9
            })
            .ThenByDescending(x => x.SubmittedAt)
            .FirstOrDefault();

        if (relatedCase is not null)
        {
            var resolved = await SearchByReferenceAsync(
                relatedCase.ReferenceNumber,
                relatedCase.RollSource,
                cancellationToken);

            if (resolved.Foundation is not null)
            {
                resolved.SearchType = "Property";
                resolved.SearchInput = provisional.Property.PropertyDescription;
                return resolved;
            }
        }

        provisional.CaseHistory = history;
        provisional.RollInformation = await _rollInformationService.GetAsync(
            provisional,
            rollSource,
            cancellationToken);
        provisional.Notices = _enquiryNoticeService.Build(provisional);

        return new AdminSearchResult
        {
            SearchType = "Property",
            SearchInput = provisional.Property.PropertyDescription,
            RollFilter = rollSource,
            Foundation = provisional
        };
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
               value.Contains("QUERY") ||
               value.Contains("-QUE-") ||
               value.Contains("-QUE");
    }

    private static bool LooksLikeReview(string refNo)
    {
        var value = refNo.Trim().ToUpperInvariant();
        return value.StartsWith("REV") ||
               value.StartsWith("REVIEW") ||
               value.Contains("REVIEW") ||
               value.EndsWith("-R");
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

        // Use the same unified read-only submitted-form viewer as the dashboards.
        // SubmissionController grants the ownership bypass only to authenticated
        // administrators and keeps the normal ownership check for client users.
        var submissionType = string.IsNullOrWhiteSpace(match.RefType)
            ? "Objection"
            : match.RefType.Trim();

        notices.Add(new AdminNoticeOption
        {
            NoticeName = "View Submitted Form",
            Url = $"/submissions/view/{Uri.EscapeDataString(submissionType)}/" +
                  $"{Uri.EscapeDataString(refNo)}?rollSource=" +
                  $"{Uri.EscapeDataString(match.RollSource)}&returnUrl=%2Fadmin%2Fsearch",
            IsAvailable = true,
            Icon = "fa-eye"
        });

        // Acknowledgement generated from the submitted database record.
        notices.Add(new AdminNoticeOption
        {
            NoticeName = "Acknowledgement",
            Url = $"/notice/acknowledgement/download?objectionNo=" +
                  $"{Uri.EscapeDataString(refNo)}&rollSource=" +
                  $"{Uri.EscapeDataString(match.RollSource)}&returnUrl=%2Fadmin%2Fsearch",
            IsAvailable = true,
            Icon = "fa-file-pdf"
        });

        // Section 51
        var isObjection = submissionType.Equals(
            "Objection",
            StringComparison.OrdinalIgnoreCase);

        notices.Add(new AdminNoticeOption
        {
            NoticeName = "Section 51 Notice",
            Url = $"/notices/download-available?referenceNo=" +
                  $"{Uri.EscapeDataString(refNo)}&type=Section51&rollSource=" +
                  $"{Uri.EscapeDataString(match.RollSource)}&returnUrl=%2Fadmin%2Fsearch" +
                  $"&ownerUserId={Uri.EscapeDataString(match.UserId ?? string.Empty)}",
            IsAvailable = isObjection && match.IsThirdParty,
            ReasonUnavailable = "Section 51 is only applicable to a Third-Party objection.",
            Icon = "fa-file-pdf"
        });

        // Section 53
        notices.Add(new AdminNoticeOption
        {
            NoticeName = "Section 53 Notice",
            Url = $"/notice/section53/download?objectionNo=" +
                  $"{Uri.EscapeDataString(refNo)}&rollSource=" +
                  $"{Uri.EscapeDataString(match.RollSource)}&returnUrl=%2Fadmin%2Fsearch",
            IsAvailable = isObjection
                          && status.Equals(
                              "Notice-Sent",
                              StringComparison.OrdinalIgnoreCase),
            ReasonUnavailable = "Section 53 is only available after Notice-Sent.",
            Icon = "fa-file-pdf"
        });

        // Appeal decision
        notices.Add(new AdminNoticeOption
        {
            NoticeName = "Appeal Decision / Section 52",
            Url = $"/notice/appeal-outcome/download?referenceNumber=" +
                  $"{Uri.EscapeDataString(refNo)}&rollSource=" +
                  $"{Uri.EscapeDataString(match.RollSource)}&returnUrl=%2Fadmin%2Fsearch",
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

            var queryReference = isReview && refNo.EndsWith(
                    "-R",
                    StringComparison.OrdinalIgnoreCase)
                ? refNo[..^2]
                : refNo;

            var qRow = await queryDb.Queries
                .AsNoTracking()
                .Where(x =>
                    x.QueryNo == refNo ||
                    (isReview &&
                     x.QueryNo == queryReference &&
                     x.SubType == 1))
                .OrderByDescending(x => x.SubType)
                .FirstOrDefaultAsync();

            if (qRow is null)
                return;

            var storedQueryReference = qRow.QueryNo?.Trim() ?? queryReference;
            var displayReference = isReview &&
                                   !storedQueryReference.EndsWith(
                                       "-R",
                                       StringComparison.OrdinalIgnoreCase)
                ? storedQueryReference + "-R"
                : storedQueryReference;

            var section = await queryDb.Section6
                .AsNoTracking()
                .Where(x =>
                    x.ObjectionReference == displayReference ||
                    x.ObjectionReference == storedQueryReference)
                .OrderByDescending(x =>
                    x.ObjectionReference == displayReference)
                .FirstOrDefaultAsync();

            var propertyDescription = FirstNonEmptyDynamic(
                qRow.PropertyDescription,
                section?.OldPropertyDescription);

            var match = new AdminRefMatch
            {
                RollSource = "Objection_Query",
                RollName = "Section 78 Query / Review",
                SourceTable = "Query",

                RefType = isReview ? "Review" : "Query",
                ReferenceNo = displayReference,

                Query_No = storedQueryReference,


                CurrentStatus = qRow.QueryStatus,

                Property_Desc = propertyDescription,
                Property_Type = qRow.PropertyType,
                Town_Name = ExtractTownFromPropertyDesc(propertyDescription),
                Old_Category = section?.OldCategory,
                Old_Market_Value = section?.OldMarketValue,

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
                entity.Property(x => x.ObjectionStatus).HasColumnName("objection_Status");
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
                entity.Property(x => x.OldPropertyDescription).HasColumnName("Old_Property_Description");
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
                entity.Property(x => x.QueryStatus).HasColumnName("Query_Status");
                entity.Property(x => x.SubType).HasColumnName("Sub_typ");
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
        public string? ObjectionStatus { get; set; }
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
        public string? OldPropertyDescription { get; set; }
        public string? OldCategory { get; set; }
        public string? OldMarketValue { get; set; }
    }

    private sealed class AdminQueryRow
    {
        public long QueryId { get; set; }
        public string? QueryNo { get; set; }
        public string? PropertyDescription { get; set; }
        public string? PropertyType { get; set; }
        public string? QueryStatus { get; set; }
        public int SubType { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PremiseId { get; set; }
        public string? UserId { get; set; }
    }
}
