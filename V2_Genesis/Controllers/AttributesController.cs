using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<string, byte>
        ActiveAttributeSubmissions = new(StringComparer.OrdinalIgnoreCase);

    private readonly IAttributesSearchService _attrSearch;
    private readonly IPropertySearchService _propSearch;
    private readonly ApplicationDbContext _db;
    private readonly AttributesDbContext _attrDb;
    private readonly ILogger<AttributesController> _logger;
    private readonly IAttributeSubmissionService _attributeService;
    private readonly IEvidenceService _evidenceService;
    private readonly IEmailService _emailService;
    public AttributesController(
        IAttributesSearchService attrSearch,
        IPropertySearchService propSearch,
        ApplicationDbContext db, AttributesDbContext attributesDb,
        ILogger<AttributesController> logger, IAttributeSubmissionService attributeService,
        IEvidenceService evidenceSe, IEmailService emailService)
    {
        _attrSearch = attrSearch;
        _propSearch = propSearch;
        _db = db;
        _attrDb = attributesDb;
        _logger = logger;
        _attributeService = attributeService;
        _evidenceService = evidenceSe;
        _emailService = emailService;
    }

    [HttpGet("attributes/correct/{attrId:long}")]
    public async Task<IActionResult> CorrectReturned(
        long attrId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();

        var model = await _attributeService.GetReturnedCorrectionAsync(
            attrId,
            userId,
            cancellationToken);

        if (model is null)
        {
            TempData["AttributeError"] =
                "This submission is not available for correction or does not belong to your account.";
            return RedirectToAction("Index", "Dashboard", new { openRoll = "attributes" });
        }

        return View("CorrectReturned", model);
    }

    [HttpPost("attributes/correct/{attrId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CorrectReturned(
        long attrId,
        ReturnedAttributeCorrectionViewModel model,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();

        model.AttrId = attrId;

        if (!ModelState.IsValid)
        {
            var current = await _attributeService.GetReturnedCorrectionAsync(
                attrId,
                userId,
                cancellationToken);

            if (current is null) return NotFound();

            current.Submission = model.Submission;
            current.RevisionComment = model.RevisionComment;
            return View("CorrectReturned", current);
        }

        try
        {
            await _attributeService.ResubmitReturnedCorrectionAsync(
                model,
                userId,
                User.Identity?.Name ?? userId,
                cancellationToken);

            TempData["AttributeSuccess"] =
                "Your corrected attribute submission was resubmitted to the valuer successfully.";

            return RedirectToAction("Index", "Dashboard", new { openRoll = "attributes" });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var current = await _attributeService.GetReturnedCorrectionAsync(
                attrId,
                userId,
                cancellationToken);

            if (current is null) return NotFound();
            current.Submission = model.Submission;
            current.RevisionComment = model.RevisionComment;
            return View("CorrectReturned", current);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to resubmit returned attribute {AttrId} for user {UserId}.",
                attrId,
                userId);

            ModelState.AddModelError(
                string.Empty,
                "The corrections could not be saved. Please try again or contact Valuation Services.");

            var current = await _attributeService.GetReturnedCorrectionAsync(
                attrId,
                userId,
                cancellationToken);

            if (current is null) return NotFound();
            current.Submission = model.Submission;
            current.RevisionComment = model.RevisionComment;
            return View("CorrectReturned", current);
        }
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

    // ════════════════════════════════════════════════════════════
    // ATTRIBUTE FORM SELECTION
    //
    // Dashboard -> SelectForm.cshtml -> Representative/Form
    //
    // The old /attributes/check route remains available, but it now
    // renders the SelectForm view instead of the old Check view.
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("attributes/check")]
    public Task<IActionResult> Check(
        string? idProperty,
        string? formType = null)
    {
        return LoadSelectFormViewAsync(
            idProperty,
            formType);
    }

    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("attributes/select-form")]
    public Task<IActionResult> SelectForm(
        string? unitKey,
        string? idProperty,
        string? formType = null)
    {
        var propertyReference =
            !string.IsNullOrWhiteSpace(idProperty)
                ? idProperty
                : unitKey;

        return LoadSelectFormViewAsync(
            propertyReference,
            formType);
    }

    private async Task<IActionResult> LoadSelectFormViewAsync(
        string? propertyReference,
        string? requestedFormType)
    {
        ViewBag.GvList = await _db.GvList
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(propertyReference))
        {
            TempData["AttrLinkError"] =
                "Property reference was not supplied.";

            return RedirectToAction(
                "Index",
                "Dashboard",
                new { openRoll = "attributes" });
        }

        propertyReference = propertyReference.Trim();

        var property =
            await _attrSearch.GetPropertyDetailAsync(
                propertyReference);

        if (property is null)
        {
            TempData["AttrLinkError"] =
                "Could not load the linked property. " +
                "Please search and link it again.";

            return RedirectToAction(
                "Index",
                "Dashboard",
                new { openRoll = "attributes" });
        }

        TempData["Attr_Detail_Json"] =
            System.Text.Json.JsonSerializer.Serialize(property);

        TempData.Keep("Attr_Detail_Json");

        var selectedFormType =
            NormalizeAttributeFormType(
                requestedFormType);

        var model = new AttributeSelectViewModel
        {
            UnitKey = propertyReference,
            PropertyDesc =
                BuildDisplayPropertyDescription(property),
            CatDesc = property.CatDesc,
            TownNameDesc = property.TownNameDesc,
            LisStreetAddress = property.LisStreetAddress,
            MarketValue = property.MarketValue,
            RateableArea =
                property.RateableAreaVal
                ?? property.RateableArea,
            Erf = property.Erf,
            Ptn = property.Ptn,
            Re = property.Re,
            SchemeName = property.SchemeName,
            SchemeNumber = property.SchemeNumber,
            SchemeYear = property.SchemeYear,
            UnitNo = property.UnitNo,
            OwnerName = property.OwnerName,
            ValuationDate = property.ValuationDate,
            Zoning = property.Zoning,
            Reason = property.Reason,

            SuggestedFormType =
                ResolveSuggestedAttributeFormType(
                    property.CatDesc,
                    property.SchemeName,
                    property.UnitNo.ToString()),

            SelectedFormType =
                IsValidAttributeFormType(selectedFormType)
                    ? selectedFormType
                    : string.Empty
        };

        return View("SelectForm", model);
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [Route("attributes/select-form")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectForm(
        AttributeSelectViewModel model)
    {
        ViewBag.GvList = await _db.GvList
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(model.UnitKey))
        {
            TempData["AttrLinkError"] =
                "Property reference was not supplied.";

            return RedirectToAction(
                "Index",
                "Dashboard",
                new { openRoll = "attributes" });
        }

        model.UnitKey = model.UnitKey.Trim();

        model.SelectedFormType =
            NormalizeAttributeFormType(
                model.SelectedFormType);

        if (!IsValidAttributeFormType(
                model.SelectedFormType))
        {
            ModelState.AddModelError(
                nameof(model.SelectedFormType),
                "Please select a valid attribute form.");
        }

        if (string.IsNullOrWhiteSpace(
                model.DeclarationType))
        {
            ModelState.AddModelError(
                nameof(model.DeclarationType),
                "Please select whether you are the Owner " +
                "or Representative.");
        }
        else
        {
            model.DeclarationType =
                model.DeclarationType.Trim();

            if (model.DeclarationType is not
                    ("Owner" or "Representative"))
            {
                ModelState.AddModelError(
                    nameof(model.DeclarationType),
                    "Please select a valid submitter type.");
            }
        }

        var detail =
            await _attrSearch.GetPropertyDetailAsync(
                model.UnitKey);

        if (detail is null)
        {
            TempData["AttrLinkError"] =
                "Could not load the linked property. " +
                "Please search and link it again.";

            return RedirectToAction(
                "Index",
                "Dashboard",
                new { openRoll = "attributes" });
        }

        PopulateAttributeSelectModel(
            model,
            detail);

        if (!ModelState.IsValid)
        {
            return View("SelectForm", model);
        }

        TempData["Attr_Detail_Json"] =
            System.Text.Json.JsonSerializer.Serialize(detail);

        TempData["AttrDeclaration"] =
            model.DeclarationType;

        TempData["AttrRepRequired"] =
            model.DeclarationType == "Representative"
                ? "true"
                : "false";

        TempData.Keep("Attr_Detail_Json");
        TempData.Keep("AttrDeclaration");
        TempData.Keep("AttrRepRequired");

        if (model.DeclarationType == "Representative")
        {
            return RedirectToAction(
                nameof(Representative),
                new
                {
                    idProperty = model.UnitKey,
                    formType = model.SelectedFormType
                });
        }

        return RedirectToAction(
            nameof(Form),
            new
            {
                idProperty = model.UnitKey,
                formType = model.SelectedFormType
            });
    }

    // Compatibility for an old Check form POST.
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
            TempData["AttrLinkError"] =
                "Property reference was not supplied.";

            return RedirectToAction(
                "Index",
                "Dashboard",
                new { openRoll = "attributes" });
        }

        formType =
            NormalizeAttributeFormType(formType);

        if (!IsValidAttributeFormType(formType))
        {
            TempData["AttrFormError"] =
                "Please select a valid attribute form.";

            return RedirectToAction(
                nameof(SelectForm),
                new { unitKey = idProperty });
        }

        if (declarationType is not
                ("Owner" or "Representative"))
        {
            TempData["AttrCheckError"] =
                "Please select whether you are the " +
                "Owner or Representative.";

            return RedirectToAction(
                nameof(SelectForm),
                new
                {
                    unitKey = idProperty,
                    formType
                });
        }

        TempData["AttrDeclaration"] =
            declarationType;

        TempData["AttrRepRequired"] =
            declarationType == "Representative"
                ? "true"
                : "false";

        TempData.Keep("Attr_Detail_Json");
        TempData.Keep("AttrDeclaration");
        TempData.Keep("AttrRepRequired");

        if (declarationType == "Representative")
        {
            return RedirectToAction(
                nameof(Representative),
                new { idProperty, formType });
        }

        return RedirectToAction(
            nameof(Form),
            new { idProperty, formType });
    }

    private static void PopulateAttributeSelectModel(
        AttributeSelectViewModel model,
        LisPropertyDetail detail)
    {
        model.PropertyDesc =
            BuildDisplayPropertyDescription(detail);

        model.CatDesc = detail.CatDesc;
        model.TownNameDesc = detail.TownNameDesc;
        model.LisStreetAddress = detail.LisStreetAddress;
        model.MarketValue = detail.MarketValue;
        model.RateableArea =
            detail.RateableAreaVal
            ?? detail.RateableArea;
        model.Erf = detail.Erf;
        model.Ptn = detail.Ptn;
        model.Re = detail.Re;
        model.SchemeName = detail.SchemeName;
        model.SchemeNumber = detail.SchemeNumber;
        model.SchemeYear = detail.SchemeYear;
        model.UnitNo = detail.UnitNo;
        model.OwnerName = detail.OwnerName;
        model.ValuationDate = detail.ValuationDate;
        model.Zoning = detail.Zoning;
        model.Reason = detail.Reason;

        model.SuggestedFormType =
            ResolveSuggestedAttributeFormType(
                detail.CatDesc,
                detail.SchemeName,
                detail.UnitNo.ToString());
    }

    private static string NormalizeAttributeFormType(
        string? formType)
    {
        if (string.IsNullOrWhiteSpace(formType))
            return string.Empty;

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

    private static bool IsValidAttributeFormType(
        string? formType)
    {
        return NormalizeAttributeFormType(formType) is
            "Residential"
            or "ResidentialST"
            or "BusinessCommercial"
            or "DRCMethod";
    }

    private static string ResolveSuggestedAttributeFormType(
        string? catDesc,
        string? schemeName,
        string? unitNo)
    {
        var category =
            (catDesc ?? string.Empty)
            .Trim()
            .ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(schemeName)
            || (!string.IsNullOrWhiteSpace(unitNo)
                && unitNo != "0"))
        {
            return "ResidentialST";
        }

        if (category.Contains("business")
            || category.Contains("commercial")
            || category.Contains("industrial")
            || category.Contains("retail")
            || category.Contains("office"))
        {
            return "BusinessCommercial";
        }

        if (category.Contains("drc")
            || category.Contains("public service")
            || category.Contains("municipal")
            || category.Contains("religious")
            || category.Contains("mining")
            || category.Contains("agricultural")
            || category.Contains("vacant")
            || category.Contains("institutional"))
        {
            return "DRCMethod";
        }

        return "Residential";
    }

    private static string BuildDisplayPropertyDescription(
        LisPropertyDetail property)
    {
        if (!string.IsNullOrWhiteSpace(
                property.PropertyDesc))
        {
            return property.PropertyDesc;
        }

        var town =
            property.TownNameDesc
            ?? string.Empty;

        var scheme =
            property.SchemeName
            ?? string.Empty;

        var unitNo = property.UnitNo;
        var erf = property.Erf;

        var portion =
            property.Ptn?.ToString()
            ?? string.Empty;

        var remainder =
            property.Re
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(scheme)
            || unitNo != 0)
        {
            var parts = new List<string>();

            if (unitNo != 0)
                parts.Add($"UNIT {unitNo}");

            if (!string.IsNullOrWhiteSpace(scheme))
                parts.Add(scheme);

            if (!string.IsNullOrWhiteSpace(town))
                parts.Add(town);

            return "Scheme " +
                   string.Join(", ", parts);
        }

        if (!string.IsNullOrWhiteSpace(portion)
            && portion != "0"
            && !string.IsNullOrWhiteSpace(town))
        {
            if (remainder.Equals(
                    "RE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return $"RE PORTION {portion} {town}";
            }

            return $"PORTION {portion} {town}";
        }

        if (erf != 0
            && !string.IsNullOrWhiteSpace(town))
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
        if (model.Declaration is null)
        {
            ModelState.AddModelError(
                "Declaration",
                "Declaration details are required.");
        }
        else
        {
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
            else
            {
                model.Declaration.SignatureName =
                    model.Declaration.SignatureName.Trim();
            }
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Please correct the highlighted fields before submitting.";

            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var submissionKey = string.Join("|",
            userId,
            model.FormType?.Trim(),
            model.PropertyDetails?.ValuationKey?.Trim(),
            model.PropertyDetails?.UnitKey?.Trim(),
            model.PropertyDetails?.PropertyId?.Trim(),
            model.PropertyDetails?.PremiseId?.Trim());

        if (!ActiveAttributeSubmissions.TryAdd(submissionKey, 0))
        {
            ModelState.AddModelError(
                string.Empty,
                "This attribute submission is already being processed. Please wait.");

            return View(model);
        }

        var userName =
            User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name
            ?? "Client";

        var userEmail =
            User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("email");

        var userPhone =
            User.FindFirstValue(ClaimTypes.MobilePhone)
            ?? User.FindFirstValue(ClaimTypes.HomePhone)
            ?? User.FindFirstValue("phone_number");

        // Some identity configurations store the email in User.Identity.Name.
        if (string.IsNullOrWhiteSpace(userEmail) &&
            !string.IsNullOrWhiteSpace(User.Identity?.Name) &&
            User.Identity.Name.Contains('@'))
        {
            userEmail = User.Identity.Name.Trim();
        }

        try
        {
            var attrId = await _attributeService.SubmitAsync(
                model,
                userId,
                userName,
                userEmail,
                userPhone);

            if (attrId <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The attribute submission could not be completed.");

                return View(model);
            }

            TempData["Success"] =
                "Attribute submission saved successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = attrId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Attribute submission validation failed for user {UserId}.",
                userId);

            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error submitting attribute request for user {UserId}.",
                userId);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred while submitting the attribute request.");

            return View(model);
        }
        finally
        {
            ActiveAttributeSubmissions.TryRemove(submissionKey, out _);
        }
    }

    // Compatibility route for older dashboard links such as:
    // /attributes/submission/7
    //
    // The numeric database ID is resolved to the public Attr_No and
    // redirected to the shared submitted-form viewer.
    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("attributes/submission/{id:long}", Name = "AttributeSubmissionById")]
    public async Task<IActionResult> Submission(
        long id,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var submission = await _attrDb.AttrPropertyInfo
            .AsNoTracking()
            .Where(row =>
                row.Attr_ID == id
                && row.SubmittedByUserId == userId
                && row.IsActive)
            .Select(row => new
            {
                row.Attr_No
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
        {
            TempData["ErrorMessage"] =
                "The attribute submission could not be found, " +
                "or you do not have permission to view it.";

            return RedirectToAction(
                "Index",
                "Dashboard",
                new { openRoll = "attributes" });
        }

        if (string.IsNullOrWhiteSpace(submission.Attr_No))
        {
            TempData["ErrorMessage"] =
                "The attribute submission does not yet have a valid " +
                "attribute reference number.";

            return RedirectToAction(
                "Index",
                "Dashboard",
                new { openRoll = "attributes" });
        }

        var safeReturnUrl =
            !string.IsNullOrWhiteSpace(returnUrl)
            && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action(
                    "Index",
                    "Dashboard",
                    new { openRoll = "attributes" });

        return RedirectToRoute(
            "ViewSubmission",
            new
            {
                submissionType = "Attribute",
                referenceNumber = submission.Attr_No.Trim(),
                rollSource = "Attributes",
                returnUrl = safeReturnUrl
            });
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
        var generated = await _attributeService.GenerateAcknowledgementPdfAsync(id);

        if (generated is null || generated.Value.Pdf.Length == 0)
        {
            _logger.LogWarning(
                "[Attributes] On-demand acknowledgement generation returned nothing for Attr_ID={AttrId}",
                id);
            return NotFound("Could not generate the acknowledgement for this submission.");
        }

        return File(generated.Value.Pdf, "application/pdf", generated.Value.FileName);
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
    public async Task<IActionResult> Unlink(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) || id <= 0)
        {
            TempData["AttrLinkError"] = "The linked property could not be identified.";
            return RedirectToAction("Index", "Dashboard");
        }

        // The dashboard passes LinkedProperties_Attr.ID, not IDProperty.
        // Keep the UserID condition so one client cannot unlink another
        // client's property by changing the URL value.
        var linked = await _attrDb.LinkedProperties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ID == id && p.UserID == userId);

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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) || id <= 0)
        {
            TempData["AttrLinkError"] = "The linked property could not be identified.";
            return RedirectToAction("Index", "Dashboard");
        }

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
        var uploadedAt = DateTime.Now;
        var remainingSlots = Math.Max(0, 10 - newCount);

        try
        {
            await _emailService.SendAttributeEvidenceUploadConfirmationAsync(
                attrNo,
                savedNames,
                uploadedAt,
                remainingSlots);
        }
        catch (Exception ex)
        {
            // Evidence remains successfully uploaded even when SMTP is unavailable.
            _logger.LogError(ex,
                "[Attribute Evidence Email] Upload succeeded but confirmation email failed for {AttributeNo}",
                attrNo);
            TempData["AttrEv_EmailWarning"] =
                "Your evidence was uploaded successfully, but the confirmation email could not be sent.";
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
        ViewBag.EmailWarning = TempData.Peek("AttrEv_EmailWarning")?.ToString();

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

        var generated = await _attributeService.GenerateAcknowledgementPdfAsync(attrNo.Trim());

        if (generated is null || generated.Value.Pdf.Length == 0)
        {
            _logger.LogWarning(
                "[Attributes] On-demand acknowledgement generation returned nothing for Attr_No={AttrNo}",
                attrNo);
            return NotFound("Could not generate the acknowledgement for this submission.");
        }

        return File(generated.Value.Pdf, "application/pdf", generated.Value.FileName);
    }
}
