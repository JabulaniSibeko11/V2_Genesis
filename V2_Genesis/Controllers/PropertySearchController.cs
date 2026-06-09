using GenesisV2.Services.PropertySearch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Attributes;
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
    private readonly IAttributesSearchService _attributesService;

    private static readonly Regex AdminPattern =
    new(@"^val\.admin(1[0-9]?|[1-9])@joburg\.org\.za$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);


    public PropertySearchController(
        IPropertySearchService search,
        ApplicationDbContext db,
      IOptions<RollDatesSettings> rollDatesOpts, IConfiguration config, ILisSearchService lisSearchService, IOmissionService omissionService, IAttributesSearchService attributesService,
  ILogger<PropertySearchController> logger)
    {
        _search = search;
        _db = db;
        _rollDates = rollDatesOpts.Value;
        _config = config;
        _logger = logger;
        _lisSearchService = lisSearchService;
        _omissionService = omissionService;
        _attributesService = attributesService;
    }
    private bool IsAdmin(string? email) =>
      !string.IsNullOrEmpty(email) && (
          email.Equals("AdministrationEnquiries@Joburg.org.za",
              StringComparison.OrdinalIgnoreCase) ||
          AdminPattern.IsMatch(email));
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
    [AllowAnonymous]
    public async Task<IActionResult> ViewProperty(
     string rollSource,
     string unitKey,
     string valuationKey)
    {
        if (string.IsNullOrWhiteSpace(rollSource))
            return BadRequest("Roll source is required.");

        if (string.IsNullOrWhiteSpace(unitKey))
            return BadRequest("Unit key is required.");

        // ============================================================
        // ATTRIBUTES FLOW
        // Attributes is NOT in GvList, so do not check GvList
        // ============================================================
        if (rollSource.Equals("Attributes", StringComparison.OrdinalIgnoreCase))
        {
            var attrItem = await _attributesService.GetPropertyDetailAsync(unitKey);

            if (attrItem == null)
                return NotFound("Attribute property details not found.");

            // ── Also read propertyFrom query param ────────────────
            var propertyFrom = Request.Query["propertyFrom"]
                                      .FirstOrDefault() ?? "Attributes";

            HttpContext.Session.SetString("UnitKey", unitKey ?? string.Empty);
            HttpContext.Session.SetString("ValuationKey", valuationKey ?? string.Empty);
            HttpContext.Session.SetString("RollSource", "Attributes");

            ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

            var attrVm = new PropertyDetailViewModel
            {
                Items = new List<PropertyDetailResult>
        {
            MapAttributePropertyToResult(attrItem)
        },
                Roll = null,
                IsAttributes = true
            };

            return View(attrVm);
        }



        // ============================================================
        // NORMAL ROLL FLOW
        // GV23 / SUPP / QUERY etc.
        // ============================================================
        var roll = await _db.GvList
            .FirstOrDefaultAsync(r => r.Source == rollSource);

        if (roll is null)
            return NotFound($"Roll '{rollSource}' not found.");

        unitKey = NormalizeKey(unitKey) ?? string.Empty;
        valuationKey = NormalizeKey(valuationKey) ?? string.Empty;
        var items = await _search.GetPropertyDetailsAsync(
            rollSource,
            unitKey,
            valuationKey);

        if (!items.Any())
            return NotFound("Property details not found.");

        HttpContext.Session.SetString("UnitKey", unitKey ?? string.Empty);
        HttpContext.Session.SetString("ValuationKey", valuationKey ?? string.Empty);
        HttpContext.Session.SetString("RollSource", rollSource);

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        var dates = _rollDates.For(rollSource);


        var vm = new PropertyDetailViewModel
        {

            Items = items,
            Roll = roll,
            OpenDate = rollSource.Equals("Query", StringComparison.OrdinalIgnoreCase)
        ? null
        : dates?.OpenDate,

            VisibleUntil = rollSource.Equals("Query", StringComparison.OrdinalIgnoreCase)
        ? null
        : dates?.VisibleUntil,
            IsAttributes = false
        };

        return View(vm);
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

    private static PropertyDetailResult MapAttributePropertyToResult(
       LisPropertyDetail d)
    {
        return new PropertyDetailResult
        {
            // ── Identifiers ───────────────────────────────────────
            UnitKey = d.UnitKey,           // ← CRITICAL for link form
            ValuationKey = d.ValuationKey,
            Id = d.UnitKey.ToString(),
            PremiseId = d.PremiseId,

            // ── Property ──────────────────────────────────────────
            PropertyDesc = d.PropertyDesc,
            TownNameDesc = d.TownNameDesc,
            Erf = d.Erf,
            Ptn = d.Ptn,
            Re = d.Re,
            LisStreetAddress = d.LisStreetAddress,
            OwnerName = d.OwnerName,
            LeaseDesc = null,

            // ── Valuation ─────────────────────────────────────────
            CatDesc = d.CatDesc,
            RateableArea = d.RateableAreaVal ?? d.RateableArea,
            MarketValue = d.MarketValue,
            WefDate = d.WefDate,
            ValuationDate = d.ValuationDate,
            Reason = d.Reason,

            // ── Scheme ────────────────────────────────────────────
            SchemeName = d.SchemeName,
            SchemeNumber = d.SchemeNumber,
            SchemeYear = d.SchemeYear,
            UnitNo = d.UnitNo,
        };
    }

    [HttpGet]
    [Route("property/save")]
    [Authorize]
    public async Task<IActionResult> SaveRecord(
        string rollSource,
        int key,
        string sourceTable)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var currentEmail = User.FindFirstValue(ClaimTypes.Name) ?? "";

        bool isAdmin = !string.IsNullOrEmpty(currentEmail) && (
            currentEmail.Equals("AdministrationEnquiries@Joburg.org.za",
                StringComparison.OrdinalIgnoreCase) ||
            System.Text.RegularExpressions.Regex.IsMatch(
                currentEmail,
                @"^val\.admin(1[0-9]?|[1-9])@joburg\.org\.za$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));

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

        if (isAdmin)
        {
            return RedirectToAction("Index", "Admin");
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
            // FIX: use _search (DefaultConnection + Objection.dbo SPs) — same source
            //      as the home and property search pages. OmissionService was connecting
            //      to the roll-specific DB (e.g. Sup3) which doesn't have those SPs.
            var townsTask = _search.GetTownshipsAsync();
            var schemesTask = _search.GetSchemesAsync();
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
       string? propType,
       // Freehold
       string? FH_Town,
       string? FH_ERF,
       string? FH_Portion,
       string? FH_RE,
       string? FH_Right,
       string? FH_Address,
       // Sectional Title
       string? ST_Scheme,
       string? ST_SchemeNumber,
       string? ST_SchemeYear,
       string? ST_Unit,
       string? ST_Right)
    {
        // ── Call service to build desc + resolve correct roll target ───────
        var (propertyDesc, sourceTable, controllerName) =
            _omissionService.BuildOmissionDescription(
                rollSource,
                propType ?? "FH",
                FH_Town,
                FH_ERF,
                FH_Portion,
                FH_RE,
                FH_Right,
                ST_Scheme,
                ST_SchemeNumber,
                ST_SchemeYear,
                ST_Unit,
                ST_Right);

        var town = (propType == "ST" ? FH_Town : FH_Town)?.Trim();

        _logger.LogInformation(
            "[Omission] {RollSource} → controller={Ctrl} sourceTable={St} desc={Desc}",
            rollSource, controllerName, sourceTable, propertyDesc);

        // ── Set TempData ───────────────────────────────────────────────────
        TempData["OmissionStatus"] = "True";
        TempData["OmittedPropertyDesc"] = propertyDesc;
        TempData["OmittedTownName"] = town;
        TempData["RollSource"] = rollSource;
        TempData["Omission_Address"] = FH_Address?.Trim();
        TempData["Omission_Stand"] = FH_ERF?.Trim();
        TempData["Omission_Scheme"] = propType == "ST" ? ST_Scheme?.Trim() : null;
        TempData["Omission_Unit"] = propType == "ST" ? ST_Unit?.Trim() : null;

        // ── Redirect — pass sourceTable so CheckProperty picks up the right
        //   controller and DB connection without defaulting to Sup3 ─────────
        return RedirectToAction("CheckProperty", "Objection",
            new
            {
                rollSource,
                sourceTable,          // ← now correctly set e.g. "GV23-SUP2"
                PropertyFrom = rollSource,
                omission = true
            });
    }
}