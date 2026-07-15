using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Services.Implementations;
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
    private readonly IEvidenceService _evidenceService;
    public AttributesController(
        IAttributesSearchService attrSearch,
        IPropertySearchService propSearch,
        ApplicationDbContext db, AttributesDbContext attributesDb,
        ILogger<AttributesController> logger, IAttributeSubmissionService attributeService,
        IEvidenceService evidenceSe)
    {
        _attrSearch = attrSearch;
        _propSearch = propSearch;
        _db = db;
        _attrDb = attributesDb;
        _logger = logger;
        _attributeService = attributeService;
        _evidenceService = evidenceSe;
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

        if (string.IsNullOrWhiteSpace(idProperty))
        {
            TempData["AttrLinkError"] = "Property reference was not supplied. Please search and link the property again.";
            return RedirectToAction("Search", "Attributes");
        }

        idProperty = idProperty.Trim();
        propertyFrom = string.IsNullOrWhiteSpace(propertyFrom)
            ? "Attributes"
            : propertyFrom.Trim();

        try
        {
            // Important: idProperty must be UnitKey because dashboard and check page use Attr_GetPropertyForCheck @UnitKey.
            var detail = await _attrSearch.GetPropertyDetailAsync(idProperty);

            if (detail == null)
            {
                TempData["AttrLinkError"] = "Could not load this property from the Attributes data source. Please search and link it again.";
                return RedirectToAction("Search", "Attributes");
            }

            var result = await _attrSearch.LinkPropertyAsync(idProperty, userId, propertyFrom);

            TempData[result.IsDuplicate ? "AttrLinkInfo" : "AttrLinkSuccess"] =
                result.IsDuplicate
                    ? "This property is already linked to your profile."
                    : "Property linked. You can now submit attributes from your dashboard.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Attributes] Link failed — UnitKey={UnitKey} User={UserId}", idProperty, userId);
            TempData["AttrLinkError"] = "Could not link this property. Please try again.";
        }

        return RedirectToAction("Index", "Dashboard", new { openRoll = "attributes" });
    }
    // AttributesController.cs — updated Check + CheckConfirm + Form actions

    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("attributes/check")]
    public async Task<IActionResult> Check(string idProperty, string formType)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        if (string.IsNullOrWhiteSpace(idProperty))
        {
            TempData["AttrLinkError"] = "Property reference was not supplied.";
            return RedirectToAction("Index", "Dashboard", new { openRoll = "attributes" });
        }

        if (string.IsNullOrWhiteSpace(formType))
        {
            return RedirectToAction("SelectForm", new { unitKey = idProperty });
        }

        idProperty = idProperty.Trim();
        formType = NormalizeAttributeFormType(formType);

        if (!IsValidAttributeFormType(formType))
        {
            TempData["AttrFormError"] = "Please select a valid attribute form.";
            return RedirectToAction("SelectForm", new { unitKey = idProperty });
        }

        var detail = await _attrSearch.GetPropertyDetailAsync(idProperty);

        if (detail == null)
        {
            TempData["AttrLinkError"] = "Could not load the linked property. Please search and link it again.";
            return RedirectToAction("Index", "Dashboard", new { openRoll = "attributes" });
        }

        // Important:
        // We no longer auto-select the form here.
        // The client-selected formType is now the source of truth.
        TempData["Attr_Detail_Json"] =
            System.Text.Json.JsonSerializer.Serialize(detail);

        TempData.Keep("Attr_Detail_Json");

        var vm = new CheckAttributesViewModel
        {
            IDProperty = idProperty,
            FormType = formType,

            PropertyDesc = BuildDisplayPropertyDescription(detail),
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
        };

        return View(vm);
    }
    
    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("attributes/select-form")]
    public async Task<IActionResult> SelectForm(string unitKey)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        if (string.IsNullOrWhiteSpace(unitKey))
        {
            TempData["AttrLinkError"] = "Property reference is missing.";
            return RedirectToAction("Index", "Dashboard", new { openRoll = "attributes" });
        }

        unitKey = unitKey.Trim();

        var property = await _attrSearch.GetPropertyDetailAsync(unitKey);

        if (property == null)
        {
            TempData["AttrLinkError"] = "Could not load the linked property. Please search and link it again.";
            return RedirectToAction("Index", "Dashboard", new { openRoll = "attributes" });
        }

        var vm = new AttributeFormSelectionViewModel
        {
            UnitKey = unitKey,
            PropertyDescription = BuildDisplayPropertyDescription(property),
            Category = property.CatDesc,
            Town = property.TownNameDesc,
            MarketValue = property.MarketValue?.ToString(),
            SuggestedFormType = ResolveSuggestedAttributeFormType(
                property.CatDesc,
                property.SchemeName,
                property.UnitNo.ToString() ?? "0")
        };

        return View("SelectForm", vm);
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    [Route("attributes/select-form")]
    public IActionResult SelectForm(AttributeFormSelectionViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.UnitKey))
        {
            TempData["AttrLinkError"] = "Property reference is missing.";
            return RedirectToAction("Index", "Dashboard", new { openRoll = "attributes" });
        }

        if (string.IsNullOrWhiteSpace(vm.SelectedFormType))
        {
            TempData["AttrFormError"] = "Please select the form you want to complete.";
            return RedirectToAction("SelectForm", new { unitKey = vm.UnitKey });
        }

        var selectedFormType = NormalizeAttributeFormType(vm.SelectedFormType);

        if (!IsValidAttributeFormType(selectedFormType))
        {
            TempData["AttrFormError"] = "Please select a valid attribute form.";
            return RedirectToAction("SelectForm", new { unitKey = vm.UnitKey });
        }

        return RedirectToAction("Check", "Attributes", new
        {
            idProperty = vm.UnitKey.Trim(),
            formType = selectedFormType
        });
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
        if (string.IsNullOrWhiteSpace(idProperty))
        {
            TempData["AttrLinkError"] = "Property reference was not supplied.";
            return RedirectToAction("Index", "Dashboard", new { openRoll = "attributes" });
        }

        formType = NormalizeAttributeFormType(formType);

        if (!IsValidAttributeFormType(formType))
        {
            TempData["AttrFormError"] = "Please select a valid attribute form.";
            return RedirectToAction("SelectForm", new { unitKey = idProperty });
        }

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

        if (declarationType == "Representative")
            return RedirectToAction("Representative", new { idProperty, formType });

        return RedirectToAction("Form", new { idProperty, formType });
    }
    private static string NormalizeAttributeFormType(string? formType)
    {
        if (string.IsNullOrWhiteSpace(formType))
            return "";

        return formType.Trim() switch
        {
            "Business" => "BusinessCommercial",
            "BusinessCommercial" => "BusinessCommercial",
            "NonResidential" => "BusinessCommercial",
            "Non-Residential" => "BusinessCommercial",
            "Non Res" => "BusinessCommercial",

            "DRC" => "DRCMethod",
            "DRCMethod" => "DRCMethod",
            "DRC Method" => "DRCMethod",

            "Residential-ST" => "ResidentialST",
            "Residential ST" => "ResidentialST",
            "ResidentialST" => "ResidentialST",

            "Residential" => "Residential",

            _ => formType.Trim()
        };
    }

    private static bool IsValidAttributeFormType(string? formType)
    {
        formType = NormalizeAttributeFormType(formType);

        return formType is
            "Residential" or
            "ResidentialST" or
            "DRCMethod" or
            "BusinessCommercial";
    }

    private static string ResolveSuggestedAttributeFormType(
        string? catDesc,
        string? schemeName,
        string? unitNo)
    {
        var cat = (catDesc ?? "").Trim().ToLower();

        if (!string.IsNullOrWhiteSpace(schemeName) ||
            (!string.IsNullOrWhiteSpace(unitNo) && unitNo != "0"))
        {
            return "ResidentialST";
        }

        if (cat.Contains("business") ||
            cat.Contains("commercial") ||
            cat.Contains("industrial") ||
            cat.Contains("retail") ||
            cat.Contains("office"))
        {
            return "BusinessCommercial";
        }

        if (cat.Contains("drc") ||
            cat.Contains("public service") ||
            cat.Contains("municipal") ||
            cat.Contains("religious") ||
            cat.Contains("mining") ||
            cat.Contains("agricultural") ||
            cat.Contains("vacant") ||
            cat.Contains("institutional"))
        {
            return "DRCMethod";
        }

        return "Residential";
    }

    private static string BuildDisplayPropertyDescription(
        V2_Genesis.Models.Attributes.LisPropertyDetail property)
    {
        if (!string.IsNullOrWhiteSpace(property.PropertyDesc))
            return property.PropertyDesc;

        var town = property.TownNameDesc ?? "";
        var scheme = property.SchemeName ?? "";
        var unitNo = property.UnitNo;
        var erf = property.Erf;
        var ptn = property.Ptn?.ToString() ?? "";
        var re = property.Re ?? "";

        if (!string.IsNullOrWhiteSpace(scheme) ||
            ( unitNo != 0))
        {
            var parts = new List<string>();

            if (unitNo != 0)
                parts.Add($"UNIT {unitNo}");

            if (!string.IsNullOrWhiteSpace(scheme))
                parts.Add(scheme);

            if (!string.IsNullOrWhiteSpace(town))
                parts.Add(town);

            return "Scheme " + string.Join(", ", parts);
        }

        if (!string.IsNullOrWhiteSpace(ptn) &&
            ptn != "0" &&
            !string.IsNullOrWhiteSpace(town))
        {
            if (!string.IsNullOrWhiteSpace(re) &&
                re.Equals("RE", StringComparison.OrdinalIgnoreCase))
            {
                return $"RE PORTION {ptn} {town}";
            }

            return $"PORTION {ptn} {town}";
        }

        if (erf != 0 &&
            !string.IsNullOrWhiteSpace(town))
        {
            return $"Full Title ERF {erf} {town}";
        }

        return town;
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
                    model.PropertyDetails.UnitKey = d.UnitKey.ToString();
                    model.PropertyDetails.PremiseId = d.PremiseId;
                    model.PropertyDetails.ValuationKey = d.ValuationKey.ToString();
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
        PrepareAttributeCreateSubmission(model);

        if (!model.Declaration.DeclarationAccepted)
        {
            ModelState.AddModelError(
                "Declaration.DeclarationAccepted",
                "You must accept the declaration before submitting.");
        }

        if (string.IsNullOrWhiteSpace(model.Declaration.SignatureName))
        {
            ModelState.AddModelError(
                "Declaration.SignatureName",
                "Signature name is required.");
        }

        if (!ModelState.IsValid)
        {
            return View("Create", model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? throw new InvalidOperationException("User not authenticated.");

        var userName = User.Identity?.Name ?? "Client";

        var attrId = await _attributeService.SubmitAsync(model, userId, userName);

        TempData["Success"] = "Attribute submission saved successfully.";

        return RedirectToAction("Details", new { id = attrId });
    }
    private static void PrepareAttributeCreateSubmission(AttributeSubmissionViewModel model)
    {
        model.FormType = NormalizeAttributeFormType(model.FormType);

        // Access is hidden on the online form.
        // Keep backend fields available, but do not force client input.
        model.Access ??= new AttributeAccessVm();
        model.Access.AccessType = null;
        model.Access.PermissionStatus = null;
        model.Access.Comments = null;

        model.ContactInfos ??= new List<AttributeContactInfoVm>();

        if (!model.ContactInfos.Any())
        {
            model.ContactInfos.Add(new AttributeContactInfoVm
            {
                ContactType = "Owner",
                IsCompany = false
            });
        }

        foreach (var contact in model.ContactInfos)
        {
            // These fields must not be captured on the online client form.
            contact.IDNumber = null;
            contact.DateOfBirth = null;
            contact.Gender = null;
            contact.MaritalStatus = null;
            contact.Citizenship = null;
            contact.FaxNo = null;
            contact.Interviewed = null;
            contact.MaidenName = null;

            if (contact.IsCompany)
            {
                contact.ContactType = "Company";

                // Hide owner person fields when Company is selected.
                contact.FirstNames = null;
                contact.LastName = null;
            }
            else
            {
                contact.ContactType = string.IsNullOrWhiteSpace(contact.ContactType)
                    ? "Owner"
                    : contact.ContactType;

                // Hide company fields when Owner is selected.
                contact.CompanyName = null;
                contact.CompanyRegistrationNumber = null;
            }
        }

        model.ValuationDetails ??= new AttributeValuationDetailsVm();

        if (!model.ValuationDetails.IsMixedUse)
        {
            model.ValuationDetails.AlternateUsages = null;
        }
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
    [HttpGet]
    [AllowAnonymous]
    [Route("attributes/evidence")]
    public async Task<IActionResult> Evidence(string? attrNo)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.AttrNo = attrNo; // pre-fill from dashboard link

        // Logged-in: load their submissions for dropdown
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var submissions = await _attrDb.AttrPropertyInfo
                .Where(p => p.SubmittedByUserId == userId && p.IsActive)
                .OrderByDescending(p => p.SubmissionDateTime)
                .Select(p => new { p.Attr_No, p.Property_Desc })
                .ToListAsync();

            ViewBag.Submissions = submissions;
        }

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("attributes/evidence")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EvidenceValidate(
    string attrNo, string pin)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.AttrNo = attrNo;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var submissions = await _attrDb.AttrPropertyInfo
                .Where(p => p.SubmittedByUserId == userId && p.IsActive)
                .OrderByDescending(p => p.SubmissionDateTime)
                .Select(p => new { p.Attr_No, p.Property_Desc })
                .ToListAsync();
            ViewBag.Submissions = submissions;
        }

        if (string.IsNullOrWhiteSpace(attrNo) || string.IsNullOrWhiteSpace(pin))
        {
            ViewBag.Error = "Please enter both the Attribute Number and PIN.";
            return View("Evidence");
        }

        var result = await _evidenceService.ValidateAttributeAsync(attrNo, pin);

        if (!result.IsValid)
        {
            ViewBag.Error = result.Error;
            ViewBag.Expired = result.IsExpired;
            return View("Evidence");
        }

        // Valid — store in TempData and show upload form
        TempData["AttrEv_AttrNo"] = result.AttrNo;
        TempData["AttrEv_PropertyDesc"] = result.PropertyDesc;
        TempData["AttrEv_Current"] = result.CurrentCount;
        TempData["AttrEv_Expiry"] = result.ExpiryDate?.ToString("o");
        TempData["AttrEv_Slots"] = result.SlotsRemaining;

        return RedirectToAction("EvidenceUpload");
    }

    // ── GET /attributes/evidence/upload ─────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    [Route("attributes/evidence/upload")]
    public async Task<IActionResult> EvidenceUpload()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        var attrNo = TempData.Peek("AttrEv_AttrNo")?.ToString();
        if (string.IsNullOrWhiteSpace(attrNo))
            return RedirectToAction("Evidence");

        ViewBag.AttrNo = attrNo;
        ViewBag.PropertyDesc = TempData.Peek("AttrEv_PropertyDesc")?.ToString();
        ViewBag.Current = TempData.Peek("AttrEv_Current") as int? ?? 0;
        ViewBag.Slots = TempData.Peek("AttrEv_Slots") as int? ?? 0;
        ViewBag.Expiry = TempData.Peek("AttrEv_Expiry")?.ToString();

        return View();
    }

    // ── POST /attributes/evidence/upload ────────────────────────────
    [HttpPost]
    [AllowAnonymous]
    [Route("attributes/evidence/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EvidenceUpload(List<IFormFile> evidenceFiles)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        var attrNo = TempData["AttrEv_AttrNo"]?.ToString();
        int current = TempData["AttrEv_Current"] as int? ?? 0;
        int slots = TempData["AttrEv_Slots"] as int? ?? 0;

        if (string.IsNullOrWhiteSpace(attrNo))
            return RedirectToAction("Evidence");

        if (!evidenceFiles.Any())
        {
            ViewBag.Error = "Please select at least one file to upload.";
            ViewBag.AttrNo = attrNo;
            ViewBag.PropertyDesc = TempData["AttrEv_PropertyDesc"]?.ToString();
            ViewBag.Current = current;
            ViewBag.Slots = slots;
            ViewBag.Expiry = TempData["AttrEv_Expiry"]?.ToString();
            TempData.Keep();
            return View();
        }

        if (evidenceFiles.Count > slots)
        {
            ViewBag.Error = $"You selected {evidenceFiles.Count} file(s) " +
                                   $"but only {slots} slot(s) remain. " +
                                   $"Please select fewer files.";
            ViewBag.AttrNo = attrNo;
            ViewBag.PropertyDesc = TempData["AttrEv_PropertyDesc"]?.ToString();
            ViewBag.Current = current;
            ViewBag.Slots = slots;
            ViewBag.Expiry = TempData["AttrEv_Expiry"]?.ToString();
            TempData.Keep();
            return View();
        }

        var (success, error, newCount, savedNames) =
            await _evidenceService.UploadAttributeEvidenceAsync(
                attrNo, current, evidenceFiles);

        if (!success)
        {
            ViewBag.Error = error;
            ViewBag.AttrNo = attrNo;
            ViewBag.PropertyDesc = TempData["AttrEv_PropertyDesc"]?.ToString();
            ViewBag.Current = current;
            ViewBag.Slots = slots;
            ViewBag.Expiry = TempData["AttrEv_Expiry"]?.ToString();
            TempData.Keep();
            return View();
        }
        // Pass acknowledgement data
        TempData["AttrEv_NewCount"] = newCount;
        TempData["AttrEv_Uploaded"] = savedNames.Count;
        TempData["AttrEv_FileNames"] = System.Text.Json.JsonSerializer.Serialize(savedNames);
        TempData["AttrEv_AttrNo"] = attrNo;
        TempData["AttrEv_PropDesc"] = TempData["AttrEv_PropertyDesc"]?.ToString();

        return RedirectToAction("EvidenceAcknowledgement");
    }
    [HttpGet]
    [AllowAnonymous]
    [Route("attributes/evidence/acknowledgement")]
    public async Task<IActionResult> EvidenceAcknowledgement()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        // Peek — reads without marking for deletion
        ViewBag.AttrNo = TempData.Peek("AttrEv_AttrNo")?.ToString();
        ViewBag.PropDesc = TempData.Peek("AttrEv_PropDesc")?.ToString();
        ViewBag.NewCount = Convert.ToInt32(TempData.Peek("AttrEv_NewCount") ?? 0);
        ViewBag.Uploaded = Convert.ToInt32(TempData.Peek("AttrEv_Uploaded") ?? 0);

        var json = TempData.Peek("AttrEv_FileNames")?.ToString();
        ViewBag.FileNames = string.IsNullOrWhiteSpace(json)
            ? new List<string>()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);

        if (string.IsNullOrWhiteSpace(ViewBag.AttrNo as string))
            return RedirectToAction("Evidence");

        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("attributes/acknowledgement/download")]
    public async Task<IActionResult> DownloadAcknowledgement(string attrNo)
    {
        if (string.IsNullOrWhiteSpace(attrNo))
            return BadRequest("Attribute number is required.");

        var files = await _attrDb.AttrFiles
            .FirstOrDefaultAsync(f => f.Attr_No == attrNo.Trim());

        if (files is null || string.IsNullOrWhiteSpace(files.RootFolder))
            return NotFound("Acknowledgement record not found.");

        // Acknowledgement_FileName is a DB computed column:
        // Attr_Ref_Files + '_Acknowledgement.pdf'
        var fileName = files.Acknowledgement_FileName
                       ?? $"{attrNo.Trim()}_Acknowledgement.pdf";

        var filePath = Path.Combine(files.RootFolder, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound("Acknowledgement PDF not found on server.");

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, "application/pdf", fileName);
    }
}
