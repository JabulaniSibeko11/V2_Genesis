// ═══════════════════════════════════════════════════════════════
//  Controllers/AdminController.cs  — REPLACE full file
// ═══════════════════════════════════════════════════════════════
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.RegularExpressions;
using V2_Genesis.Data;
using V2_Genesis.Models.Admin;
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services;
using V2_Genesis.Services.Attributes;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Controllers;

public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IAdminDashboardService _adminService;
    private readonly IAuditService _audit;
    private readonly ApplicationDbContext _db;
    private readonly RollDatesSettings _rollDates;
    private readonly IAnnouncementService _announcement;
    private readonly IAttributesDashboardService _attributesService;
    private readonly IRebatesService _rebates;
    private readonly IPropertySearchService _search;
    private readonly INoticeService _noticeService;
    private readonly ILogger<AdminController> _logger;

    private static readonly Regex AdminPattern =
        new(@"^val\.admin(1[0-9]?|[1-9])@joburg\.org\.za$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public AdminController(
        IDashboardService dashboardService,
        IAdminDashboardService adminService,
        IAuditService audit,
        ApplicationDbContext db,
        IOptions<RollDatesSettings> rollDatesOpts,
        IAnnouncementService announcement,
        IAttributesDashboardService attributesService,
        IRebatesService rebates,
        IPropertySearchService search,
        INoticeService noticeService,
        ILogger<AdminController> logger)
    {
        _dashboardService = dashboardService;
        _adminService = adminService;
        _audit = audit;
        _db = db;
        _rollDates = rollDatesOpts.Value;
        _announcement = announcement;
        _attributesService = attributesService;
        _rebates = rebates;
        _search = search;
        _noticeService = noticeService;
        _logger = logger;
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private bool IsAdmin(string? email) =>
        !string.IsNullOrEmpty(email) && (
            email.Equals("AdministrationEnquiries@Joburg.org.za",
                StringComparison.OrdinalIgnoreCase) ||
            AdminPattern.IsMatch(email));

    private string AdminEmail => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string SapNumber => User.FindFirstValue("SapNumber") ?? string.Empty;
    private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // ══════════════════════════════════════════════════════════════════
    //  GET /admin  — Dashboard
    // ══════════════════════════════════════════════════════════════════
    [HttpGet]
    [Route("admin")]
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin(AdminEmail))
            return RedirectToAction("Login", "Account");

        var userId = UserId;
        var userEmail = AdminEmail;

        var rolls = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        var attributesData = await _attributesService.GetDashboardDataAsync(userId);
        var attributesLinked = await _dashboardService.GetAttributesLinkedAsync(userId);

        ViewBag.GvList = rolls;

        // ── Use the SAME SPs as the client dashboard ──────────────────
        var rollDataTasks = rolls
            .ToDictionary(
                r => r.Source,
                r => _dashboardService.GetRollDataAsync(r.Source, userId, userEmail));

        await Task.WhenAll(rollDataTasks.Values);

        var rollData = rollDataTasks.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Result);

        // ── Build view model ──────────────────────────────────────────
        var vm = new AdminDashboardViewModel
        {
            UserId = userId,
            AdminEmail = AdminEmail,
            SapNumber = SapNumber,
            Announcement = _announcement.GetAnnouncement(),
            Rolls = rolls,
            RollData = rollData,
            RollDates = _rollDates.Dates,
            AttributesData = attributesData,
            AttributesLinked = attributesLinked,
        };

        try
        {
            vm.Rebates = await _rebates.GetDashboardAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[AdminDashboard] Rebates failed for {UserId}", userId);
            vm.Rebates = new();
        }

        await _audit.LogAsync(AdminEmail, AuditActions.ViewDashboard,
            SapNumber, ipAddress: ClientIp);

        return View(vm);
    }

    // ══════════════════════════════════════════════════════════════════
    //  GET /admin/search  — Unified search page
    // ══════════════════════════════════════════════════════════════════
    [HttpGet]
    [Route("admin/search")]
    public async Task<IActionResult> Search()
    {
        if (!IsAdmin(AdminEmail)) return View("_NoAccess");

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.Townships = await _search.GetTownshipsAsync();
        ViewBag.Schemes = await _search.GetSchemesAsync();
        return View();
    }

    // POST /admin/search/ref — search by reference number
    [HttpPost]
    [Route("admin/search/ref")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchByRef(string refNo, string? rollSource)
    {
        if (!IsAdmin(AdminEmail)) return View("_NoAccess");

        if (string.IsNullOrWhiteSpace(refNo))
        {
            TempData["SearchError"] = "Please enter a reference number.";
            return RedirectToAction(nameof(Search));
        }

        var result = await _adminService.SearchByReferenceAsync(refNo.Trim(), rollSource);

        await _audit.LogAsync(AdminEmail, AuditActions.Search, SapNumber,
            rollSource: rollSource ?? "All",
            searchValue: refNo, ipAddress: ClientIp);

        return View("SearchResults", result);
    }

    // POST /admin/search/property — search by property attributes
    [HttpPost]
    [Route("admin/search/property")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchByProperty(
        string? TownName, string? Stand, string? Address,
        string? Scheme, string? Unit, string? rollSource)
    {
        if (!IsAdmin(AdminEmail)) return View("_NoAccess");

        var result = await _adminService.SearchByPropertyAsync(
            TownName, Stand, Address, Scheme, Unit, rollSource);

        await _audit.LogAsync(AdminEmail, AuditActions.Search, SapNumber,
            rollSource: rollSource ?? "All",
            searchValue: result.SearchInput, ipAddress: ClientIp);

        return View("SearchResults", result);
    }

    // ══════════════════════════════════════════════════════════════════
    //  GET /admin/notices  — View any client's notices
    // ══════════════════════════════════════════════════════════════════
    [HttpGet]
    [Route("admin/notices")]
    public IActionResult Notices()
    {
        if (!IsAdmin(AdminEmail)) return View("_NoAccess");
        return View();
    }

    [HttpPost]
    [Route("admin/notices/search")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NoticesSearch(string clientEmail)
    {
        if (!IsAdmin(AdminEmail)) return View("_NoAccess");

        if (string.IsNullOrWhiteSpace(clientEmail))
        {
            ViewBag.Error = "Please enter a client email address.";
            return View("Notices");
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail ==
                                      clientEmail.Trim().ToUpper());

        if (user is null)
        {
            ViewBag.Error = $"No account found for: {clientEmail}";
            return View("Notices");
        }

        var vm = await _noticeService.GetNoticesDashboardAsync(
            user.Id, user.Email ?? clientEmail);

        await _audit.LogAsync(AdminEmail, "ViewClientNotices", SapNumber,
            entityRef: clientEmail, ipAddress: ClientIp);

        return View("Notices", vm);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Legacy search endpoints (kept for backward compatibility)
    // ══════════════════════════════════════════════════════════════════
    [HttpPost]
    [Route("admin/search/objection")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DashBoard1(string searchValue, string? rollSource)
    {
        if (!IsAdmin(AdminEmail)) return View("_NoAccess");

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        TempData["SearchValue"] = searchValue;

        var rolls = string.IsNullOrEmpty(rollSource)
            ? (await _db.GvList.Where(r => !r.IsQuery).ToListAsync()).Select(r => r.Source)
            : new[] { rollSource };

        var results = new Dictionary<string, List<AdminObjectionResult>>();
        foreach (var roll in rolls)
            results[roll] = await _adminService.SearchObjectionsAsync(roll, searchValue);

        await _audit.LogAsync(AdminEmail, AuditActions.Search, SapNumber,
            rollSource: rollSource ?? "All", searchValue: searchValue, ipAddress: ClientIp);

        ViewBag.SearchResults = results;
        ViewBag.SearchValue = searchValue;
        ViewBag.SearchType = "Objection";
        return View("SearchResults");
    }

    [HttpPost]
    [Route("admin/search/appeal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DashBoard2(string searchAppealValue, string? rollSource)
    {
        if (!IsAdmin(AdminEmail)) return View("_NoAccess");

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        TempData["SearchAppealValue"] = searchAppealValue;

        var rolls = string.IsNullOrEmpty(rollSource)
            ? (await _db.GvList.Where(r => !r.IsQuery).ToListAsync()).Select(r => r.Source)
            : new[] { rollSource };

        var results = new Dictionary<string, List<AdminAppealResult>>();
        foreach (var roll in rolls)
            results[roll] = await _adminService.SearchAppealsAsync(roll, searchAppealValue);

        await _audit.LogAsync(AdminEmail, AuditActions.SearchAppeal, SapNumber,
            rollSource: rollSource ?? "All", searchValue: searchAppealValue, ipAddress: ClientIp);

        ViewBag.SearchResults = results;
        ViewBag.SearchValue = searchAppealValue;
        ViewBag.SearchType = "Appeal";
        return View("SearchResults");
    }

    // ── JS fire-and-forget audit log ──────────────────────────────────
    [HttpPost]
    [Route("admin/log-action")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogAction([FromBody] AdminActionRequest req)
    {
        if (!IsAdmin(AdminEmail)) return Forbid();
        await _audit.LogAsync(AdminEmail, req.Action, SapNumber,
            rollSource: req.RollSource, entityRef: req.EntityRef, ipAddress: ClientIp);
        return Ok();
    }
}

public record AdminActionRequest(string Action, string RollSource, string EntityRef);