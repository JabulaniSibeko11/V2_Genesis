
using GenesisV2.Services.PropertySearch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.LIS;
using V2_Genesis.Models.Results;
using V2_Genesis.Services.Implementations;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Controllers;

[Authorize]
public class PropertySearchController : Controller
{
    private readonly IPropertySearchService _search;
    private readonly ApplicationDbContext _db;
    private readonly RollDatesSettings _rollDates;
    private readonly IConfiguration _config;
    private readonly ILisSearchService _lisSearchService;
    private readonly IOmissionService _omissionService;
    private readonly ILogger<PropertySearchController> _logger;

    public PropertySearchController(
        IPropertySearchService search,
        ApplicationDbContext db,
      IOptions<RollDatesSettings> rollDatesOpts,IConfiguration config,ILisSearchService lisSearchService,IOmissionService omissionService,
  ILogger<PropertySearchController> logger)
    {
        _search = search;
        _db = db;
        _rollDates = rollDatesOpts.Value;
        _config = config;
        _logger = logger;
        _lisSearchService = lisSearchService;
        _omissionService = omissionService;
    }

    // ── GET /search/{rollSource} ──────────────────────────────────────
    [HttpGet]
    [Route("search/{rollSource}")]
    public async Task<IActionResult> Index(string rollSource)
    {
        // Load the roll info from GV_LIST
        var roll = await _db.GvList
            .FirstOrDefaultAsync(r => r.Source == rollSource);

        if (roll is null)
            return NotFound($"Roll '{rollSource}' not found.");

        // Validate this roll has a search config
        if (!RollSearchRegistry.Configs.ContainsKey(rollSource))
            return NotFound($"No search configuration found for '{rollSource}'.");

        // Load shared township + scheme lists (same across all rolls)
        var townships = await _search.GetTownshipsAsync();
        var schemes = await _search.GetSchemesAsync();

        ViewBag.Roll = roll;
        ViewBag.Townships = townships;
        ViewBag.Schemes = schemes;
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        return View(new PropertySearchParams());
    }

    // ── POST /search/{rollSource} — returns partial (AJAX) ───────────
    [HttpPost]
    [Route("search/{rollSource}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(string rollSource, PropertySearchParams @params)
    {
        var roll = await _db.GvList
            .FirstOrDefaultAsync(r => r.Source == rollSource);

        if (!ModelState.IsValid || roll is null)
            return PartialView("_NoResults", roll);

        var results = await _search.SearchAsync(rollSource, @params);

        if (!results.Any())
            return PartialView("_NoResults", roll);

        ViewBag.Roll = roll;
        ViewBag.Params = @params;
        return PartialView("_Results", results);
    }
    [HttpGet]
    [Route("property/view")]
    public async Task<IActionResult> ViewProperty(
    string rollSource,
    string unitKey,
    string valuationKey)
    {
        var roll = await _db.GvList
            .FirstOrDefaultAsync(r => r.Source == rollSource);

        if (roll is null) return NotFound($"Roll '{rollSource}' not found.");

        var items = await _search.GetPropertyDetailsAsync(
            rollSource, unitKey, valuationKey);

        if (!items.Any()) return NotFound("Property details not found.");

        HttpContext.Session.SetString("UnitKey", unitKey);
        HttpContext.Session.SetString("ValuationKey", valuationKey);
        HttpContext.Session.SetString("RollSource", rollSource);

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        // ── Look up this roll's specific dates ────────────────────────────
        var dates = _rollDates.For(rollSource);

        var vm = new PropertyDetailViewModel
        {
            Items = items,
            Roll = roll,
            OpenDate = dates.OpenDate,
            VisibleUntil = dates.VisibleUntil
        };

        return View(vm);
    }
    [HttpGet]
    [Route("property/save")]
    [Authorize]
    public async Task<IActionResult> SaveRecord(
     string rollSource,
     int key,
     string sourceTable)
    {
        // Must be authenticated
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        try
        {
            var result = await _search.LinkPropertyAsync(
                rollSource: rollSource,
                idProperty: key.ToString(),
                userId: userId,
                propertyFrom: sourceTable);

            if (result.Success)
            {
                TempData["LinkSuccess"] = "Property successfully linked to your profile.";
                _logger.LogInformation(
                    "User {UserId} linked property {Key} from {Roll}.",
                    userId, key, rollSource);
            }
            else if (result.IsDuplicate)
            {
                TempData["LinkError"] = result.ErrorMessage;
            }
            else
            {
                TempData["LinkError"] = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            TempData["LinkError"] = "An error occurred while linking the property. Please try again.";
            _logger.LogError(ex,
                "Error linking property {Key} for user {UserId} on roll {Roll}.",
                key, userId, rollSource);
        }

        return RedirectToAction("Index", "Dashboard");
    }
    [HttpPost]
    [Authorize]
    [Route("search/{rollSource}/lis")]
    public async Task<IActionResult> SearchLis(
    string rollSource,
    string? SearchTownName,
    string? SearchStand,
    string? SearchAddress,
    string? SearchScheme,
    string? SearchUnit,
    string? SearchOwner)
    {
        var p = new LisSearchParams
        {
            SearchTownName = SearchTownName,
            SearchStand = SearchStand,
            SearchAddress = SearchAddress,
            SearchScheme = SearchScheme,
            SearchUnit = SearchUnit,
            SearchOwner = SearchOwner,
        };

        var lisResults = await _lisSearchService.SearchAsync(rollSource, p);

        if (!lisResults.Any())
        {
            // Load towns + schemes in parallel so the omission form
            // is ready without a second round-trip
            var townsTask = _omissionService.GetTownsAsync(rollSource);
            var schemesTask = _omissionService.GetSchemesAsync(rollSource);
            await Task.WhenAll(townsTask, schemesTask);

            var roll = await _db.GvList
                .FirstOrDefaultAsync(r => r.Source == rollSource);

            ViewBag.RollSource = rollSource;
            ViewBag.RollName = roll?.Name ?? rollSource;
            ViewBag.Towns = townsTask.Result;
            ViewBag.Schemes = schemesTask.Result;
            ViewBag.SearchParams = p;          // pre-fill editable fields

            return PartialView("_LisNoResults");
        }

        // Map and return results in the standard partial
        var mapped = lisResults.Select(l => new PropertySearchResult
        {
            TownNameDesc = l.TownNameDescription,
            LisStreetAddress = l.LisStreetAddress,
            Erf = l.Erf,
            Ptn = l.Ptn,
            Re = l.Re,
            CatDesc = l.CATDescription,
            RateableArea = l.RateableArea,
            MarketValue = l.MarketValue,
            SchemeName = l.SchemeName,
            SchemeNumber = l.SchemeNumber,
            SchemeYear = l.SchemeYear,
            Lease = l.Lease,
            UnitNo = int.TryParse(l.UnitNo, out var u) ? u : 0,
            Reason = l.Reason,
            UnitKey = l.UnitKey,
            ValuationKey = l.ValuationKey,
        }).ToList();

        var rollRecord = await _db.GvList
            .FirstOrDefaultAsync(r => r.Source == rollSource);

        ViewBag.Roll = rollRecord;
        ViewBag.IsLisSearch = true;

        return PartialView("_PropertySearchResults", mapped);
    }


    // ── POST /search/{rollSource}/omission ────────────────────────────────
    // Called when client confirms their details and clicks "Lodge as Omission"
    [HttpPost]
    [Authorize]
    [Route("search/{rollSource}/omission")]
    [ValidateAntiForgeryToken]
    public IActionResult SubmitOmission(
        string rollSource,
        string? FH_Town,
        string? FH_Address,
        string? FH_Stand,
        string? FH_Scheme,
        string? FH_Unit,
        string? FH_Description)
    {
        // Build a human-readable description of the omitted property
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(FH_Stand))
            parts.Add($"ERF {FH_Stand}");
        if (!string.IsNullOrWhiteSpace(FH_Address))
            parts.Add(FH_Address);
        if (!string.IsNullOrWhiteSpace(FH_Scheme))
            parts.Add($"Scheme: {FH_Scheme}");
        if (!string.IsNullOrWhiteSpace(FH_Unit))
            parts.Add($"Unit {FH_Unit}");

        var propertyDesc = string.IsNullOrWhiteSpace(FH_Description)
            ? string.Join(" | ", parts)
            : FH_Description.Trim();

        // Set omission flags — CheckProperty reads these from TempData
        TempData["OmissionStatus"] = "True";
        TempData["OmittedTownName"] = FH_Town?.Trim();
        TempData["OmittedPropertyDesc"] = propertyDesc;
        TempData["RollSource"] = rollSource;

        // Store all fields so CheckProperty / objection form can access them
        TempData["Omission_Address"] = FH_Address?.Trim();
        TempData["Omission_Stand"] = FH_Stand?.Trim();
        TempData["Omission_Scheme"] = FH_Scheme?.Trim();
        TempData["Omission_Unit"] = FH_Unit?.Trim();

        // Redirect to the V2 CheckProperty flow with omission source
        return RedirectToAction("CheckProperty", "Objection",
            new { rollSource, PropertyFrom = rollSource, omission = true });
    }
}