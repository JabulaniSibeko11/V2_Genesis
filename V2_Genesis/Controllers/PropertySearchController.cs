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
using V2_Genesis.Models.Lis;
using V2_Genesis.Models.LIS;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.Section78;
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
       !string.IsNullOrWhiteSpace(email) &&
       (
           email.Equals(
               "AdministrationEnquiries@Joburg.org.za",
               StringComparison.OrdinalIgnoreCase)
           ||
           AdminPattern.IsMatch(email)
       );

    private static bool IsQueryRoll(string? rollSource)
    {
        return string.Equals(
            rollSource,
            "Query",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveReviewStatus(
        DateTime? reviewCloseDate)
    {
        /*
         * Current business rule:
         *
         * NULL Review_Close_Date:
         *     Initial Section 78 Query is open.
         *
         * Today or future date:
         *     Review period is open.
         *
         * Past date:
         *     Review period is closed.
         */
        if (!reviewCloseDate.HasValue)
            return Section78ReviewStatus.Open;

        return reviewCloseDate.Value.Date >= DateTime.Today
            ? Section78ReviewStatus.Open
            : Section78ReviewStatus.Closed;
    }

    private static void ApplySection78ReviewStatus(
        IEnumerable<PropertyDetailResult> properties)
    {
        foreach (var property in properties)
        {
            property.Review_Status =
                ResolveReviewStatus(
                    property.Review_Close_Date);
        }
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

        // Supplementary rolls use only their own townships. GV uses the
        // complete list; LIS also keeps the complete list in SearchLis.
        var townships = await _search.GetTownshipsAsync(rollSource);
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
      string valuationKey,
      string? propertyFrom)
    {
        if (string.IsNullOrWhiteSpace(rollSource))
            return BadRequest("Roll source is required.");

        propertyFrom = string.IsNullOrWhiteSpace(propertyFrom)
            ? rollSource
            : propertyFrom.Trim();

        unitKey = NormalizeKey(unitKey) ?? string.Empty;
        valuationKey = NormalizeKey(valuationKey) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(unitKey) &&
            string.IsNullOrWhiteSpace(valuationKey))
        {
            return BadRequest("A unit key or valuation key is required.");
        }

        var isUniversalSearch = propertyFrom.Equals(
            "UniversalSearch",
            StringComparison.OrdinalIgnoreCase);

        // ============================================================
        // ATTRIBUTES FLOW
        // Attributes is NOT in GvList, so do not check GvList
        // ============================================================
        if (rollSource.Equals("Attributes", StringComparison.OrdinalIgnoreCase))
        {
            var attrItem = await _attributesService.GetPropertyDetailAsync(unitKey);

            if (attrItem == null)
                return NotFound("Attribute property details not found.");

            HttpContext.Session.SetString("UnitKey", unitKey);
            HttpContext.Session.SetString("ValuationKey", valuationKey);
            HttpContext.Session.SetString("RollSource", "Attributes");
            HttpContext.Session.SetString("PropertyFrom", "Attributes");

            ViewBag.GvList = await _db.GvList
                .OrderBy(r => r.ID)
                .ToListAsync();

            var attrVm = new PropertyDetailViewModel
            {
                Items = new List<PropertyDetailResult>
            {
                MapAttributePropertyToResult(attrItem)
            },
                Roll = null,
                IsAttributes = true,
                IsLis = false
            };

            return View(attrVm);
        }

        // ============================================================
        // LIS FLOW
        // Property was not found on the roll, but found on LIS.
        // Do NOT call normal roll property detail SP here.
        // ============================================================
        if (propertyFrom.Equals("LIS", StringComparison.OrdinalIgnoreCase))
        {
            var roll = await _db.GvList
                .FirstOrDefaultAsync(r => r.Source == rollSource);

            if (roll is null)
                return NotFound($"Roll '{rollSource}' not found.");

            var lisItem = await _lisSearchService.GetPropertyByKeysAsync(
                rollSource,
                unitKey,
                valuationKey);

            if (lisItem == null)
                return NotFound("LIS property details not found.");

            HttpContext.Session.SetString("UnitKey", unitKey);
            HttpContext.Session.SetString("ValuationKey", valuationKey);
            HttpContext.Session.SetString("RollSource", rollSource);
            HttpContext.Session.SetString("PropertyFrom", "LIS");

            if (IsQueryRoll(rollSource))
            {
                var reviewStatus =
                    ResolveReviewStatus(
                        lisItem.ReviewCloseDate);

                HttpContext.Session.SetString(
                    "ReviewStatus",
                    reviewStatus);

                if (lisItem.ReviewCloseDate.HasValue)
                {
                    HttpContext.Session.SetString(
                        "ReviewCloseDate",
                        lisItem.ReviewCloseDate.Value
                            .ToString(
                                "yyyy-MM-dd",
                                CultureInfo.InvariantCulture));
                }
                else
                {
                    HttpContext.Session.Remove(
                        "ReviewCloseDate");
                }
            }

            ViewBag.GvList = await _db.GvList
                .OrderBy(r => r.ID)
                .ToListAsync();

            var dates = _rollDates.For(rollSource);

            var mappedLisProperty =
      MapLisPropertyToResult(lisItem);

            if (IsQueryRoll(rollSource))
            {
                mappedLisProperty.Review_Status =
                    ResolveReviewStatus(
                        mappedLisProperty.Review_Close_Date);
            }

            var lisVm = new PropertyDetailViewModel
            {
                Items = new List<PropertyDetailResult>
    {
        mappedLisProperty
    },

                Roll = roll,

                OpenDate = IsQueryRoll(rollSource)
                    ? null
                    : dates?.OpenDate,

                VisibleUntil = IsQueryRoll(rollSource)
                    ? null
                    : dates?.VisibleUntil,

                IsAttributes = false,
                IsLis = true
            };

            return View(lisVm);
        }

        // ============================================================
        // NORMAL ROLL FLOW
        // GV23 / SUPP / QUERY etc.
        // ============================================================
        var normalRoll = await _db.GvList
            .FirstOrDefaultAsync(r => r.Source == rollSource);

        if (normalRoll is null)
            return NotFound($"Roll '{rollSource}' not found.");

        var items = await _search.GetPropertyDetailsAsync(
      rollSource,
      unitKey,
      valuationKey);

        if (!items.Any())
            return NotFound("Property details not found.");

        if (IsQueryRoll(rollSource))
        {
            ApplySection78ReviewStatus(items);
        }

        if (!items.Any())
            return NotFound("Property details not found.");

        HttpContext.Session.SetString("UnitKey", unitKey);
        HttpContext.Session.SetString("ValuationKey", valuationKey);
        HttpContext.Session.SetString("RollSource", rollSource);
        HttpContext.Session.SetString(
            "PropertyFrom",
            isUniversalSearch ? "UniversalSearch" : rollSource);

        if (isUniversalSearch)
        {
            // Global Search is a public, read-only property lookup.
            // Do not pass owner information to its view model.
            foreach (var item in items)
            {
                item.OwnerName = null;
            }
        }

        ViewBag.GvList = await _db.GvList
            .OrderBy(r => r.ID)
            .ToListAsync();

        var rollDates = _rollDates.For(rollSource);

        var vm = new PropertyDetailViewModel
        {
            Items = items,
            Roll = normalRoll,
            OpenDate = rollSource.Equals("Query", StringComparison.OrdinalIgnoreCase)
                ? null
                : rollDates?.OpenDate,
            VisibleUntil = rollSource.Equals("Query", StringComparison.OrdinalIgnoreCase)
                ? null
                : rollDates?.VisibleUntil,
            IsAttributes = false,
            IsLis = false,
            IsUniversalSearch = isUniversalSearch,
            CanViewOwnerDetails = !isUniversalSearch &&
                User.Identity?.IsAuthenticated == true
        };

        return View(vm);
    }
    private static PropertyDetailResult MapLisPropertyToResult(
        LisProperty property)
    {
        return new PropertyDetailResult
        {
            TownNameDesc =
                property.TownNameDescription,

            PropertyDesc =
                !string.IsNullOrWhiteSpace(
                    property.PropertyDescription)
                    ? property.PropertyDescription
                    : BuildLisPropertyDescription(property),

            LisStreetAddress =
                property.LisStreetAddress,

            Erf =
                property.Erf,

            Ptn =
                property.Ptn.ToString(),

            Re =
                string.IsNullOrWhiteSpace(property.Re)
                    ? "-"
                    : property.Re,

            CatDesc =
                property.CATDescription,

            RateableArea =
                property.RateableArea,

            MarketValue =
                property.MarketValue,

            SchemeName =
                property.SchemeName,

            SchemeNumber =
                property.SchemeNumber,

            SchemeYear =
                property.SchemeYear,

            UnitNo =
                int.TryParse(
                    property.UnitNo,
                    out var unitNumber)
                        ? unitNumber
                        : 0,

            Reason =
                property.Reason,

            UnitKey =
                property.UnitKey,

            ValuationKey =
                property.ValuationKey,

            WefDate =
                property.ValuationEffectiveDateWefDate,

            AdditionalNotes =
                property.AdditionalNotes,

            ValuationDate =
                property.ValuationEndDate,

            OwnerName =
                property.OwnerName,

            PremiseId =
                property.PremiseId,

            PropertyId =
                property.PropertyId,

            Sector =
                property.Sector,

            /*
             * Section 78 Review information.
             */
            Review_Close_Date =
                property.ReviewCloseDate,

            Review_Status =
                ResolveReviewStatus(
                    property.ReviewCloseDate)
        };
    }
    private static string BuildLisPropertyDescription(V2_Genesis.Models.Lis.LisProperty l)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(l.Re))
            parts.Add(l.Re);

        if (l.Ptn > 0)
            parts.Add($"PORTION {l.Ptn}");

        if (l.Erf > 0)
            parts.Add($"ERF {l.Erf}");

        if (!string.IsNullOrWhiteSpace(l.TownNameDescription))
            parts.Add(l.TownNameDescription);

        if (!string.IsNullOrWhiteSpace(l.SchemeName))
            parts.Add(l.SchemeName);

        if (!string.IsNullOrWhiteSpace(l.UnitNo))
            parts.Add($"UNIT {l.UnitNo}");

        return parts.Any()
            ? string.Join(" ", parts)
            : "LIS Property";
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
     string? key,
     string? sourceTable,
     string? propertyFrom,
     string? unitKey,
     string? valuationKey,
     string? propertyId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        propertyFrom = string.IsNullOrWhiteSpace(propertyFrom)
            ? sourceTable
            : propertyFrom.Trim();

        var isLis = string.Equals(
        propertyFrom,
        "LIS",
        StringComparison.OrdinalIgnoreCase);

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
            var idProperty = isLis
                ? FirstNotEmpty(propertyId, key, unitKey, valuationKey)
                : key;

            if (string.IsNullOrWhiteSpace(idProperty))
            {
                TempData["LinkError"] = "Property could not be linked because the property key is missing.";

                _logger.LogWarning(
                    "Property link failed. Missing key. User={UserId}, Roll={Roll}, PropertyFrom={PropertyFrom}, Key={Key}, PropertyId={PropertyId}, UnitKey={UnitKey}, ValuationKey={ValuationKey}",
                    userId,
                    rollSource,
                    propertyFrom,
                    key,
                    propertyId,
                    unitKey,
                    valuationKey);

                return isAdmin
                    ? RedirectToAction("Index", "Admin")
                    : RedirectToAction("Index", "Dashboard");
            }

            var linkPropertyFrom = isLis
                ? "LIS"
                : sourceTable ?? rollSource;

            var result = await _search.LinkPropertyAsync(
                rollSource: rollSource,
                idProperty: idProperty,
                userId: userId,
                propertyFrom: linkPropertyFrom);

            if (result.Success)
            {
                var isQuery =
                    IsQueryRoll(rollSource);

                if (isQuery)
                {
                    TempData["ReviewStatus"] =
                        result.ReviewStatus
                        ?? Section78ReviewStatus.Open;

                    if (result.ReviewCloseDate.HasValue)
                    {
                        TempData["ReviewCloseDate"] =
                            result.ReviewCloseDate.Value
                                .ToString(
                                    "dd MMMM yyyy",
                                    CultureInfo.GetCultureInfo("en-ZA"));
                    }

                    TempData["LinkSuccess"] =
                        result.ReviewStatus?.Equals(
                            Section78ReviewStatus.Closed,
                            StringComparison.OrdinalIgnoreCase) == true

                            ? "Property linked successfully. The Section 78 review period for this property is closed."

                            : "Property linked successfully. You can continue with the available Section 78 process from your dashboard.";
                }
                else
                {
                    TempData["LinkSuccess"] = isLis
                        ? "LIS property successfully linked to your profile."
                        : "Property successfully linked to your profile.";
                }

                _logger.LogInformation(
                    "User {UserId} linked property {PropertyKey} from {Roll}. " +
                    "PropertyFrom={PropertyFrom}, PropertyId={PropertyId}, " +
                    "UnitKey={UnitKey}, ValuationKey={ValuationKey}, " +
                    "ReviewStatus={ReviewStatus}, ReviewCloseDate={ReviewCloseDate}",
                    userId,
                    idProperty,
                    rollSource,
                    linkPropertyFrom,
                    propertyId,
                    unitKey,
                    valuationKey,
                    result.ReviewStatus,
                    result.ReviewCloseDate);
            }
            else
            {
                TempData["LinkError"] =
                    result.ErrorMessage
                    ?? "The property could not be linked.";
            }

        }
        catch (Exception ex)
        {
            TempData["LinkError"] = "An error occurred while linking the property. Please try again.";

            _logger.LogError(
                ex,
                "Error linking property. User={UserId}, Roll={Roll}, PropertyFrom={PropertyFrom}, Key={Key}, PropertyId={PropertyId}, UnitKey={UnitKey}, ValuationKey={ValuationKey}",
                userId,
                rollSource,
                propertyFrom,
                key,
                propertyId,
                unitKey,
                valuationKey);
        }

        if (isAdmin)
        {
            return RedirectToAction("Index", "Admin", new
            {
                openRoll = rollSource
            });
        }

        return RedirectToAction("Index", "Dashboard", new
        {
            openRoll = rollSource
        });
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
        _logger.LogInformation(
    "[LIS] Search returned {Count} records for Roll={Roll}, Town={Town}, Stand={Stand}, Address={Address}, Scheme={Scheme}, Unit={Unit}",
    lisResults.Count(),
    rollSource,
    SearchTownName,
    SearchStand,
    SearchAddress,
    SearchScheme,
    SearchUnit);

        var firstLis = lisResults.FirstOrDefault();

        if (firstLis != null)
        {
            _logger.LogInformation(
                "[LIS] First result: {@FirstLis}",
                firstLis);
        }

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
        var mapped = lisResults
     .Select(property => new PropertySearchResult
     {
         TownNameDesc =
             property.TownNameDescription,

         LisStreetAddress =
             property.LisStreetAddress,

         Erf =
             property.Erf,

         Ptn =
             property.Ptn,

         Re =
             property.Re,

         CatDesc =
             property.CATDescription,

         RateableArea =
             property.RateableArea,

         MarketValue =
             property.MarketValue,

         SchemeName =
             property.SchemeName,

         SchemeNumber =
             property.SchemeNumber,

         SchemeYear =
             property.SchemeYear,

         Lease =
             property.Lease,

         UnitNo =
             int.TryParse(
                 property.UnitNo,
                 out var unitNumber)
                     ? unitNumber
                     : 0,

         Reason =
             property.Reason,

         UnitKey =
             property.UnitKey,

         ValuationKey =
             property.ValuationKey,

         Review_Close_Date =
             property.ReviewCloseDate
     })
     .ToList();

        var rollRecord = await _db.GvList
            .FirstOrDefaultAsync(r => r.Source == rollSource);

        ViewBag.Roll = rollRecord;
        ViewBag.IsLisSearch = true;

        return PartialView("_Results", mapped);
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
