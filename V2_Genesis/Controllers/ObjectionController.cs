using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.ViewModels.Objections;
using V2_Genesis.Services.Implementations;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers;

[Authorize]
public class ObjectionController : Controller
{
    private readonly IObjectionService _objectionService;
    private readonly ApplicationDbContext _db;
    private readonly IObjectionFormService _objectionFormService;

    public ObjectionController(
        IObjectionService objectionService,
        ApplicationDbContext db,
        IObjectionFormService objectionFormService)
    {
        _objectionService = objectionService;
        _db = db;
        _objectionFormService = objectionFormService;
    }

    [HttpGet]
    [Route("objection/check")]
    public async Task<IActionResult> CheckProperty(
        string rollSource,
        string sourceTable,
        string unitKey,
        string valuationKey,
        string? objectionNo = null,
        string appealStatus = "False")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        // ── Fetch property details ─────────────────────────────────────
        List<V2_Genesis.Models.Objections.CheckPropertyResult> items;

        if (appealStatus == "True" && !string.IsNullOrEmpty(objectionNo))
        {
            items = await _objectionService.GetPropertyForAppealAsync(rollSource, objectionNo);
        }
        else
        {
            items = await _objectionService.GetPropertyForObjectionAsync(
                sourceTable, unitKey, valuationKey);
        }

        // ── Set TempData (required by old controllers' objection forms) ─
        if (items.Any())
        {
            var d = items.First();
            TempData["CurrentFilter_PD"] = d.PropertyDesc;
            TempData["CurrentFilter_CD"] = d.CatDesc;
            TempData["CurrentFilter_LSA"] = d.LisStreetAddress;
            TempData["CurrentFilter_RA"] = d.RateableArea;
            TempData["CurrentFilter_MV"] = d.MarketValue;
            TempData["CurrentFilter_ON"] = d.OwnerName;
            TempData["CurrentFilter_TN"] = d.TownNameDesc;
            TempData["CurrentFilter_P_ID"] = d.PremiseId;
            TempData["CurrentFilter_P_I"] = d.PropertyId;
            TempData["CurrentFilter_UK"] = d.UnitKey;
            TempData["CurrentFilter_VK"] = d.ValuationKey;
            TempData["CurrentFilter_S"] = d.Sector;
            TempData["AppealStatus"] = appealStatus;

            foreach (var item in items)
            {
                if (item.IsMultiPurpose)
                {
                    TempData["CurrentFilter_mult_purp_CAT"] = item.CatDesc;
                    TempData["CurrentFilter_mult_purp_PA"] = item.LisStreetAddress;
                    TempData["CurrentFilter_mult_purp_EXT"] = item.RateableArea;
                    TempData["CurrentFilter_mult_purp_MV"] = item.MarketValue;
                }
                else if (item.CatDesc is "Residential" or "Public Service Infrastructure" or
                         "Split - Residential" or "Split - Industrial" or "Industrial")
                {
                    TempData["CurrentFilter_mult_Res_CAT"] = item.CatDesc;
                    TempData["CurrentFilter_mult_Res_PA"] = item.LisStreetAddress;
                    TempData["CurrentFilter_mult_Res_EXT"] = item.RateableArea;
                    TempData["CurrentFilter_mult_Res_MV"] = item.MarketValue;
                }
                else
                {
                    TempData["CurrentFilter_mult_Bus_CAT"] = item.CatDesc;
                    TempData["CurrentFilter_mult_Bus_PA"] = item.LisStreetAddress;
                    TempData["CurrentFilter_mult_Bus_EXT"] = item.RateableArea;
                    TempData["CurrentFilter_mult_Bus_MV"] = item.MarketValue;
                }
            }
        }

        ViewData["SourceTable"] = sourceTable;
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        var vm = new CheckPropertyViewModel
        {
            Items = items,
            SourceTable = sourceTable,
            RollSource = rollSource,
            AppealStatus = appealStatus,
            ControllerName = ObjectionService.SourceToController
                                 .GetValueOrDefault(sourceTable, "Sup3")
        };

        return View(vm);
    }

    // ── GET /objection/form ───────────────────────────────────────────────
    [HttpGet]
    [Route("objection/form")]
    public IActionResult ViewObjectionForm(string? propertyFrom)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        ViewData["UserEmail"] = userEmail;
        ViewData["SourceTable"] = propertyFrom;

        return View("ObjectionForm");
    }

    // ── POST /objection/form ──────────────────────────────────────────────
    [HttpPost]
    [Route("objection/form")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitObjectionForm(
        Obj_Property_InfoModel obj,
        Obj_Section1Model obj1,
        Obj_Section2Model obj2,
        Obj_Section3ResModel objR3,
        Obj_Section3BusModel objB3,
        Obj_Section3AgriModel objA3,
        Obj_Section4BusModel objB4,
        Obj_Section4ResModel objR4,
        Obj_Section5Model obj5,
        Obj_Section6Model obj6,
        Obj_Section7Model obj7,
        Obj_Files obj_file,
        List<IFormFile> files,
        List<IFormFile> fileR,
        string AppealStat,
        string obj_appeal,
        Obj_Property_Info_AppealModel appeal,
        string? PropertyFrom)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var rollSource = HttpContext.Session.GetString("RollSource")
                        ?? "Objection_Supp3";   // fallback

        var result = await _objectionFormService.SubmitAsync(
            rollSource, userId, AppealStat, obj_appeal,
            obj, obj1, obj2, objR3, objB3, objA3, objB4, objR4,
            obj5, obj6, obj7, obj_file, files, fileR, appeal);

        if (!result.Success)
        {
            TempData["FormError"] = result.ErrorMessage;
            return RedirectToAction("CheckProperty");
        }

        // Set TempData for display page (same keys as V1)
        TempData["pin"] = result.Pin;
        TempData["id"] = result.ObjectionNo;
        TempData["successmessage"] = result.IsAppeal
            ? "Appeal Submitted Successfully"
            : "Objection Submitted Successfully";
        TempData["Count"] = result.IsMulti ? "Multi" : "Single";
        TempData["Old_Category"] = obj6.Old_Category;
        TempData["Old_Market_Value"] = obj6.Old_Market_Value;

        // Redirect to the same V1 display pages
        var ctrl = GetControllerForSession();
        return result.IsMulti
            ? RedirectToAction("MultiPurposeDisplay", ctrl)
            : RedirectToAction("Display", ctrl);
    }

    private string GetControllerForSession()
    {
        var rollSource = HttpContext.Session.GetString("RollSource")
                         ?? "Objection_Supp3";
        return ObjectionService.SourceToController
                   .GetValueOrDefault(rollSource, "Sup3");
    }
    // ── GET /objection/multipurpose ───────────────────────────────────────
    [HttpGet]
    [Route("objection/multipurpose")]
    public IActionResult ViewMultiPurposeForm(string? propertyFrom, string? objectorType, string? appeal)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        ViewData["UserEmail"] = userEmail;
        ViewData["SourceTable"] = propertyFrom;

        return View("ObjectionForm");   // same view — JS shows multi-purpose sections
    }
}