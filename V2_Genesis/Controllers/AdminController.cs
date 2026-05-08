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

    private static readonly Regex AdminPattern =
        new(@"^val\.admin(1[0-9]?|[1-9])@joburg\.org\.za$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public AdminController(
        IDashboardService dashboardService,
        IAdminDashboardService adminService,
        IAuditService audit,
        ApplicationDbContext db,
        IOptions<RollDatesSettings> rollDatesOpts)
    {
        _dashboardService = dashboardService;
        _adminService = adminService;
        _audit = audit;
        _db = db;
        _rollDates = rollDatesOpts.Value;
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

    // ── GET /admin ────────────────────────────────────────────────────
    [HttpGet]
    [Route("admin")]
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin(AdminEmail))
            return RedirectToAction("Login", "Account");

        var rolls = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.GvList = rolls;

        // Same client SPs — admin tracks their own linked/objected/appeals
        var rollDataTasks = rolls
            .Where(r => !r.IsQuery)
            .ToDictionary(
                r => r.Source,
                r => _dashboardService.GetRollDataAsync(r.Source, UserId, AdminEmail));

        await Task.WhenAll(rollDataTasks.Values);

        var rollData = rollDataTasks.ToDictionary(k => k.Key, k => k.Value.Result);
        foreach (var roll in rolls.Where(r => r.IsQuery))
            rollData[roll.Source] = new();

        var vm = new AdminDashboardViewModel
        {
            Rolls = rolls,
            RollData = rollData,
            RollDates = _rollDates.Dates,
            AdminEmail = AdminEmail,
            SapNumber = SapNumber
        };

        await _audit.LogAsync(AdminEmail, AuditActions.ViewDashboard,
            SapNumber, ipAddress: ClientIp);

        return View(vm);
    }

    // ── GET /admin/search ─────────────────────────────────────────────
    [HttpGet]
    [Route("admin/search")]
    public async Task<IActionResult> Search()
    {
        if (!IsAdmin(AdminEmail))
            return View("_NoAccess");

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        return View();
    }

    // ── POST /admin/search/objection ──────────────────────────────────
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

    // ── POST /admin/search/appeal ─────────────────────────────────────
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

    // ── POST /admin/log-action (JS fire-and-forget) ───────────────────
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