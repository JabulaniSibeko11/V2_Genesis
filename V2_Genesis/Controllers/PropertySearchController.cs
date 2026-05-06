
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
using V2_Genesis.Models.Results;
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
    private readonly ILogger<PropertySearchController> _logger;

    public PropertySearchController(
        IPropertySearchService search,
        ApplicationDbContext db,
      IOptions<RollDatesSettings> rollDatesOpts,IConfiguration config,
  ILogger<PropertySearchController> logger)
    {
        _search = search;
        _db = db;
        _rollDates = rollDatesOpts.Value;
        _config = config;
        _logger = logger;
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
}