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
    private readonly AttributesDbContext _attrDb;
    private readonly ILogger<AttributesController> _logger;
    private readonly IAttributeSubmissionService _attributeService;
    public AttributesController(
        IAttributesSearchService attrSearch,
        IPropertySearchService propSearch,
        ApplicationDbContext db,AttributesDbContext attributesDb,
        ILogger<AttributesController> logger,IAttributeSubmissionService attributeService)
    {
        _attrSearch = attrSearch;
        _propSearch = propSearch;
        _db = db;
        _attrDb = attributesDb;
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
        ViewBag.Townships = await _attrSearch.GetTownshipsAsync();
        ViewBag.Schemes = await _attrSearch.GetSchemesAsync();
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
        ViewBag.Townships = await _attrSearch.GetTownshipsAsync();
        ViewBag.Schemes = await _attrSearch.GetSchemesAsync();
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
     string declarationType)
    {
        if (string.IsNullOrWhiteSpace(declarationType))
        {
            TempData["AttrCheckError"] =
                "Please select whether you are the Owner or Representative.";
            return RedirectToAction("Check", new { idProperty, formType });
        }

        TempData["AttrDeclaration"] = declarationType;
        TempData["AttrRepRequired"] =
            declarationType == "Representative" ? "true" : "false";
        TempData.Keep("Attr_Detail_Json");
        TempData.Keep("Attr_OwnerId");

        // Representative → separate rep details page
        if (declarationType == "Representative")
            return RedirectToAction("Representative", new { idProperty, formType });

        // Owner → straight to form (no ID validation)
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

        // Pre-fill property details from LIS data
        var detailJson = TempData["Attr_Detail_Json"]?.ToString();
        if (!string.IsNullOrWhiteSpace(detailJson))
        {
            try
            {
                var d = System.Text.Json.JsonSerializer
                    .Deserialize<V2_Genesis.Models.Attributes.LisPropertyDetail>(detailJson);
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
                    model.PropertyDetails.SectionalTitle = d.UnitType;

                    // Pre-fill contact 0 with owner data
                    if (model.ContactInfos.Any())
                    {
                        model.ContactInfos[0].FirstNames = d.OwnerFirstNames;
                        model.ContactInfos[0].LastName = d.OwnerLastName;
                        model.ContactInfos[0].IDNumber = d.OwnerId;
                        model.ContactInfos[0].Email = d.Email;
                        model.ContactInfos[0].CellNo = d.CellNo;
                        model.ContactInfos[0].HomePhoneNo = d.TelNo;
                        model.ContactInfos[0].ContactType =
                            declaration == "Representative" ? "Representative" : "Owner";
                    }
                }
            }
            catch { /* open form blank */ }
        }
        else
        {
            model.PropertyDetails.PropertyId = idProperty;
            model.PropertyDetails.UnitKey = idProperty;
        }

        // Pre-fill representative details if they exist in TempData
        var repJson = TempData["AttrRepDetails"]?.ToString();
        if (!string.IsNullOrWhiteSpace(repJson))
        {
            try
            {
                var rep = System.Text.Json.JsonSerializer
                    .Deserialize<System.Text.Json.JsonElement>(repJson);

                model.RepresentativeDetails = new RepresentativeDetailsVm
                {
                    IsRepresentative = true,
                    Representative_Name = rep.GetProperty("Representative_Name").GetString(),
                    Rep_Postal_1 = rep.GetProperty("Rep_Postal_1").GetString(),
                    Rep_Postal_2 = rep.GetProperty("Rep_Postal_2").GetString(),
                    Rep_Postal_3 = rep.GetProperty("Rep_Postal_3").GetString(),
                    Rep_Postal_4 = rep.GetProperty("Rep_Postal_4").GetString(),
                    Rep_Postal_5 = rep.GetProperty("Rep_Postal_5").GetString(),
                    Rep_Home_Phone = rep.GetProperty("Rep_Home_Phone").GetString(),
                    Rep_Cell_Phone = rep.GetProperty("Rep_Cell_Phone").GetString(),
                    Rep_Work_Phone = rep.GetProperty("Rep_Work_Phone").GetString(),
                    Rep_Fax_Phone = rep.GetProperty("Rep_Fax_Phone").GetString(),
                    Rep_Email = rep.GetProperty("Rep_Email").GetString(),
                };
            }
            catch { /* rep section stays empty */ }
        }

        ViewBag.Declaration = declaration;
        ViewBag.RepRequired = declaration == "Representative";

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
        if (!model.Declaration.DeclarationAccepted)
        {
            ModelState.AddModelError("Declaration.DeclarationAccepted", "You must accept the declaration before submitting.");
        }

        if (string.IsNullOrWhiteSpace(model.Declaration.SignatureName))
        {
            ModelState.AddModelError("Declaration.SignatureName", "Signature name is required.");
        }

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
        var model = await _attributeService.GetAcknowledgementAsync(id);

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAcknowledgement(long id)
    {
        var model = await _attributeService.GetAcknowledgementAsync(id);

        if (model == null || string.IsNullOrWhiteSpace(model.AcknowledgementPath))
            return NotFound();

        if (!System.IO.File.Exists(model.AcknowledgementPath))
            return NotFound();

        var fileName = model.AcknowledgementFileName ?? $"{model.AttrNo}_Acknowledgement.pdf";

        var bytes = await System.IO.File.ReadAllBytesAsync(model.AcknowledgementPath);

        return File(bytes, "application/pdf", fileName);
    }


    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("attributes/representative")]
    public async Task<IActionResult> Representative(
    string idProperty, string formType = "Residential")
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        // Guard: must have come through Check page
        var declaration = TempData.Peek("AttrDeclaration")?.ToString();
        if (string.IsNullOrWhiteSpace(declaration) || declaration != "Representative")
        {
            TempData["AttrCheckError"] = "Please complete the check step first.";
            return RedirectToAction("Check", new { idProperty, formType });
        }

        var vm = new RepresentativeViewModel
        {
            IDProperty = idProperty,
            FormType = formType
        };

        // Pre-fill from LIS SAP data stored in TempData
        var json = TempData.Peek("Attr_Detail_Json")?.ToString();
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var d = System.Text.Json.JsonSerializer
                    .Deserialize<V2_Genesis.Models.Attributes.LisPropertyDetail>(json);
                if (d is not null)
                {
                    vm.PropertyDesc = d.PropertyDesc;
                    vm.TownNameDesc = d.TownNameDesc;
                    vm.LisStreetAddress = d.LisStreetAddress;
                    vm.CatDesc = d.CatDesc;
                    vm.Rep_Email = d.Email;
                    vm.Rep_Cell_Phone = d.CellNo;
                    vm.Rep_Home_Phone = d.TelNo;
                }
            }
            catch { /* use empty vm */ }
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [Route("attributes/representative")]
    [ValidateAntiForgeryToken]
    public IActionResult RepresentativeSubmit(RepresentativeViewModel vm)
    {
        // Re-validate required fields manually
        if (string.IsNullOrWhiteSpace(vm.Representative_Name))
        {
            ModelState.AddModelError("Representative_Name",
                "Representative name is required.");
            ViewBag.GvList = _db.GvList.OrderBy(r => r.ID).ToList();
            return View("Representative", vm);
        }

        // Store rep details as JSON in TempData
        // They travel through to Form, then into SubmitAsync
        var repData = new
        {
            vm.Representative_Name,
            vm.Rep_Postal_1,
            vm.Rep_Postal_2,
            vm.Rep_Postal_3,
            vm.Rep_Postal_4,
            vm.Rep_Postal_5,
            vm.Rep_Home_Phone,
            vm.Rep_Cell_Phone,
            vm.Rep_Work_Phone,
            vm.Rep_Fax_Phone,
            vm.Rep_Email
        };

        TempData["AttrRepDetails"] = System.Text.Json.JsonSerializer.Serialize(repData);
        TempData.Keep("AttrDeclaration");
        TempData.Keep("Attr_Detail_Json");

        return RedirectToAction("Form",
            new { idProperty = vm.IDProperty, formType = vm.FormType });
    }

    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("attributes/unlink")]
    public async Task<IActionResult> Unlink(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Load the linked record — only the owner can unlink
        var linked = await _attrDb.LinkedProperties
            .FirstOrDefaultAsync(p => p.IDProperty == id && p.UserID == userId);

        if (linked is null)
        {
            TempData["AttrLinkError"] = "Property not found or already removed.";
            return RedirectToAction("Index", "Dashboard");
        }

        // Optionally load property details for the confirmation page
        var detail = await _attrSearch.GetPropertyDetailAsync(linked.IDProperty);

        var vm = new UnlinkViewModel
        {
            Id = linked.ID,
            IDProperty = linked.IDProperty,
            PropertyDesc = detail?.PropertyDesc ?? linked.IDProperty,
            TownNameDesc = detail?.TownNameDesc,
            CatDesc = detail?.CatDesc,
            MarketValue = detail?.MarketValue,
            Address = detail?.LisStreetAddress
        };

        return View(vm);
    }

    // ── POST /attributes/unlink — permanent delete ────────────────────
    [HttpPost]
    [Authorize(Roles = "Client")]
    [Route("attributes/unlink")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkConfirm(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var linked = await _attrDb.LinkedProperties
            .FirstOrDefaultAsync(p => p.ID == id && p.UserID == userId);

        if (linked is null)
        {
            TempData["AttrLinkError"] = "Property not found or already removed.";
            return RedirectToAction("Index", "Dashboard");
        }

        _attrDb.LinkedProperties.Remove(linked);
        await _attrDb.SaveChangesAsync();

        TempData["AttrUnlinkSuccess"] =
            "Property has been removed from your profile. " +
            "You can search and link it again at any time.";

        return RedirectToAction("Index", "Dashboard");
    }

}
