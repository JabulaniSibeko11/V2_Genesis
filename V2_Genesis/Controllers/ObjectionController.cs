using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using V2_Genesis.Data;
using V2_Genesis.Helpers;
using V2_Genesis.Models;
using V2_Genesis.Models.Emails;
using V2_Genesis.Models.Objections;
using V2_Genesis.Models.Results.Section78;
using V2_Genesis.Models.ViewModels;
using V2_Genesis.Models.ViewModels.Objections;
using V2_Genesis.Services.Implementations;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Notice;

namespace V2_Genesis.Controllers;

[Authorize]
public class ObjectionController : Controller
{
    private readonly IObjectionService _objectionService;
    private readonly ApplicationDbContext _db;
    private readonly IObjectionFormService _objectionFormService;
    private readonly ISection78Service _section78Service;
    private readonly ISubmittedFormPdfService _submittedFormPdfService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ISupportingDocumentService _supportingDocumentService;

    private readonly IConfiguration _config;
    private readonly INoticeService _noticeService;
    private readonly ILogger<ObjectionController> _logger;

    public ObjectionController(
        IObjectionService objectionService,
        ApplicationDbContext db,
        IObjectionFormService objectionFormService,ISection78Service section78Service, INoticeService noticeService
        ,IEmailService emailService, IConfiguration config,ISubmittedFormPdfService submittedFormPdfService, 
        ISupportingDocumentService supportingDocumentService
        ,INotificationService notificationService, ILogger<ObjectionController> logger)
    {
        _objectionService = objectionService;
        _db = db;
        _objectionFormService = objectionFormService;
        _emailService = emailService;
        _notificationService = notificationService;
      _submittedFormPdfService = submittedFormPdfService;   
        _config = config;
        _noticeService = noticeService;
     _section78Service= section78Service;

        _supportingDocumentService = supportingDocumentService;
        _logger = logger; 
    }

    private static readonly System.Text.RegularExpressions.Regex AdminEmailRx =
    new(@"^val\.admin(1[0-9]?|[1-9])@joburg\.org\.za$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase |
        System.Text.RegularExpressions.RegexOptions.Compiled);

    [HttpGet]
    [Route("objection/check")]
    public async Task<IActionResult> CheckProperty(
       string rollSource,
       string sourceTable,
       string? unitKey = null,
       string? valuationKey = null,
       string? objectionNo = null,
       string appealStatus = "False",
       string? PropertyFrom = null,
       bool omission = false)
    {
        unitKey = FloatKeyHelper.Normalize(unitKey);
        valuationKey = FloatKeyHelper.Normalize(valuationKey);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        rollSource = string.IsNullOrWhiteSpace(rollSource)
            ? TempData.Peek("RollSource")?.ToString() ?? HttpContext.Session.GetString("RollSource") ?? ""
            : rollSource.Trim();

        sourceTable = string.IsNullOrWhiteSpace(sourceTable)
            ? TempData.Peek("SourceTable")?.ToString() ?? ResolveSourceTable(rollSource)
            : sourceTable.Trim();

        // Always set these early so the next view/form has them
        TempData["AppealStatus"] = appealStatus ?? "False";
        TempData["RollSource"] = rollSource;
        TempData["SourceTable"] = sourceTable;
        TempData["PropertyFrom"] = PropertyFrom ?? sourceTable ?? rollSource ?? "";

        TempData.Keep("AppealStatus");
        TempData.Keep("RollSource");
        TempData.Keep("SourceTable");
        TempData.Keep("PropertyFrom");

        ViewData["SourceTable"] = sourceTable;
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        var rollRecord = await _db.GvList
            .FirstOrDefaultAsync(r => r.Source == rollSource);

        var rollDisplayName = rollRecord == null
            ? "Valuation Roll"
            : $"{rollRecord.Name} ({sourceTable})";

        TempData["RollDisplayName"] = rollDisplayName;
        TempData.Keep("RollDisplayName");

        bool isOmission = omission ||
            TempData.Peek("OmissionStatus")?.ToString() == "True";

        bool isAppeal = appealStatus == "True";

        if (isOmission)
        {
            TempData["AppealStatus"] = "False";
            TempData["PropertyFrom"] = "Omission";
            TempData["OmissionStatus"] = "True";

            TempData["Omission_PropertyDesc"] = TempData.Peek("OmittedPropertyDesc")?.ToString();
            TempData["Omission_TownName"] = TempData.Peek("OmittedTownName")?.ToString();
            TempData["Omission_Address"] = TempData.Peek("Omission_Address")?.ToString();

            TempData["CurrentFilter_PD"] = "";
            TempData["CurrentFilter_Prop"] = "";
            TempData["CurrentFilter_CD"] = "";
            TempData["CurrentFilter_LSA"] = "";
            TempData["CurrentFilter_RA"] = "";
            TempData["CurrentFilter_MV"] = "";
            TempData["CurrentFilter_ON"] = "";
            TempData["CurrentFilter_TN"] = TempData.Peek("OmittedTownName")?.ToString();
            TempData["CurrentFilter_P_ID"] = "";
            TempData["CurrentFilter_P_I"] = "";
            TempData["CurrentFilter_UK"] = "";
            TempData["CurrentFilter_VK"] = "";
            TempData["CurrentFilter_S"] = "";

            KeepObjectionFormTempData();

            var omitVm = new CheckPropertyViewModel
            {
                Items = new(),
                SourceTable = sourceTable,
                RollSource = rollSource,
                AppealStatus = "False",
                IsAppeal = false,
                PropertyFrom = "Omission",
                ControllerName = !string.IsNullOrEmpty(sourceTable)
                    ? ObjectionService.SourceToController.GetValueOrDefault(sourceTable, "Omission")
                    : ObjectionService.RollSourceToController.GetValueOrDefault(rollSource, "Omission"),

                IsOmission = true,
                OmittedTownName = TempData.Peek("OmittedTownName")?.ToString(),
                OmittedPropertyDesc = TempData.Peek("OmittedPropertyDesc")?.ToString(),
                OmittedAddress = TempData.Peek("Omission_Address")?.ToString(),
                OmittedStand = TempData.Peek("Omission_Stand")?.ToString(),
                OmittedScheme = TempData.Peek("Omission_Scheme")?.ToString(),
                OmittedUnit = TempData.Peek("Omission_Unit")?.ToString(),
            };

            TempData.Keep("PropertyFrom");
            TempData.Keep("OmissionStatus");
            TempData.Keep("Omission_PropertyDesc");
            TempData.Keep("Omission_TownName");
            TempData.Keep("Omission_Address");

            return View(omitVm);
        }

        List<CheckPropertyResult> items = new();
        List<Section78PropertyDetail> Queitems = new();

        var propertyFromValue = PropertyFrom ?? sourceTable ?? rollSource ?? "";
        bool isLis = propertyFromValue.Equals("LIS", StringComparison.OrdinalIgnoreCase);
        bool isQuery = rollSource.Contains("Query", StringComparison.OrdinalIgnoreCase);

        // ============================================================
        // OBJECTION PERIOD CHECK
        // Only objection lodging uses RollDates OpenDate / VisibleUntil.
        // Appeal is checked later after property/MVD data is loaded.
        // ============================================================
        if (!isQuery && !isLis && !isAppeal)
        {
            var objectionWindow = await _objectionService.CheckObjectionWindowAsync(
                rollSource,
                sourceTable);

            if (!objectionWindow.IsOpen)
            {
                TempData["LodgementWindowError"] = objectionWindow.Message;

                return RedirectToAction("Index", "Dashboard", new
                {
                    openRoll = rollSource
                });
            }
        }

        if (isQuery)
        {
            var queItem = await _section78Service
                .GetPropertyDetailAsync(unitKey, valuationKey);

            Queitems = queItem != null
                ? new List<Section78PropertyDetail> { queItem }
                : new List<Section78PropertyDetail>();

            if (Queitems.Any())
            {
                var q = Queitems.First();

                TempData["CurrentFilter_PD"] = q.PropertyDesc;
                TempData["CurrentFilter_Prop"] = q.PropertyDesc;
                TempData["CurrentFilter_CD"] = q.CatDesc;
                TempData["CurrentFilter_LSA"] = q.LisStreetAddress;
                TempData["CurrentFilter_RA"] = q.RateableArea;
                TempData["CurrentFilter_MV"] = q.MarketValue;
                TempData["CurrentFilter_ON"] = q.OwnerName;
                TempData["CurrentFilter_TN"] = q.TownNameDesc;
                TempData["CurrentFilter_P_ID"] = q.PremiseId;
                TempData["CurrentFilter_P_I"] = q.PropertyId;
                TempData["CurrentFilter_UK"] = q.UnitKey;
                TempData["CurrentFilter_VK"] = q.ValuationKey;
                TempData["CurrentFilter_S"] = q.Sector;
                TempData["AppealStatus"] = "False";

                KeepObjectionFormTempData();
            }
        }
        else if (isLis)
        {
            items = await _objectionService.GetPropertyForLisAsync(
                rollSource,
                unitKey,
                valuationKey);

            TempData["PropertyFrom"] = "LIS";
            TempData.Keep("PropertyFrom");
        }
        else
        {
            if (isAppeal && !string.IsNullOrEmpty(objectionNo))
            {
                items = await _objectionService
                    .GetPropertyForAppealAsync(rollSource, objectionNo);
            }
            else
            {
                items = await _objectionService
                    .GetPropertyForObjectionAsync(
                        sourceTable,
                        unitKey,
                        valuationKey);
            }
        }

        if (items.Any())
        {
            var d = items.First();

            // ============================================================
            // APPEAL PERIOD CHECK
            // Appeal dates come from dbo.Objection_MVD:
            // Appeal_Start_Date / Appeal_Close_Date
            // or Appeal_Start_Date_ReviseMVD / Appeal_Close_Date_ReviseMVD
            // ============================================================
            if (isAppeal)
            {
                var appealWindow = await _objectionService.CheckAppealWindowAsync(
                    rollSource: rollSource,
                    objectionNo: objectionNo,
                    unitKey: d.UnitKey,
                    valuationKey: d.ValuationKey,
                    propertyDesc: d.PropertyDesc);

                if (!appealWindow.IsOpen)
                {
                    TempData["LodgementWindowError"] = appealWindow.Message;

                    return RedirectToAction("Index", "Dashboard", new
                    {
                        openRoll = rollSource
                    });
                }
            }

            // ============================================================
            // DUPLICATE LODGEMENT CHECK
            // Blocks duplicate objection/appeal before form opens.
            // ============================================================
            var duplicate = await _objectionService.CheckDuplicateLodgementAsync(
                rollSource: rollSource,
                sourceTable: sourceTable,
                unitKey: d.UnitKey,
                valuationKey: d.ValuationKey,
                propertyDesc: d.PropertyDesc,
                isAppeal: isAppeal);

            if (duplicate.Exists)
            {
                var typeWord = isAppeal ? "appeal" : "objection";

                TempData["DuplicateLodgementError"] =
                    $"This property already has an {typeWord} lodged or in progress. " +
                    "You cannot lodge it again. Please contact the Valuation team.";

                TempData["DuplicateReferenceNo"] = duplicate.ReferenceNo;
                TempData["DuplicateStatus"] = duplicate.Status;
                TempData["DuplicatePropertyDescription"] = duplicate.PropertyDescription;

                return RedirectToAction("Index", "Dashboard", new
                {
                    openRoll = rollSource
                });
            }

            TempData["CurrentFilter_PD"] = d.PropertyDesc;
            TempData["CurrentFilter_Prop"] = d.PropertyDesc;
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
            TempData["AppealStatus"] = appealStatus ?? "False";

            foreach (var item in items)
            {
                if (item.IsMultiPurpose)
                {
                    TempData["CurrentFilter_mult_purp_CAT"] = item.CatDesc;
                    TempData["CurrentFilter_mult_purp_PA"] = item.LisStreetAddress;
                    TempData["CurrentFilter_mult_purp_EXT"] = item.RateableArea;
                    TempData["CurrentFilter_mult_purp_MV"] = item.MarketValue;
                }
                else if (item.CatDesc is
                    "Residential" or
                    "Public Service Infrastructure" or
                    "Split - Residential" or
                    "Split - Industrial" or
                    "Industrial")
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

            KeepObjectionFormTempData();
        }

        KeepObjectionFormTempData();

        var vm = new CheckPropertyViewModel
        {
            Items = items,
            Queitems = Queitems,
            SourceTable = sourceTable,
            RollSource = rollSource,
            AppealStatus = appealStatus ?? "False",
            IsAppeal = isAppeal,
            PropertyFrom = isLis ? "LIS" : PropertyFrom ?? sourceTable ?? rollSource,
            ControllerName = isQuery
                ? "Query"
                : ObjectionService.SourceToController
                    .GetValueOrDefault(sourceTable ?? string.Empty, "Sup3"),
            IsOmission = false,
        };

        return View(vm);
    }

    //[HttpGet]
    //[Route("objection/check")]
    //public async Task<IActionResult> CheckProperty(
    //    string rollSource,
    //    string sourceTable,
    //    string? unitKey = null,
    //    string? valuationKey = null,
    //    string? objectionNo = null,
    //    string appealStatus = "False",
    //    string? PropertyFrom = null,
    //    bool omission = false)
    //{

    //    unitKey = NormalizeKey(unitKey);
    //    valuationKey = NormalizeKey(valuationKey);

    //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    //    if (string.IsNullOrEmpty(userId))
    //        return RedirectToAction("Login", "Account");

    //    ViewData["SourceTable"] = sourceTable;
    //    ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

    //    // ══════════════════════════════════════════════════════════════════
    //    //  OMISSION PATH
    //    //  Property was not found on roll or LIS — client is lodging
    //    //  an omission objection for a property not yet on the roll
    //    // ══════════════════════════════════════════════════════════════════
    //    bool isOmission = omission ||
    //        TempData.Peek("OmissionStatus")?.ToString() == "True";

    //    if (isOmission)
    //    {
    //        // Keep all omission TempData alive for the form that follows
    //        TempData.Keep("OmissionStatus");
    //        TempData.Keep("OmittedTownName");
    //        TempData.Keep("OmittedPropertyDesc");
    //        TempData.Keep("Omission_Address");
    //        TempData.Keep("Omission_Stand");
    //        TempData.Keep("Omission_Scheme");
    //        TempData.Keep("Omission_Unit");

    //        // Omission TempData also expected by old controllers' objection forms
    //        TempData["AppealStatus"] = "False";
    //        TempData["CurrentFilter_TN"] = TempData.Peek("OmittedTownName");
    //        TempData["CurrentFilter_PD"] = TempData.Peek("OmittedPropertyDesc");
    //        TempData["CurrentFilter_LSA"] = TempData.Peek("Omission_Address");

    //        var omitVm = new CheckPropertyViewModel
    //        {
    //            Items = new(),                  // no DB rows for omission
    //            SourceTable = sourceTable,
    //            RollSource = rollSource,
    //            AppealStatus = "False",
    //            IsAppeal = false,
    //            PropertyFrom = PropertyFrom ?? rollSource,
    //            ControllerName = !string.IsNullOrEmpty(sourceTable)
    //// sourceTable was passed (e.g. "GV23-SUP2") → use SourceToController
    //? ObjectionService.SourceToController
    //      .GetValueOrDefault(sourceTable, "Omission")
    //// sourceTable missing → fall back to rollSource mapping
    //: ObjectionService.RollSourceToController
    //      .GetValueOrDefault(rollSource, "Omission"),


    //            // Omission-specific fields
    //            IsOmission = true,
    //            OmittedTownName = TempData.Peek("OmittedTownName")?.ToString(),
    //            OmittedPropertyDesc = TempData.Peek("OmittedPropertyDesc")?.ToString(),
    //            OmittedAddress = TempData.Peek("Omission_Address")?.ToString(),
    //            OmittedStand = TempData.Peek("Omission_Stand")?.ToString(),
    //            OmittedScheme = TempData.Peek("Omission_Scheme")?.ToString(),
    //            OmittedUnit = TempData.Peek("Omission_Unit")?.ToString(),
    //        };

    //        return View(omitVm);
    //    }

    //    // ══════════════════════════════════════════════════════════════════
    //    //  NORMAL PATH — fetch property from DB
    //    // ══════════════════════════════════════════════════════════════════
    //    List<CheckPropertyResult> items = new();
    //    List<Section78PropertyDetail> Queitems = new();

    //    // ── EXISTING: populate Queitems from SP ──────────────────────────
    //    if (rollSource.Contains("Query", StringComparison.OrdinalIgnoreCase))
    //    {
    //        var queItem = await _section78Service
    //            .GetPropertyDetailAsync(unitKey, valuationKey);

    //        Queitems = queItem != null
    //            ? new List<Section78PropertyDetail> { queItem }
    //            : new List<Section78PropertyDetail>();

    //        // ── FIX 2: Set TempData for the S78 form ─────────────────────
    //        if (Queitems.Any())
    //        {
    //            var q = Queitems.First();
    //            TempData["CurrentFilter_PD"] = q.PropertyDesc;
    //            TempData["CurrentFilter_CD"] = q.CatDesc;
    //            TempData["CurrentFilter_LSA"] = q.LisStreetAddress;
    //            TempData["CurrentFilter_RA"] = q.RateableArea;
    //            TempData["CurrentFilter_MV"] = q.MarketValue;
    //            TempData["CurrentFilter_ON"] = q.OwnerName;
    //            TempData["CurrentFilter_TN"] = q.TownNameDesc;
    //            TempData["CurrentFilter_P_ID"] = q.PremiseId;
    //            TempData["CurrentFilter_P_I"] = q.PropertyId;
    //            TempData["CurrentFilter_UK"] = q.UnitKey;
    //            TempData["CurrentFilter_VK"] = q.ValuationKey;
    //            TempData["CurrentFilter_S"] = q.Sector;
    //            TempData["AppealStatus"] = "False";
    //        }
    //    }
    //    else
    //    {
    //        if (appealStatus == "True" && !string.IsNullOrEmpty(objectionNo))
    //        {
    //            items = await _objectionService
    //                .GetPropertyForAppealAsync(rollSource, objectionNo);
    //        }
    //        else
    //        {
    //            items = await _objectionService
    //                .GetPropertyForObjectionAsync(
    //                    sourceTable,
    //                    unitKey,
    //                    valuationKey);
    //        }
    //    }
    //    // ── TempData for old controllers' objection forms ─────────────────
    //    if (items.Any())
    //    {
    //        var d = items.First();
    //        TempData["CurrentFilter_PD"] = d.PropertyDesc;
    //        TempData["CurrentFilter_CD"] = d.CatDesc;
    //        TempData["CurrentFilter_LSA"] = d.LisStreetAddress;
    //        TempData["CurrentFilter_RA"] = d.RateableArea;
    //        TempData["CurrentFilter_MV"] = d.MarketValue;
    //        TempData["CurrentFilter_ON"] = d.OwnerName;
    //        TempData["CurrentFilter_TN"] = d.TownNameDesc;
    //        TempData["CurrentFilter_P_ID"] = d.PremiseId;
    //        TempData["CurrentFilter_P_I"] = d.PropertyId;
    //        TempData["CurrentFilter_UK"] = d.UnitKey;
    //        TempData["CurrentFilter_VK"] = d.ValuationKey;
    //        TempData["CurrentFilter_S"] = d.Sector;
    //        TempData["AppealStatus"] = appealStatus;

    //        foreach (var item in items)
    //        {
    //            if (item.IsMultiPurpose)
    //            {
    //                TempData["CurrentFilter_mult_purp_CAT"] = item.CatDesc;
    //                TempData["CurrentFilter_mult_purp_PA"] = item.LisStreetAddress;
    //                TempData["CurrentFilter_mult_purp_EXT"] = item.RateableArea;
    //                TempData["CurrentFilter_mult_purp_MV"] = item.MarketValue;
    //            }
    //            else if (item.CatDesc is
    //                "Residential" or
    //                "Public Service Infrastructure" or
    //                "Split - Residential" or
    //                "Split - Industrial" or
    //                "Industrial")
    //            {
    //                TempData["CurrentFilter_mult_Res_CAT"] = item.CatDesc;
    //                TempData["CurrentFilter_mult_Res_PA"] = item.LisStreetAddress;
    //                TempData["CurrentFilter_mult_Res_EXT"] = item.RateableArea;
    //                TempData["CurrentFilter_mult_Res_MV"] = item.MarketValue;
    //            }
    //            else
    //            {
    //                TempData["CurrentFilter_mult_Bus_CAT"] = item.CatDesc;
    //                TempData["CurrentFilter_mult_Bus_PA"] = item.LisStreetAddress;
    //                TempData["CurrentFilter_mult_Bus_EXT"] = item.RateableArea;
    //                TempData["CurrentFilter_mult_Bus_MV"] = item.MarketValue;
    //            }
    //        }
    //    }
    //    // ── FIX 1: Add Queitems to the view model ────────────────────────
    //    var vm = new CheckPropertyViewModel
    //    {
    //        Items = items,
    //        Queitems = Queitems,   // ← THIS LINE WAS MISSING
    //        SourceTable = sourceTable,
    //        RollSource = rollSource,
    //        AppealStatus = appealStatus,
    //        IsAppeal = appealStatus == "True",
    //        PropertyFrom = PropertyFrom ?? sourceTable ?? rollSource,
    //        ControllerName = rollSource.Contains("Query", StringComparison.OrdinalIgnoreCase)
    //            ? "Query"
    //            : ObjectionService.SourceToController
    //                .GetValueOrDefault(sourceTable ?? string.Empty, "Sup3"),
    //        IsOmission = false,
    //    };
    //    return View(vm);
    //}



    // ── GET /objection/form ───────────────────────────────────────────────
    [HttpGet]
    [Route("objection/form")]
    public IActionResult ViewObjectionForm(
     string? propertyFrom,
     string? objectorType,
     string? appeal)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Name) ?? "";

        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        // Keep property values received from CheckProperty
        RestorePropertyContextFromSession();
        KeepObjectionFormTempData();

        // ------------------------------------------------------------
        // PropertyFrom must survive:
        // LIS       -> PropertyFrom = LIS
        // Omission  -> PropertyFrom = Omission
        // Roll      -> PropertyFrom = GV23-SUP3 / GV23-SUP4 / etc.
        // ------------------------------------------------------------
        var tempPropertyFrom = TempData.Peek("PropertyFrom")?.ToString();
        var tempSourceTable = TempData.Peek("SourceTable")?.ToString();
        var tempRollSource = TempData.Peek("RollSource")?.ToString();

        var resolvedPropertyFrom = ResolvePropertyFromForForm(
            propertyFrom,
            tempPropertyFrom,
            tempSourceTable,
            tempRollSource);

        var resolvedSourceTable = ResolveSourceTableForForm(
            resolvedPropertyFrom,
            tempSourceTable,
            tempRollSource);

        TempData["PropertyFrom"] = resolvedPropertyFrom;
        TempData["SourceTable"] = resolvedSourceTable;

        TempData.Keep("PropertyFrom");
        TempData.Keep("SourceTable");
        TempData.Keep("RollSource");

        if (TempData.Peek("AppealStatus") == null)
        {
            TempData["AppealStatus"] =
                string.Equals(appeal, "True", StringComparison.OrdinalIgnoreCase)
                    ? "True"
                    : "False";
        }

        TempData.Keep("AppealStatus");

        bool isAdmin =
            userEmail.Equals(
                "AdministrationEnquiries@Joburg.org.za",
                StringComparison.OrdinalIgnoreCase)
            || AdminEmailRx.IsMatch(userEmail);

        var sapFull =
            User.FindFirstValue("SAPNumber")
            ?? HttpContext.Session.GetString("AdminSapNumber")
            ?? "";

        var adminFullName =
            User.FindFirstValue("FullName")
            ?? HttpContext.Session.GetString("AdminFullName")
            ?? "";

        var adminPosition =
            User.FindFirstValue("Position")
            ?? HttpContext.Session.GetString("AdminPosition")
            ?? "";

        var sapNumeric = sapFull.Contains('\\')
            ? sapFull.Split('\\').Last()
            : sapFull;

        ViewData["UserEmail"] = userEmail;

        // IMPORTANT:
        // SourceTable remains the roll source table.
        // PropertyFrom is the origin: LIS / Omission / GV23-SUP3.
        ViewData["SourceTable"] = resolvedSourceTable;
        ViewData["PropertyFrom"] = resolvedPropertyFrom;

        ViewBag.IsAdmin = isAdmin;
        ViewBag.PropertyFrom = resolvedPropertyFrom;
        ViewBag.SourceTable = resolvedSourceTable;

        if (isAdmin)
        {
            HttpContext.Session.SetString("AdminSapNumber", sapFull);
            HttpContext.Session.SetString("AdminFullName", adminFullName);
            HttpContext.Session.SetString("AdminPosition", adminPosition);

            TempData["SapNumber"] = sapNumeric;
            TempData["AdminFullName"] = adminFullName;
            TempData["AdminPosition"] = adminPosition;
            TempData["SapFull"] = sapFull;

            TempData.Keep("SapNumber");
            TempData.Keep("AdminFullName");
            TempData.Keep("AdminPosition");
            TempData.Keep("SapFull");
        }
        else
        {
            ViewBag.SapNumeric = "";
            ViewBag.AdminFullName = "";
            ViewBag.AdminPosition = "";
            ViewBag.SapFull = "";

            TempData.Remove("SapNumber");
            TempData.Remove("AdminFullName");
            TempData.Remove("AdminPosition");
            TempData.Remove("SapFull");
        }

        KeepObjectionFormTempData();

        return View("ObjectionForm");
    }
    private static string ResolvePropertyFromForForm(
    string? routePropertyFrom,
    string? tempPropertyFrom,
    string? tempSourceTable,
    string? tempRollSource)
    {
        var value =
            FirstNotEmpty(routePropertyFrom, tempPropertyFrom, tempSourceTable, tempRollSource)
            ?? "";

        value = value.Trim();

        if (value.Equals("LIS", StringComparison.OrdinalIgnoreCase))
            return "LIS";

        if (value.Equals("Omission", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Omitted", StringComparison.OrdinalIgnoreCase))
            return "Omission";

        return value;
    }

    private static string ResolveSourceTableForForm(
        string propertyFrom,
        string? tempSourceTable,
        string? tempRollSource)
    {
        // If origin is LIS or Omission, SourceTable must still be the real roll table.
        if (propertyFrom.Equals("LIS", StringComparison.OrdinalIgnoreCase) ||
            propertyFrom.Equals("Omission", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(tempSourceTable) &&
                !tempSourceTable.Equals("LIS", StringComparison.OrdinalIgnoreCase) &&
                !tempSourceTable.Equals("Omission", StringComparison.OrdinalIgnoreCase))
            {
                return tempSourceTable.Trim();
            }

            return tempRollSource switch
            {
                "Objection" => "GV23",
                "Objection_Supp1" => "GV23-SUP1",
                "Objection_Supp2" => "GV23-SUP2",
                "Objection_Supp3" => "GV23-SUP3",
                "Objection_Supp4" => "GV23-SUP4",
                "Objection_Supp5" => "GV23-SUP5",
                _ => tempRollSource ?? ""
            };
        }

        return !string.IsNullOrWhiteSpace(tempSourceTable)
            ? tempSourceTable.Trim()
            : propertyFrom;
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
    private void KeepObjectionFormTempData()
    {
        var keys = new[]
        {
        "AppealStatus",

        "CurrentFilter_PD",
        "CurrentFilter_Prop",
        "CurrentFilter_CD",
        "CurrentFilter_LSA",
        "CurrentFilter_RA",
        "CurrentFilter_MV",
        "CurrentFilter_ON",
        "CurrentFilter_TN",
        "CurrentFilter_P_ID",
        "CurrentFilter_P_I",
        "CurrentFilter_UK",
        "CurrentFilter_VK",
        "CurrentFilter_S",

        "CurrentFilter_mult_purp_CAT",
        "CurrentFilter_mult_purp_PA",
        "CurrentFilter_mult_purp_EXT",
        "CurrentFilter_mult_purp_MV",

        "CurrentFilter_mult_Res_CAT",
        "CurrentFilter_mult_Res_PA",
        "CurrentFilter_mult_Res_EXT",
        "CurrentFilter_mult_Res_MV",

        "CurrentFilter_mult_Bus_CAT",
        "CurrentFilter_mult_Bus_PA",
        "CurrentFilter_mult_Bus_EXT",
        "CurrentFilter_mult_Bus_MV",

        "OmissionStatus",
        "OmittedTownName",
        "OmittedPropertyDesc",
        "Omission_Address",
        "Omission_Stand",
        "Omission_Scheme",
        "Omission_Unit"
    };

        foreach (var key in keys)
        {
            if (TempData.ContainsKey(key))
                TempData.Keep(key);
        }
    }
    private void SetPropertyContext(string key, object? value)
    {
        var stringValue = value?.ToString() ?? string.Empty;

        TempData[key] = stringValue;
        HttpContext.Session.SetString(key, stringValue);
        TempData.Keep(key);
    }

    private void RestorePropertyContextFromSession()
    {
        var keys = new[]
        {
        "AppealStatus",

        "RollSource",
        "SourceTable",
        "PropertyFrom",

        "CurrentFilter_PD",
        "CurrentFilter_Prop",
        "CurrentFilter_CD",
        "CurrentFilter_LSA",
        "CurrentFilter_RA",
        "CurrentFilter_MV",
        "CurrentFilter_ON",
        "CurrentFilter_TN",
        "CurrentFilter_P_ID",
        "CurrentFilter_P_I",
        "CurrentFilter_UK",
        "CurrentFilter_VK",
        "CurrentFilter_S",

        "CurrentFilter_mult_purp_CAT",
        "CurrentFilter_mult_purp_PA",
        "CurrentFilter_mult_purp_EXT",
        "CurrentFilter_mult_purp_MV",

        "CurrentFilter_mult_Res_CAT",
        "CurrentFilter_mult_Res_PA",
        "CurrentFilter_mult_Res_EXT",
        "CurrentFilter_mult_Res_MV",

        "CurrentFilter_mult_Bus_CAT",
        "CurrentFilter_mult_Bus_PA",
        "CurrentFilter_mult_Bus_EXT",
        "CurrentFilter_mult_Bus_MV",

        "OmissionStatus",
        "OmittedTownName",
        "OmittedPropertyDesc",
        "Omission_Address",
        "Omission_Stand",
        "Omission_Scheme",
        "Omission_Unit"
    };

        foreach (var key in keys)
        {
            if (!TempData.ContainsKey(key))
            {
                var sessionValue = HttpContext.Session.GetString(key);

                if (!string.IsNullOrWhiteSpace(sessionValue))
                    TempData[key] = sessionValue;
            }

            if (TempData.ContainsKey(key))
                TempData.Keep(key);
        }
    }
    private static string NormalizeKey(object? value)
    {
        if (value == null)
            return string.Empty;

        var key = value.ToString()?.Trim();

        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        // Fix broken scientific notation like "1.05735e 007"
        key = Regex.Replace(
            key,
            @"([eE])\s+([+-]?\d+)",
            "$1+$2");

        // Convert scientific notation / float / decimal into plain whole-number string
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
    string? PropertyFrom,
    string? SourceTable,
    string? RollSource)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return RedirectToAction("Login", "Account");

        var rollSource = !string.IsNullOrWhiteSpace(RollSource)
     ? RollSource.Trim()
     : ResolveSubmissionRollSource(SourceTable ?? PropertyFrom);

        var sourceTable = !string.IsNullOrWhiteSpace(SourceTable)
            ? SourceTable.Trim()
            : ResolveSourceTable(rollSource);

        var propertyFrom = ResolveSubmittedPropertyFrom(
            PropertyFrom,
            SourceTable,
            rollSource);

        HttpContext.Session.SetString("RollSource", rollSource);
        HttpContext.Session.SetString("SourceTable", sourceTable);
        HttpContext.Session.SetString("PropertyFrom", propertyFrom);

        TempData["RollSource"] = rollSource;
        TempData["SourceTable"] = sourceTable;
        TempData["PropertyFrom"] = propertyFrom;

        TempData.Keep("RollSource");
        TempData.Keep("SourceTable");
        TempData.Keep("PropertyFrom");

        TempData["RollDisplayName"] = BuildRollDisplayName(rollSource, sourceTable);
        TempData.Keep("RollDisplayName");

        var result = await _objectionFormService.SubmitAsync(
            rollSource,userId,AppealStat,obj_appeal,propertyFrom,obj,obj1, obj2,objR3,objB3,
            objA3,objB4,objR4,obj5,obj6,obj7,obj_file,files,fileR,appeal);

        if (!result.Success)
        {
            TempData["FormError"] = result.ErrorMessage;
            return RedirectToAction("CheckProperty");
        }



        var objectionRef = result.ObjectionNo;
        var isAppeal = result.IsAppeal;
        var isMulti = obj.Property_Type?.Equals("Multi", StringComparison.OrdinalIgnoreCase) ?? false;

        var currentUserEmail = GetCurrentUserEmail();

        var notificationTitle = isAppeal
            ? "Appeal lodged successfully"
            : "Objection lodged successfully";

        var notificationMessage = isAppeal
            ? $"Your appeal {objectionRef} has been received."
            : $"Your objection {objectionRef} has been received.";

        var adminTitle = isAppeal
            ? "New appeal lodged"
            : "New objection lodged";

        var adminMessage = isAppeal
            ? $"A new appeal {objectionRef} was lodged on {sourceTable}."
            : $"A new objection {objectionRef} was lodged on {sourceTable}.";

        await _notificationService.CreateClientNotificationAsync(
            userId: userId,
            userEmail: currentUserEmail,
            title: notificationTitle,
            message: notificationMessage,
            referenceNumber: objectionRef,
            premiseId: obj.Premise_id,
            rollSource: rollSource,
            sourceTable: sourceTable,
            url: BuildClientNotificationUrl(rollSource),
            createdBy: userId);

        await _notificationService.CreateAdminNotificationAsync(
            title: adminTitle,
            message: adminMessage,
            referenceNumber: objectionRef,
            premiseId: obj.Premise_id,
            rollSource: rollSource,
            sourceTable: sourceTable,
            url: BuildAdminNotificationUrl(objectionRef),
            createdBy: userId);

        TempData["pin"] = result.Pin;
        TempData["Id"] = objectionRef;
        TempData["objection_ref"] = objectionRef;
        TempData["section51pin"] = result.Pin;
        TempData["time"] = DateTime.Now.ToString("dd MMMM yyyy HH:mm");
        TempData["IsMulti"] = isMulti.ToString();
        TempData["IsAppeal"] = isAppeal.ToString();

        TempData["successmessage"] = isAppeal
            ? "Appeal Submitted Successfully"
            : "Objection Submitted Successfully";

        TempData["desc"] = obj.Property_Desc;

        TempData["ValuationKey"] = isAppeal
            ? appeal?.A_Valuation_Key ?? obj.Valuation_Key
            : obj.Valuation_Key;

        TempData["Old_Property_Description"] = obj6.Old_Property_Description;
        TempData["Old_Category"] = obj6.Old_Category;
        TempData["Old_Address"] = obj6.Old_Address;
        TempData["Old_Extent"] = obj6.Old_Extent;
        TempData["Old_Market_Value"] = obj6.Old_Market_Value;
        TempData["Old_Owner"] = obj6.Old_Owner;

        TempData["new_Property_Description"] = obj6.New_Property_Description;
        TempData["new_Category"] = obj6.New_Category;
        TempData["new_Address"] = obj6.New_Address;
        TempData["new_Extent"] = obj6.New_Extent;
        TempData["new_Market_Value"] = obj6.New_Market_Value;
        TempData["new_Owner"] = obj6.New_Owner;

        TempData["Old2_Category"] = obj6.Old2_Category;
        TempData["Old2_Extent"] = obj6.Old2_Extent;
        TempData["Old2_Market_Value"] = obj6.Old2_Market_Value;

        TempData["new2_Category"] = obj6.New2_Category;
        TempData["new2_Extent"] = obj6.New2_Extent;
        TempData["new2_Market_Value"] = obj6.New2_Market_Value;

        TempData["Old3_Category"] = obj6.Old3_Category;
        TempData["Old3_Extent"] = obj6.Old3_Extent;
        TempData["Old3_Market_Value"] = obj6.Old3_Market_Value;

        TempData["new3_Category"] = obj6.New3_Category;
        TempData["new3_Extent"] = obj6.New3_Extent;
        TempData["new3_Market_Value"] = obj6.New3_Market_Value;

        TempData["objection_reason"] = obj6.Objection_Reasons;

        var allFiles = (files ?? new List<IFormFile>())
            .Concat(fileR ?? new List<IFormFile>())
            .Where(f => f != null && f.Length > 0)
            .ToList();

        TempData["Count"] = allFiles.Count.ToString();

        for (int i = 1; i <= 10; i++)
        {
            TempData[$"File{i}"] = i <= allFiles.Count
                ? allFiles[i - 1].FileName
                : null;
        }

        return RedirectToAction(isMulti ? nameof(MultiPurposeDisplay) : nameof(Display));
    }
    private static string BuildRollDisplayName(string? rollSource, string? sourceTable)
    {
        return rollSource switch
        {
            "Objection" => $"General Valuation Roll ({sourceTable ?? "GV23"})",
            "Objection_Supp1" => $"Supplementary Valuation Roll 1 ({sourceTable ?? "GV23-SUP1"})",
            "Objection_Supp2" => $"Supplementary Valuation Roll 2 ({sourceTable ?? "GV23-SUP2"})",
            "Objection_Supp3" => $"Supplementary Valuation Roll 3 ({sourceTable ?? "GV23-SUP3"})",
            "Objection_Supp4" => $"Supplementary Valuation Roll 4 ({sourceTable ?? "GV23-SUP4"})",
            "Objection_Supp5" => $"Supplementary Valuation Roll 5 ({sourceTable ?? "GV23-SUP5"})",
            _ => "Valuation Roll"
        };
    }
    private static string ResolveSubmittedPropertyFrom(
    string? propertyFrom,
    string? sourceTable,
    string? rollSource)
    {
        var value = propertyFrom?.Trim();

        if (value?.Equals("LIS", StringComparison.OrdinalIgnoreCase) == true)
            return "LIS";

        if (value?.Equals("Omission", StringComparison.OrdinalIgnoreCase) == true ||
            value?.Equals("Omitted", StringComparison.OrdinalIgnoreCase) == true)
            return "Omission";

        if (!string.IsNullOrWhiteSpace(sourceTable))
            return sourceTable.Trim();

        return rollSource?.Trim() ?? "";
    }
    private string ResolveSubmissionRollSource(string? sourceFromForm)
    {
        var source = sourceFromForm;

        if (string.IsNullOrWhiteSpace(source) && Request.HasFormContentType)
        {
            if (Request.Form.TryGetValue("RollSource", out var postedRoll))
                source = postedRoll.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(source)
                && Request.Form.TryGetValue("PropertyFrom", out var postedPropertyFrom))
                source = postedPropertyFrom.FirstOrDefault();
        }

        source ??= TempData.Peek("RollSource")?.ToString()
                ?? HttpContext.Session.GetString("RollSource")
                ?? TempData.Peek("SourceTable")?.ToString()
                ?? TempData.Peek("PropertyFrom")?.ToString()
                ?? "Objection_Supp3";

        return source.Trim() switch
        {
            "GV23" => "Objection",
            "GV23-SUP1" => "Objection_Supp1",
            "GV23-SUP2" => "Objection_Supp2",
            "GV23-SUP3" => "Objection_Supp3",
            "GV23-SUP4" => "Objection_Supp4",
            "GV23-SUP5" => "Objection_Supp5",

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

            var s => s
        };
    }

    private static string ResolveSourceTable(string rollSource)
    {
        return ObjectionService.RollSourceToSourceTable.TryGetValue(rollSource, out var sourceTable)
            ? sourceTable
            : rollSource switch
            {
                "Objection" => "GV23",
                "Objection_Supp1" => "GV23-SUP1",
                "Objection_Supp2" => "GV23-SUP2",
                "Objection_Supp3" => "GV23-SUP3",
                "Objection_Supp4" => "GV23-SUP4",
                "Objection_Supp5" => "GV23-SUP5",
                _ => rollSource
            };
    }
    [HttpGet]
    [Route("objection/display")]
    public async Task<IActionResult> Display()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.IsMulti = false;

        await LoadSupportingDocumentsForDisplayAsync();

        TempData.Keep();

        return View("Display");
    }

    [HttpGet]
    [Route("objection/multipurpose-display")]
    public async Task<IActionResult> MultiPurposeDisplay()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.IsMulti = true;

        await LoadSupportingDocumentsForDisplayAsync();

        TempData.Keep();

        return View("Display");
    }

    private async Task LoadSupportingDocumentsForDisplayAsync()
    {
        var referenceNo =
            TempData.Peek("objection_ref")?.ToString()
            ?? TempData.Peek("Id")?.ToString()
            ?? "";

        var rollSource =
            TempData.Peek("RollSource")?.ToString()
            ?? HttpContext.Session.GetString("RollSource")
            ?? "";

        if (string.IsNullOrWhiteSpace(referenceNo))
        {
            ViewBag.SupportingDocuments = new List<SupportingDocumentViewModel>();
            ViewBag.SupportingDocumentCount = 0;
            ViewBag.CanAddMoreDocuments = true;
            ViewBag.RemainingSupportingDocuments = 10;
            return;
        }

        var docs = await _supportingDocumentService.GetDocumentsAsync(referenceNo, rollSource);

        ViewBag.SupportingDocuments = docs;
        ViewBag.SupportingDocumentCount = docs.Count;
        ViewBag.CanAddMoreDocuments = docs.Count < 10;
        ViewBag.RemainingSupportingDocuments = Math.Max(0, 10 - docs.Count);

        TempData["Count"] = docs.Count.ToString();

        for (int i = 1; i <= 10; i++)
        {
            TempData[$"File{i}"] = i <= docs.Count
                ? docs[i - 1].FileName
                : null;
        }

        TempData.Keep("objection_ref");
        TempData.Keep("Id");
        TempData.Keep("RollSource");
        TempData.Keep("SourceTable");
        TempData.Keep("Count");
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

        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        RestorePropertyContextFromSession();
        KeepObjectionFormTempData();

        if (TempData.Peek("AppealStatus") == null)
        {
            TempData["AppealStatus"] =
                string.Equals(appeal, "True", StringComparison.OrdinalIgnoreCase)
                    ? "True"
                    : "False";
        }

        TempData["CurrentFilter_S"] = "Multi";
        TempData["Property_Type"] = "Multi";

        string adminAppEmail =
            User.FindFirstValue("AdminAppEmail")
            ?? HttpContext.Session.GetString("AdminAppEmail")
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? User.Identity?.Name
            ?? "";

        bool isAdmin =
            User.Identity?.IsAuthenticated == true
            && (
                User.IsInRole("Admin")
                || User.FindFirstValue("UMRole")?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true
                || adminAppEmail.Equals("AdministrationEnquiries@Joburg.org.za", StringComparison.OrdinalIgnoreCase)
            );

        var sapFull =
            User.FindFirstValue("SAPNumber")
            ?? HttpContext.Session.GetString("AdminSapNumber")
            ?? "";

        var sapNumeric =
            User.FindFirstValue("SAPNumeric")
            ?? HttpContext.Session.GetString("AdminSapNumeric")
            ?? "";

        if (string.IsNullOrWhiteSpace(sapNumeric) && !string.IsNullOrWhiteSpace(sapFull))
        {
            sapNumeric = sapFull.Contains('\\')
                ? sapFull.Split('\\').Last()
                : sapFull;
        }

        var adminFullName =
            User.FindFirstValue("FullName")
            ?? HttpContext.Session.GetString("AdminFullName")
            ?? "";

        var adminPosition =
            User.FindFirstValue("Position")
            ?? HttpContext.Session.GetString("AdminPosition")
            ?? "";

        var windowsUser =
            User.FindFirstValue("WindowsUser")
            ?? HttpContext.Session.GetString("AdminWindowsUser")
            ?? "";

        ViewData["UserEmail"] = adminAppEmail;
        ViewData["SourceTable"] = propertyFrom
                                  ?? TempData.Peek("SourceTable")?.ToString()
                                  ?? TempData.Peek("PropertyFrom")?.ToString()
                                  ?? "";

        ViewBag.IsAdmin = isAdmin;
        ViewBag.AdminFullName = adminFullName;
        ViewBag.AdminPosition = adminPosition;
        ViewBag.SapFull = sapFull;
        ViewBag.SapNumeric = sapNumeric;
        ViewBag.AdminWindowsUser = windowsUser;
        ViewBag.IsMulti = true;

        if (isAdmin)
        {
            TempData["SapNumber"] = sapNumeric;
            TempData["AdminFullName"] = adminFullName;
            TempData["AdminPosition"] = adminPosition;
            TempData["SapFull"] = sapFull;
            TempData["AdminWindowsUser"] = windowsUser;

            TempData.Keep("SapNumber");
            TempData.Keep("AdminFullName");
            TempData.Keep("AdminPosition");
            TempData.Keep("SapFull");
            TempData.Keep("AdminWindowsUser");
        }

        TempData.Keep("AppealStatus");
        TempData.Keep("CurrentFilter_S");
        TempData.Keep("Property_Type");

        KeepObjectionFormTempData();

        return View("ViewMultiPurposeForm");
    }
    [HttpGet]
    [Authorize]
    [Route("objection/withdraw")]
    public IActionResult Withdrawal(
        string objectionNo,
        string withdrawType,
        string rollSource,
        string? returnUrl = null)
    {
        TempData["ObjectionNum"] = objectionNo;
        TempData["WithdrawType"] = withdrawType;
        TempData["RollSource"] = rollSource;
        TempData["ReturnUrl"] = returnUrl ?? "/dashboard";
        TempData.Keep();
        return View();
    }


    [HttpPost]
    [Authorize]
    [Route("objection/withdraw")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WithdrawalPost(
        string objectionNo,
        string withdrawType,
        string rollSource,
        string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var (success, error) = await _objectionFormService.WithdrawAsync(
            objectionNo, withdrawType, rollSource, userId);

        if (!success)
        {
            TempData["WithdrawError"] = error;
            // Re-populate TempData so the view can re-render
            TempData["ObjectionNum"] = objectionNo;
            TempData["WithdrawType"] = withdrawType;
            TempData["RollSource"] = rollSource;
            TempData["ReturnUrl"] = returnUrl ?? "/dashboard";
            TempData.Keep();
            return View();
        }

        bool isAppeal = withdrawType?.Contains("Appeal", StringComparison.OrdinalIgnoreCase) == true;
        bool isQuery = withdrawType?.Equals("Query", StringComparison.OrdinalIgnoreCase) == true
                     || withdrawType?.Equals("Section78", StringComparison.OrdinalIgnoreCase) == true;

        TempData["WithdrawSuccess"] =
            $"{(isAppeal ? "Appeal" : isQuery ? "Query" : "Objection")} " +
            $"{objectionNo} has been successfully withdrawn.";

        var sourceTable = ResolveSourceTable(rollSource);
        var currentUserEmail = GetCurrentUserEmail();

        var withdrawLabel = isAppeal
            ? "Appeal"
            : isQuery
                ? "Query"
                : "Objection";

        await _notificationService.CreateClientNotificationAsync(
            userId: userId,
            userEmail: currentUserEmail,
            title: $"{withdrawLabel} withdrawn successfully",
            message: $"Your {withdrawLabel.ToLower()} {objectionNo} has been withdrawn.",
            referenceNumber: objectionNo,
            premiseId: null,
            rollSource: rollSource,
            sourceTable: sourceTable,
            url: BuildClientNotificationUrl(rollSource),
            createdBy: userId);

        await _notificationService.CreateAdminNotificationAsync(
            title: $"{withdrawLabel} withdrawn",
            message: $"{withdrawLabel} {objectionNo} was withdrawn by the client.",
            referenceNumber: objectionNo,
            premiseId: null,
            rollSource: rollSource,
            sourceTable: sourceTable,
            url: BuildAdminNotificationUrl(objectionNo),
            createdBy: userId);

        // Redirect back to wherever the user came from (dashboard, admin, etc.)
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }
    // ══════════════════════════════════════════════════════════════
    //  UNLINK — remove a linked / saved property
    // ══════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize]
    [Route("property/unlink")]
    public IActionResult UnlinkProperty(long linkedId, string? returnUrl = null)
    {
        TempData["UnlinkId"] = linkedId;
        TempData["ReturnUrl"] = returnUrl ?? "/dashboard";
        TempData.Keep();
        return View();
    }

    [HttpPost]
    [Authorize]
    [Route("property/unlink")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkPropertyConfirm(
    long linkedId,
    string rollSource,
    string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (string.IsNullOrWhiteSpace(rollSource))
        {
            rollSource = TempData.Peek("UnlinkRollSource")?.ToString()
                         ?? HttpContext.Session.GetString("RollSource")
                         ?? "Objection_Supp3";
        }

        ;

        var (success, error) = await _objectionFormService.UnlinkPropertyAsync(linkedId, userId);


        if (!success)
        {
            TempData["UnlinkError"] = error;

            return RedirectToAction("Index", "Dashboard", new
            {
                openRoll = rollSource
            });
        }

        TempData["UnlinkSuccess"] =
            "Property has been removed from your profile. " +
            "You can search and link it again at any time.";

        var currentUserEmail = GetCurrentUserEmail();

        await _notificationService.CreateClientNotificationAsync(
            userId: userId,
            userEmail: currentUserEmail,
            title: "Property removed from profile",
            message: "A linked property has been removed from your profile.",
            referenceNumber: null,
            premiseId: null,
            rollSource: rollSource,
            sourceTable: ResolveSourceTable(rollSource),
            url: $"/Dashboard?openRoll={rollSource}",
            createdBy: userId);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Dashboard", new
        {
            openRoll = rollSource
        });
    }
    private string GetCurrentUserEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email)
               ?? User.FindFirstValue(ClaimTypes.Name)
               ?? User.Identity?.Name
               ?? "";
    }

    private string BuildClientNotificationUrl(string rollSource)
    {
        return $"/Dashboard?openRoll={rollSource}";
    }

    private string BuildAdminNotificationUrl(string referenceNumber)
    {
        return $"/Admin/Search?reference={Uri.EscapeDataString(referenceNumber)}";
    }
    //[HttpGet]
    //[Route("notice/acknowledgement/download")]
    //public async Task<IActionResult> DownloadAcknowledgement(string objectionNo,string rollSource)
    //{
    //    if (string.IsNullOrWhiteSpace(objectionNo))
    //    {
    //        TempData["DownloadError"] = "Reference number is missing.";
    //        return RedirectToAction("Index", "Dashboard");
    //    }

    //    if (string.IsNullOrWhiteSpace(rollSource))
    //    {
    //        TempData["DownloadError"] = "Roll source is missing.";
    //        return RedirectToAction("Index", "Dashboard");
    //    }

    //    //var result = await _noticeService.GetSavedAcknowledgementAsync(
    //    //    objectionNo,
    //    //    rollSource);

    //    if (!result.Success || result.FileBytes == null)
    //    {
    //        TempData["DownloadError"] = result.Error
    //            ?? "Acknowledgement PDF was not found.";

    //        return RedirectToAction("Index", "Dashboard", new
    //        {
    //            openRoll = rollSource
    //        });
    //    }

    //    return File(
    //        result.FileBytes,
    //        "application/pdf",
    //        result.FileName ?? $"{objectionNo}_Acknowledgement.pdf");
    //}

}
