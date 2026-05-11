using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers;

[Authorize(Roles = "Client")]
public class AttributesController : Controller
{
    private readonly IAttributesSearchService _attrSearch;
    private readonly IPropertySearchService _propSearch;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AttributesController> _logger;
    private readonly IAttributeSubmissionService _attributeService;
    public AttributesController(
        IAttributesSearchService attrSearch,
        IPropertySearchService propSearch,
        ApplicationDbContext db,
        ILogger<AttributesController> logger,IAttributeSubmissionService attributeService)
    {
        _attrSearch = attrSearch;
        _propSearch = propSearch;
        _db = db;
        _logger = logger;
        _attributeService = attributeService;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("attributes/about")]
    public async Task<IActionResult> About()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("attributes/search")]
    public async Task<IActionResult> Search()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.Townships = await _propSearch.GetTownshipsAsync();
        ViewBag.Schemes = await _propSearch.GetSchemesAsync();
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("attributes/search")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(
        string? SearchTownName, string? SearchStand,
        string? SearchAddress, string? SearchScheme, string? SearchUnit)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.Townships = await _propSearch.GetTownshipsAsync();
        ViewBag.Schemes = await _propSearch.GetSchemesAsync();
        ViewBag.IsAuth = User.Identity?.IsAuthenticated == true;

        if (string.IsNullOrWhiteSpace(SearchTownName) &&
            string.IsNullOrWhiteSpace(SearchStand) &&
            string.IsNullOrWhiteSpace(SearchAddress) &&
            string.IsNullOrWhiteSpace(SearchScheme) &&
            string.IsNullOrWhiteSpace(SearchUnit))
        {
            ViewBag.Error = "Please enter at least one search field.";
            return View();
        }

        var p = new PropertySearchParams
        {
            TownName = SearchTownName?.Trim() ?? string.Empty,
            Stand = string.IsNullOrWhiteSpace(SearchStand) ? null : SearchStand.Trim(),
            Address = string.IsNullOrWhiteSpace(SearchAddress) ? null : SearchAddress.Trim(),
            Scheme = string.IsNullOrWhiteSpace(SearchScheme) ? null : SearchScheme.Trim(),
            Unit = string.IsNullOrWhiteSpace(SearchUnit) ? null : SearchUnit.Trim(),
        };

        var results = await _attrSearch.SearchAsync(p);
        ViewBag.SearchParams = p;
        ViewBag.Results = results;
        ViewBag.ResultCount = results.Count;
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [Route("attributes/link")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkProperty(string idProperty, string propertyFrom)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var result = await _attrSearch.LinkPropertyAsync(idProperty, userId, propertyFrom);
            TempData[result.IsDuplicate ? "AttrLinkInfo" : "AttrLinkSuccess"] =
                result.IsDuplicate
                    ? "This property is already linked to your profile."
                    : "Property linked. You can now submit attributes from your dashboard.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Attributes] Link failed — {P} {U}", idProperty, userId);
            TempData["AttrLinkError"] = "Could not link this property. Please try again.";
        }
        return RedirectToAction("Index", "Dashboard");
    }
    [HttpGet]
    [Authorize]
    [Route("attributes/form")]
    public IActionResult Form(string idProperty, string formType = "Residential")
    {
        if (string.IsNullOrWhiteSpace(idProperty))
        {
            TempData["AttrLinkError"] = "Property reference was not supplied.";
            return RedirectToAction("Index", "Dashboard");
        }

        var model = _attributeService.CreateNew(formType);

        model.PropertyDetails.PropertyId = idProperty;
       // model.PropertyDetails.PremiseId = idProperty;

        return View("Create", model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AttributeSubmissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.Identity?.Name ?? "anonymous";
        var userName = User.Identity?.Name ?? "Client";

        var attrId = await _attributeService.SubmitAsync(model, userId, userName);

        TempData["Success"] = "Attribute submission saved successfully.";

        return RedirectToAction("Details", new { id = attrId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var model = await _attributeService.GetForReviewAsync(id);

        if (model == null)
            return NotFound();

        return View(model);
    }
}
