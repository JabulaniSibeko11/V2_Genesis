
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Services.Implementations;
using V2_Genesis.Services.Interfaces;
[Authorize(Roles = "Client")]
public class Section78Controller : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISection78Service _section78;
    private readonly IConfiguration _config;
    private readonly ILogger<Section78Controller> _logger;
    private readonly IEmailService _emailService;

    public Section78Controller(
        ApplicationDbContext db,
        ISection78Service section78,
        IConfiguration config,IEmailService emailService,ILogger<Section78Controller>logger)
    {
        _db = db;
        _section78 = section78;
        _config = config;
        _emailService = emailService;
        _logger = logger;
    }

    // ── PropertyIndex redirect ──────────────────────────────────────
    [HttpGet]
    [Route("Section78/PropertyIndex")]
    public async Task<IActionResult> PropertyIndex()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        return RedirectToAction("Index", "PropertySearch",
            new { rollSource = "Query" });
    }

    // ── GET Section78Query ──────────────────────────────────────────
    [HttpGet]
    [Route("Section78/Section78Query")]
    public async Task<IActionResult> Section78Query(
        string? qtype,
        string? Direct,
        string? PropDesc,
        string? addr,
        string? cat,
        string? UKey,
        string? VKey,
        string? objectorType)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        // ── Store in TempData for V1-compatible view access ─────────
        TempData["CurrentFilter_PD"] = PropDesc;
        TempData["CurrentFilter_LSA"] = addr;
        TempData["CurrentFilter_CD"] = cat;
        TempData["CurrentFilter_UK"] = UKey?.Trim();
        TempData["CurrentFilter_VK"] = VKey?.Trim();
        TempData["PropertyType"] = Direct;
        TempData["objector_choice"] = objectorType;

        // ── Multi-purpose property data from DB ─────────────────────
        if (!string.IsNullOrWhiteSpace(UKey) && !string.IsNullOrWhiteSpace(VKey))
        {
            var detail = await _section78.GetPropertyDetailAsync(
                UKey.Trim(), VKey.Trim());

            if (detail is not null)
            {
                TempData["CurrentFilter_RA"] = detail.RateableArea;
                TempData["CurrentFilter_MV"] = detail.MarketValue;
                TempData["CurrentFilter_ON"] = detail.OwnerName;
                TempData["CurrentFilter_TN"] = detail.TownNameDesc;
                TempData["CurrentFilter_P_ID"] = detail.PremiseId;
                TempData["CurrentFilter_P_I"] = detail.PropertyId;
                TempData["CurrentFilter_S"] = detail.Sector;

                // Multi-purpose split values
                if (cat?.Contains("Multiple Purposes",
                        StringComparison.OrdinalIgnoreCase) == true)
                {
                    TempData["CurrentFilter_mult_purp_CAT"] = cat;
                    TempData["CurrentFilter_mult_purp_PA"] = addr;
                    TempData["CurrentFilter_mult_purp_EXT"] = detail.RateableArea;
                    TempData["CurrentFilter_mult_purp_MV"] = detail.MarketValue;
                }
            }
        }

        // ── Review vs Query routing ─────────────────────────────────
        if (qtype == "Review")
        {
            TempData["ReviewStat"] = "R";
            return Direct == "Multi"
                ? View("Section78QueryMulti")
                : View();
        }

        TempData["ReviewStat"] = "Q";

        return Direct == "Multi"
            ? View("Section78QueryMulti")
            : View();   // Views/Section78/Section78Query.cshtml
    }

    // ── POST Section78Query ─────────────────────────────────────────
    [HttpPost]
    [Route("Section78/Section78Query")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Section78Query(
        Que_Property_InfoModel que,
        Obj_Section1Model obj1,
        Obj_Section2Model obj2,
        Obj_Section2QueryModel que1,
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
        string? propertyType)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? throw new InvalidOperationException("Not authenticated.");
        var reviewStat = TempData["ReviewStat"]?.ToString() ?? "Q";

        var uploadRoot = _config["ObjectionRolls:Objection_Query:QueryRootPath"]
                  ?? throw new InvalidOperationException(
                      "ObjectionRolls:Objection_Query:QueryRootPath missing from appsettings.");

        // ── Wire query-type from objector type if not set ───────────
        if (string.IsNullOrEmpty(que.Query_Type))
            que.Query_Type = que.Query_Type ?? obj1.GetType()
                                               .GetProperty("Objector_Type")
                                               ?.GetValue(obj1)?.ToString();

        if (string.IsNullOrEmpty(que.Property_Type))
            que.Property_Type = propertyType;

        TempData.Keep("ReviewStat");

        var result = await _section78.SubmitQueryAsync(
            que, obj1, obj2, que1,
            objR3, objB3, objA3,
            objB4, objR4,
            obj5, obj6, obj7,
            obj_file, files, fileR,
            reviewStat, uploadRoot,
            propertyType ?? que.Property_Type ?? "Res",
            userId);

        
        // ── Populate TempData for acknowledgement view ──────────────
        TempData["pin"] = result.RandomPin;
        TempData["id"] = result.QueryRef;
        TempData["Count"] = result.FileCount;

        for (int i = 0; i < 10; i++)
            TempData[$"File{i + 1}"] = result.Files[i];

        TempData["Old_Property_Description"] =
            result.Section6?.Old_Property_Description?.Trim();
        TempData["Old_Category"] = result.Section6?.Old_Category;
        TempData["Old_Address"] = result.Section6?.Old_Address;
        TempData["Old_Extent"] = result.Section6?.Old_Extent?.ToString();
        TempData["Old_Market_Value"] = result.Section6?.Old_Market_Value?.ToString();
        TempData["Old_Owner"] = result.Section6?.Old_Owner;

        TempData["new_Property_Description"] = result.Section6?.New_Property_Description;
        TempData["new_Category"] = result.Section6?.New_Category;
        TempData["new_Address"] = result.Section6?.New_Address;
        TempData["new_Extent"] = result.Section6?.New_Extent?.ToString();
        TempData["new_Market_Value"] = result.Section6?.New_Market_Value?.ToString();
        TempData["new_Owner"] = result.Section6?.New_Owner;

        TempData["Old2_Category"] = result.Section6?.Old2_Category;
        TempData["Old2_Market_Value"] = result.Section6?.Old2_Market_Value?.ToString();
        TempData["Old2_Extent"] = result.Section6?.Old2_Extent?.ToString();
        TempData["Old3_Category"] = result.Section6?.Old3_Category;
        TempData["Old3_Market_Value"] = result.Section6?.Old3_Market_Value?.ToString();
        TempData["Old3_Extent"] = result.Section6?.Old3_Extent?.ToString();
        TempData["new2_Category"] = result.Section6?.New2_Category;
        TempData["new2_Market_Value"] = result.Section6?.New2_Market_Value?.ToString();
        TempData["new2_Extent"] = result.Section6?.New2_Extent?.ToString();
        TempData["new3_Category"] = result.Section6?.New3_Category;
        TempData["new3_Market_Value"] = result.Section6?.New3_Market_Value?.ToString();
        TempData["new3_Extent"] = result.Section6?.New3_Extent?.ToString();
        TempData["objection_reason"] = result.Section6?.Objection_Reasons;

      

        TempData["successmessage"] = "Query Submitted Successfully";

        return result.IsMulti
            ? RedirectToAction("MultiPurposeDisplay")
            : RedirectToAction("Display");
    }

    // ── Acknowledgement ─────────────────────────────────────────────
    [HttpGet]
    [Route("Section78/Display")]
    public async Task<IActionResult> Display()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.IsMulti = false;
        return View("Display");   // Views/Section78/Display.cshtml
    }

    [HttpGet]
    [Route("Section78/MultiPurposeDisplay")]
    public async Task<IActionResult> MultiPurposeDisplay()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.IsMulti = true;
        return View("Display");   // SAME view — flag tells it to show multi rows
    }
}