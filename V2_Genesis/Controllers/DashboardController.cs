

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
        IAttributesDashboardService attributesService,IRebatesService rebates,
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

    [HttpGet]
    [Route("dashboard")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userEmail = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        var rolls = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
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