using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Attributes;
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

    // AttributesController.cs — updated Check + CheckConfirm + Form actions

    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("attributes/check")]
    public async Task<IActionResult> Check(string idProperty, string formType = "Residential")
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        if (string.IsNullOrWhiteSpace(idProperty))
        {
            TempData["AttrLinkError"] = "Property reference was not supplied.";
            return RedirectToAction("Index", "Dashboard");
        }

        // Load from LIS_20260116 + SAP_Contact0126
        var detail = await _attrSearch.GetPropertyDetailAsync(idProperty);

        // Auto-detect form type from CatDesc
        if (detail is not null)
            formType = ResolveFormType(detail.CatDesc) ?? formType;

        formType = formType switch
        {
            "Business" => "BusinessCommercial",
            "DRC" => "DRCMethod",
            "Residential-ST" => "ResidentialST",
            _ => formType
        };

        // Stash full detail for Form pre-fill
        if (detail is not null)
            TempData["Attr_Detail_Json"] =
                System.Text.Json.JsonSerializer.Serialize(detail);

        var vm = detail is not null
            ? new CheckAttributesViewModel
            {
                IDProperty = idProperty,
                FormType = formType,
                PropertyDesc = detail.PropertyDesc,
                CatDesc = detail.CatDesc,
                TownNameDesc = detail.TownNameDesc,
                LisStreetAddress = detail.LisStreetAddress,
                MarketValue = detail.MarketValue,
                RateableArea = detail.RateableAreaVal ?? detail.RateableArea,
                Erf = detail.Erf,
                Ptn = detail.Ptn,
                Re = detail.Re,
                SchemeName = detail.SchemeName,
                SchemeNumber = detail.SchemeNumber,
                SchemeYear = detail.SchemeYear,
                UnitNo = detail.UnitNo,
                OwnerName = detail.OwnerName,
                ValuationDate = detail.ValuationDate,
                Reason = detail.Reason,
                Zoning = detail.Zoning,
            }
            : new CheckAttributesViewModel { IDProperty = idProperty, FormType = formType };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [Route("attributes/check")]
    [ValidateAntiForgeryToken]
    public IActionResult CheckConfirm(
      string idProperty,
      string formType,
      string declarationType,
      string? ownerIdNumber)       // ← NEW: only sent when Owner selected
    {
        if (string.IsNullOrWhiteSpace(declarationType))
        {
            TempData["AttrCheckError"] =
                "Please select whether you are the Owner or Representative.";
            return RedirectToAction("Check", new { idProperty, formType });
        }

        // ── Owner: validate ID number against LIS ─────────────────────
        if (declarationType == "Owner")
        {
            var lisOwnerId = TempData.Peek("Attr_OwnerId")?.ToString();

            if (!string.IsNullOrWhiteSpace(lisOwnerId) &&
                !string.IsNullOrWhiteSpace(ownerIdNumber))
            {
                // Compare trimmed, case-insensitive
                if (!string.Equals(
                        lisOwnerId.Trim(),
                        ownerIdNumber.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["AttrCheckError"] =
                        "The ID number entered does not match our records " +
                        "for this property. Please verify and try again.";
                    TempData.Keep("Attr_Detail_Json");
                    TempData.Keep("Attr_OwnerId");
                    return RedirectToAction("Check", new { idProperty, formType });
                }
            }
        }

        TempData["AttrDeclaration"] = declarationType;
        TempData["AttrRepRequired"] =
            declarationType == "Representative" ? "true" : "false";
        TempData.Keep("Attr_Detail_Json");
        TempData.Keep("Attr_OwnerId");

        // Representative → separate page first
        if (declarationType == "Representative")
            return RedirectToAction("Representative", new { idProperty, formType });

        // Owner → straight to form
        return RedirectToAction("Form", new { idProperty, formType });
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

        var declaration = TempData["AttrDeclaration"]?.ToString();
        if (string.IsNullOrWhiteSpace(declaration))
            return RedirectToAction("Check", new { idProperty, formType });

        formType = formType switch
        {
            "Business" => "BusinessCommercial",
            "DRC" => "DRCMethod",
            "Residential-ST" => "ResidentialST",
            _ => formType
        };

        var model = _attributeService.CreateNew(formType);

        // Pre-fill from LIS + SAP data stored in TempData
        var json = TempData["Attr_Detail_Json"]?.ToString();
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var d = System.Text.Json.JsonSerializer
                    .Deserialize<V2_Genesis.Models.Attributes.LisPropertyDetail>(json);

                if (d is not null)
                {
                    model.PropertyDetails.PropertyId = d.PropertyId ?? idProperty;
                    model.PropertyDetails.UnitKey = d.UnitKey;
                    model.PropertyDetails.PremiseId = d.PremiseId;
                    model.PropertyDetails.ValuationKey = d.ValuationKey;
                    model.PropertyDetails.PropertyDesc = d.PropertyDesc;
                    model.PropertyDetails.Township = d.TownNameDesc;
                    model.PropertyDetails.Address = d.LisStreetAddress;
                    model.PropertyDetails.Erf = d.Erf.ToString();
                    model.PropertyDetails.SGNumber = d.SGNumber;
                    model.PropertyDetails.Zoning = d.Zoning;
                    model.PropertyDetails.RollType = d.RollType;
                    model.PropertyDetails.RollDescription = d.RollType;
                    model.PropertyDetails.Extent = d.RateableAreaVal ?? d.RateableArea;
                    model.PropertyDetails.Sector = d.TpsCode;
                    model.PropertyDetails.SectionalTitle = d.UnitType;
                    
                    // Pre-fill first contact from SAP data
                    if (model.ContactInfos.Any())
                    {
                        model.ContactInfos[0].FirstNames = d.OwnerFirstNames;   // pre-filled
                        model.ContactInfos[0].LastName = d.OwnerLastName;     // pre-filled
                        model.ContactInfos[0].IDNumber = d.OwnerId;           // auto-populated
                        model.ContactInfos[0].Email = d.Email;
                        model.ContactInfos[0].CellNo = d.CellNo;
                        model.ContactInfos[0].HomePhoneNo = d.TelNo;
                        model.ContactInfos[0].ContactType = "Owner";
                    }
                }
            }
            catch { /* open form blank on error */ }
        }
        else
        {
            model.PropertyDetails.PropertyId = idProperty;
            model.PropertyDetails.UnitKey = idProperty;
        }

        ViewBag.Declaration = declaration;
        ViewBag.RepRequired = TempData["AttrRepRequired"]?.ToString() == "true";

        return View("Create", model);
    }

  

    // ── Helper — same logic as DashboardService.ResolveFormType ───────
    private static string? ResolveFormType(string? catDesc)
    {
        if (string.IsNullOrWhiteSpace(catDesc)) return null;

        var cat = catDesc.Trim().ToLower();

        if (cat.Contains("sectional") ||
            cat.Contains("residential-st") ||
            cat.Contains("st ") || cat.Contains("unit"))
            return "ResidentialST";

        if (cat.Contains("business") ||
            cat.Contains("commercial") ||
            cat.Contains("industrial") ||
            cat.Contains("retail") ||
            cat.Contains("office"))
            return "BusinessCommercial";

        if (cat.Contains("drc") ||
            cat.Contains("public service") ||
            cat.Contains("institutional"))
            return "DRCMethod";

        return "Residential";
    }


    // ════════════════════════════════════════════════════════════════════
    //  UPDATE Form action — read declaration from TempData
    // ════════════════════════════════════════════════════════════════════

    

   
   
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


    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("attributes/representative")]
    public async Task<IActionResult> Representative(
    string idProperty, string formType = "Residential")
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        // Must have come from Check page
        var declaration = TempData.Peek("AttrDeclaration")?.ToString();
        if (declaration != "Representative")
            return RedirectToAction("Check", new { idProperty, formType });

        // Build summary from stored detail
        var vm = new RepresentativeViewModel
        {
            IDProperty = idProperty,
            FormType = formType,
        };

        var json = TempData.Peek("Attr_Detail_Json")?.ToString();
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var d = System.Text.Json.JsonSerializer
                    .Deserialize<LisPropertyDetail>(json);
                if (d is not null)
                {
                    vm.PropertyDesc = d.PropertyDesc;
                    vm.TownNameDesc = d.TownNameDesc;
                    vm.LisStreetAddress = d.LisStreetAddress;
                    vm.CatDesc = d.CatDesc;
                    // Pre-fill email from SAP if available
                    vm.Rep_Email = d.Email;
                    vm.Rep_Cell_Phone = d.CellNo;
                    vm.Rep_Home_Phone = d.TelNo;
                }
            }
            catch { }
        }

        return View(vm);
    }
}
