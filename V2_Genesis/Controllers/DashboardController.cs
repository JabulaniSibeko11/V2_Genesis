using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Entities;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.Section78;
using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services;
using V2_Genesis.Services.Implementations;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Controllers;

[Authorize(Roles = "Client")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAnnouncementService _announcement;
    private readonly RollDatesSettings _rollDates;

    private readonly IDashboardService _dashboardService;
    private readonly IAttributesDashboardService _attributesService;
    private readonly IRebatesService _rebates;
    private readonly ILogger<DashboardController> _logger;
    public DashboardController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAnnouncementService announcement,
        IDashboardService dashboardService,
        IOptions<RollDatesSettings> rollDatesOpts,
        IAttributesDashboardService attributesService, IRebatesService rebates,
        ILogger<DashboardController> logger)
    {
        _db = db;
        _userManager = userManager;
        _announcement = announcement;
        _dashboardService = dashboardService;
        _rollDates = rollDatesOpts.Value;
        _attributesService = attributesService;
        _rebates = rebates;
        _logger = logger;
    }

    // ── On-demand roll detail — used by both the Tiles slide-over panel
    //    and the List view's lazy-loaded accordion body. Only the roll
    //    the person actually opens gets its full data queried; the
    //    initial dashboard load only needs the counts already present
    //    on RollData for the tile stats.
    [HttpGet]
    [Route("dashboard/roll-detail/{rollSource}")]
    public async Task<IActionResult> RollDetail(string rollSource)
    {
        if (string.IsNullOrWhiteSpace(rollSource))
            return BadRequest();

        var roll = await _db.GvList.FirstOrDefaultAsync(r => r.Source == rollSource);
        if (roll is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userEmail = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        var data = await _dashboardService.GetRollDataAsync(
        rollSource,
        userId,
        userEmail);

        if (roll.IsQuery)
        {
            PrepareSection78DashboardData(data);
        }

        var dates =
            _rollDates.Dates.GetValueOrDefault(rollSource);

        var periodStatus = roll.IsQuery
            ? "section78"
            : V2_Genesis.Helpers.RollPeriodHelper
                .GetPeriodStatus(dates);

        var vm = new RollDetailViewModel
        {
            Roll = roll,
            Data = data,
            Dates = dates,
            PeriodStatus = periodStatus,
            CanLodgeObjectionForRoll = !roll.IsQuery && periodStatus == "active"
        };

        return PartialView("_RollDetailPartial", vm);
    }

    // ── On-demand Rebates detail — Tiles drawer for the Rebates tile ────
    [HttpGet]
    [Route("dashboard/rebates-detail")]
    public async Task<IActionResult> RebatesDetail()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var vm = new RebatesDetailViewModel();

        try
        {
            vm.Rebates = await _rebates.GetDashboardAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Dashboard] Failed loading rebates for {UserId}", userId);
            vm.Rebates = new();
        }

        return PartialView("_RebatesDetailPartial", vm);
    }

    // ── On-demand Property Attributes detail — Tiles drawer ─────────────
    [HttpGet]
    [Route("dashboard/attributes-detail")]
    public async Task<IActionResult> AttributesDetail()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var vm = new AttributesDetailViewModel
        {
            AttrData = await _attributesService.GetDashboardDataAsync(userId),
            AttributesLinked = await _dashboardService.GetAttributesLinkedAsync(userId)
        };

        return PartialView("_AttributesDetailPartial", vm);
    }

    [HttpGet]
    [Route("dashboard")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userEmail = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        // Valuation rolls (GV/Supp1-4) first in their existing ID order,
        // Query/Review always last — the raw GvList.ID order currently
        // places Query between Supp3 and Supp4, which is wrong for display.
        var rolls = (await _db.GvList.OrderBy(r => r.ID).ToListAsync())
            .OrderBy(r => r.IsQuery)
            .ThenBy(r => r.ID)
            .ToList();
        var attributesData = await _attributesService.GetDashboardDataAsync(userId);
        var attributesLinked = await _dashboardService.GetAttributesLinkedAsync(userId);

        ViewBag.GvList = rolls;

        var rollDataTasks = rolls
            .ToDictionary(
                r => r.Source,
                r => _dashboardService.GetRollDataAsync(r.Source, userId, userEmail));

        await Task.WhenAll(rollDataTasks.Values);

        var rollData = rollDataTasks.ToDictionary(
     kvp => kvp.Key,
     kvp => kvp.Value.Result);

        foreach (var queryRoll in rolls.Where(x => x.IsQuery))
        {
            if (rollData.TryGetValue(
                    queryRoll.Source,
                    out var queryData))
            {
                PrepareSection78DashboardData(queryData);
            }
        }

        // ── REMOVED: foreach (var roll in rolls.Where(r => r.IsQuery))
        //                 rollData[roll.Source] = new RollData();
        // This line was overwriting the Query data loaded above with empty data.

        var vm = new ClientDashboardViewModel
        {
            DisplayName = user.DisplayName,
            IsCompany = user.IsCompany,
            UserId = userId,
            Announcement = _announcement.GetAnnouncement(),
            Rolls = rolls,
            RollData = rollData,
            RollDates = _rollDates.Dates,
            AttributesData = attributesData,
            AttributesLinked = attributesLinked
        };

        try
        {
            vm.Rebates = await _rebates.GetDashboardAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Dashboard] Failed loading rebates for {UserId}", userId);
            vm.Rebates = new();
        }
        return View(vm);
    }

    private static void PrepareSection78DashboardData(
    RollData rollData)
    {
        ArgumentNullException.ThrowIfNull(rollData);

        PrepareSection78LinkedProperties(
            rollData.LinkedProperties);

        PrepareSection78SubmittedProperties(
            rollData.ObjectedProperties);
    }

    private static void PrepareSection78LinkedProperties(
        IEnumerable<LinkedPropertyResult>? properties)
    {
        if (properties == null)
            return;

        foreach (var property in properties)
        {
            property.Review_Status =
                ResolveSection78ReviewStatus(
                    property.Review_Status,
                    property.Review_Close_Date);

            if (Section78ReviewStatus.IsClosed(
                    property.Review_Status))
            {
                property.AvailableAction = "Closed";
                continue;
            }

            property.AvailableAction =
                property.HasCompletedQuery
                    ? "Review"
                    : "Query";
        }
    }

    private static void PrepareSection78SubmittedProperties(
        IEnumerable<ObjectedPropertyResult>? properties)
    {
        if (properties == null)
            return;

        foreach (var property in properties)
        {
            property.Review_Status =
                ResolveSection78ReviewStatus(
                    property.Review_Status,
                    property.Review_Close_Date);

            /*
             * A Review may only be lodged when:
             *
             * 1. This is an original Query, not an existing Review.
             * 2. The Query has been finalised.
             * 3. The Review period is still open.
             */
            property.CanLodgeReview =
                property.Sub_typ == 0
                &&
                string.Equals(
                    property.objection_Status,
                    "Query-Finalized",
                    StringComparison.OrdinalIgnoreCase)
                &&
                Section78ReviewStatus.IsOpen(
                    property.Review_Status);

            if (property.Sub_typ != 0)
            {
                property.ReviewActionText = null;
                continue;
            }

            if (!string.Equals(
                    property.objection_Status,
                    "Query-Finalized",
                    StringComparison.OrdinalIgnoreCase))
            {
                property.ReviewActionText =
                    "Query must be finalised first";

                continue;
            }

            property.ReviewActionText =
                property.CanLodgeReview
                    ? "Lodge Review"
                    : "Review Closed";
        }
    }

    private static string ResolveSection78ReviewStatus(
        string? storedStatus,
        DateTime? reviewCloseDate)
    {
        /*
         * A past close date always wins, even if the SQL Agent job
         * has not updated Review_Status yet.
         */
        if (reviewCloseDate.HasValue &&
            reviewCloseDate.Value.Date < DateTime.Today)
        {
            return Section78ReviewStatus.Closed;
        }

        if (Section78ReviewStatus.IsClosed(storedStatus))
        {
            return Section78ReviewStatus.Closed;
        }

        /*
         * NULL Review_Close_Date means the original Query process
         * remains available.
         */
        return Section78ReviewStatus.Open;
    }
    // ── STUB — replace each case with real DB query when ready ─────────────
    private Task<RollData> GetRollDataAsync(GvList roll, string userId)
    {
        // TODO: You will provide the actual query per roll.
        // Pattern: query the linked-properties table filtered by userId + roll.Source
        // and return the three lists.
        //
        // Example:
        // case "Objection_Supp3":
        //     var linked = await _db.LinkedSup3
        //         .Where(x => x.UserId == userId)
        //         .ToListAsync();
        //     ...

        return Task.FromResult(new RollData());   // empty stub
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("dashboard/inspection/respond")]
    public async Task<IActionResult> RespondToInspectionAppointment(
    InspectionAppointmentResponseVm vm)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var userEmail =
            User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["AttrAppointmentError"] = "Your login session could not be verified.";
            return RedirectToAction(nameof(Index), new { openRoll = "attributes" });
        }

        try
        {
            await _attributesService.RespondToInspectionAppointmentAsync(
                vm,
                userId,
                userEmail);

            TempData["AttrAppointmentSuccess"] =
                "Inspection appointment confirmed successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[Dashboard] Failed to respond to inspection appointment {RequestId}",
                vm.InspectionRequestId);

            TempData["AttrAppointmentError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { openRoll = "attributes" });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyInspectionPin(VerifyInspectionPinVm vm)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var userEmail =
            User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name
            ?? "";

        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["AttributeError"] = "Your session could not be verified. Please log in again.";
            return RedirectToAction("Index");
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var result = await _attributesService.VerifyInspectionPinAsync(
            vm,
            userId,
            userEmail,
            ipAddress,
            userAgent);

        if (!result.Success)
        {
            TempData["PinError"] = result.ErrorMessage ?? "Invalid inspection PIN.";
            TempData["OpenAttributes"] = "true";
            TempData["OpenAppointments"] = "true";

            return RedirectToAction("Index", new { openRoll = "attributes" });
        }

        TempData["PinSuccess"] = "Inspection PIN verified successfully.";

        return View("ValuerDetails", result);
    }
    [HttpGet]
    public async Task<IActionResult> ValuerPhoto(long inspectionRequestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Forbid();

        var photo = await _attributesService.GetVerifiedValuerPhotoAsync(
            inspectionRequestId,
            userId);

        if (photo == null || photo.Bytes.Length == 0)
            return NotFound();

        return File(photo.Bytes, photo.ContentType);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResubmitReturnedAttribute(ResubmitReturnedAttributeVm vm)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var userEmail =
            User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name
            ?? "";

        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["AttributeError"] = "Your session could not be verified. Please log in again.";
            return RedirectToAction("Index");
        }

        try
        {
            await _attributesService.ResubmitReturnedAttributeAsync(
                vm,
                userId,
                userEmail);

            TempData["AttributeSuccess"] = "Your corrected attribute submission was resubmitted successfully.";
        }
        catch (Exception ex)
        {
            TempData["AttributeError"] = ex.Message;
        }

        return RedirectToAction("Index", new { openRoll = "attributes" });
    }
}