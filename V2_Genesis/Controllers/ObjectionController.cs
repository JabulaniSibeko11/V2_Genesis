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
        var rollSource = PropertyFrom
                      ?? HttpContext.Session.GetString("RollSource")
                      ?? "Objection_Supp3";

        var result = await _objectionFormService.SubmitAsync(
            rollSource, userId, AppealStat, obj_appeal,
            obj, obj1, obj2, objR3, objB3, objA3, objB4, objR4,
            obj5, obj6, obj7, obj_file, files, fileR, appeal);

        if (!result.Success)
        {
            TempData["FormError"] = result.ErrorMessage;
            return RedirectToAction("CheckProperty");
        }

        bool isMulti = obj.Property_Type?.Equals(
            "Multi", StringComparison.OrdinalIgnoreCase) ?? false;

        // ── Reference info ────────────────────────────────────────────────
        TempData["pin"] = result.Pin;
        TempData["Id"] = result.ObjectionNo;
        TempData["objection_ref"] = result.ObjectionNo;
        TempData["section51pin"] = result.Pin;
        TempData["time"] = DateTime.Now.ToString("dd MMMM yyyy HH:mm");
        TempData["IsMulti"] = isMulti.ToString();
        TempData["desc"] = obj.Property_Desc;
        TempData["successmessage"] = result.IsAppeal
            ? "Appeal Submitted Successfully"
            : "Objection Submitted Successfully";

        // ── Section 6 — Old (roll) values ─────────────────────────────────
        TempData["Old_Property_Description"] = obj6.Old_Property_Description;
        TempData["Old_Category"] = obj6.Old_Category;
        TempData["Old_Address"] = obj6.Old_Address;
        TempData["Old_Extent"] = obj6.Old_Extent;
        TempData["Old_Market_Value"] = obj6.Old_Market_Value;
        TempData["Old_Owner"] = obj6.Old_Owner;

        // ── Section 7 — New (objector's) values ───────────────────────────
        TempData["new_Property_Description"] = obj6.New_Property_Description;
        TempData["new_Category"] = obj6.New_Category;
        TempData["new_Address"] = obj6.New_Address;
        TempData["new_Extent"] = obj6.New_Extent;
        TempData["new_Market_Value"] = obj6.New_Market_Value;
        TempData["new_Owner"] = obj6.New_Owner;

        // ── Multi — Section 2 old/new ─────────────────────────────────────
        TempData["Old2_Category"] = obj6.Old2_Category;
        TempData["Old2_Extent"] = obj6.Old2_Extent;
        TempData["Old2_Market_Value"] = obj6.Old2_Market_Value;
        TempData["new2_Category"] = obj6.New2_Category;
        TempData["new2_Extent"] = obj6.New2_Extent;
        TempData["new2_Market_Value"] = obj6.New2_Market_Value; 
        // ── Multi — Section 3 old/new ─────────────────────────────────────
        TempData["Old3_Category"] = obj6.Old3_Category;
        TempData["Old3_Extent"] = obj6.Old3_Extent;
        TempData["Old3_Market_Value"] = obj6.Old3_Market_Value;
        TempData["new3_Category"] = obj6.New3_Category;
        TempData["new3_Extent"] = obj6.New3_Extent;
        TempData["new3_Market_Value"] = obj6.New3_Market_Value;

        // ── Reason ────────────────────────────────────────────────────────
        TempData["objection_reason"] = obj6.Objection_Reasons;

        // ── Files ─────────────────────────────────────────────────────────
        var allFiles = (files ?? new())
            .Concat(fileR ?? new())
            .Where(f => f is not null && f.Length > 0)
            .ToList();

        TempData["Count"] = allFiles.Count.ToString();

        for (int i = 1; i <= 10; i++)
        {
            TempData[$"File{i}"] = i <= allFiles.Count
                ? allFiles[i - 1].FileName
                : null;
        }

        // ── Redirect to acknowledgement download ──────────────────────────
        return RedirectToAction(
            "DownloadAcknowledgement", "Notice",
            new { objectionNo = result.ObjectionNo, rollSource });
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