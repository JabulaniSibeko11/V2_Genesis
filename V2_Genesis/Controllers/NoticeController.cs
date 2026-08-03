using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Notice;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Controllers;

[Authorize]
public class NoticeController : Controller
{
    private readonly INoticeService _notice;
    private readonly IObjectionFormService _objectionFormService;
    private readonly IPropertySearchService _search;
    private readonly ApplicationDbContext _db;
    private readonly RollDatesSettings _rollDates;
    private readonly IAcknowledgementDownloadService _acknowledgementDownloadService;
    private readonly ISection53NoticeService _section53NoticeService;
    private readonly IDearJohnnyNoticeService _dearJohnnyNoticeService;
    private readonly IInvalidNoticeService _invalidNoticeService;
    private readonly IAppealDecisionNoticeService _appealDecisionNoticeService;
    private readonly ILogger<NoticeController> _logger;
    public NoticeController(
        INoticeService notice,
        IObjectionFormService objectionFormService,
        IPropertySearchService search,
        ApplicationDbContext db, IOptions<RollDatesSettings> rollDatesOpts,
        IAcknowledgementDownloadService acknowledgementDownloadService,
        ISection53NoticeService section53NoticeService,
        IDearJohnnyNoticeService dearJohnnyNoticeService,
        IInvalidNoticeService invalidNoticeService,
        IAppealDecisionNoticeService appealDecisionNoticeService,
        ILogger<NoticeController> logger)
    {
        _notice = notice;
        _objectionFormService = objectionFormService;
        _search = search;
        _db = db;
        _rollDates = rollDatesOpts.Value;
        _acknowledgementDownloadService = acknowledgementDownloadService;
        _section53NoticeService = section53NoticeService;
        _dearJohnnyNoticeService = dearJohnnyNoticeService;
        _invalidNoticeService = invalidNoticeService;
        _appealDecisionNoticeService = appealDecisionNoticeService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("notice/appeal-outcome/download")]
    public async Task<IActionResult> DownloadAppealOutcome(
        string rollSource,
        string referenceNumber,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            var generated = await _appealDecisionNoticeService.GenerateAsync(
                rollSource,
                referenceNumber,
                userId,
                cancellationToken);

            return File(generated.Pdf, "application/pdf", generated.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex,
                "Final appeal outcome not found. Roll={RollSource}, Reference={ReferenceNumber}",
                rollSource,
                referenceNumber);
            return NotFound("The final appeal outcome could not be found for your account.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Final appeal outcome generation failed. Roll={RollSource}, Reference={ReferenceNumber}",
                rollSource,
                referenceNumber);
            TempData["NoticeError"] =
                "The final appeal outcome could not be generated. Please try again.";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("notice/invalid-outcome/download")]
    public async Task<IActionResult> DownloadInvalidOutcome(
        string rollSource,
        string objectionNo,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            var generated = await _invalidNoticeService.GenerateAsync(
                rollSource,
                objectionNo,
                userId,
                cancellationToken);

            return File(generated.Pdf, "application/pdf", generated.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex,
                "Invalid outcome notice not found. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            return NotFound("The objection outcome notice could not be found for your account.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Invalid outcome generation failed. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            TempData["NoticeError"] =
                "The objection outcome notice could not be generated. Please try again.";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("notice/objection-outcome/download")]
    public async Task<IActionResult> DownloadPreviousProcessOutcome(
        string rollSource,
        string objectionNo,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            var generated = await _dearJohnnyNoticeService.GenerateAsync(
                rollSource,
                objectionNo,
                userId,
                cancellationToken);

            return File(generated.Pdf, "application/pdf", generated.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex,
                "Objection outcome notice not found. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            return NotFound("The objection outcome notice could not be found for your account.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Objection outcome generation failed. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            TempData["NoticeError"] =
                "The objection outcome notice could not be generated. Please try again.";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpGet]
    [Authorize(Roles = "Client")]
    [Route("notice/section53/download")]
    public async Task<IActionResult> DownloadSection53(
        string rollSource,
        string objectionNo,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            var generated = await _section53NoticeService.GenerateAsync(
                rollSource,
                objectionNo,
                userId,
                cancellationToken);

            return File(generated.Pdf, "application/pdf", generated.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex,
                "Section 53 notice not found. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            return NotFound("The Section 53 notice could not be found for your account.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Section 53 generation failed. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            TempData["NoticeError"] = ex.Message;
            return RedirectToAction("Index", "Dashboard");
        }
    }

    // ── GET /notice/section49 — display view ─────────────────────────
    [HttpGet]
    [Route("notice/section49")]
    public async Task<IActionResult> Section49Display(
        string rollSource,
        string unitKey,
        string valuationKey)
    {
        var roll = await _db.GvList
            .FirstOrDefaultAsync(r => r.Source == rollSource);

        if (roll is null) return NotFound();

        var items = await _search.GetPropertyDetailsAsync(
            rollSource, unitKey, valuationKey);

        if (!items.Any()) return NotFound("Property not found.");

        // ── Pass dates to view ────────────────────────────────────────
        var dates = _rollDates.For(rollSource);             // ← NEW

        ViewData["RollSource"] = rollSource;
        ViewData["UnitKey"] = unitKey;
        ViewData["ValuationKey"] = valuationKey;
        ViewBag.Roll = roll;
        ViewBag.Dates = dates;            // ← NEW
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        return View(items);
    }

    // ── GET /notice/section49/download — PDF download ────────────────
    [HttpGet]
    [Route("notice/section49/download")]
    public async Task<IActionResult> DownloadSection49(
        string rollSource,
        string unitKey,
        string valuationKey)
    {
        try
        {
            var (pdf, fileName) = await _notice.GenerateSection49Async(
                rollSource, unitKey, valuationKey);

            return File(pdf, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            TempData["NoticeError"] = ex.Message;
            return RedirectToAction(nameof(Section49Display),
                new { rollSource, unitKey, valuationKey });
        }
    }
    // ── GET /notice/acknowledgement/download ──────────────────────
    // Rebuilds the PDF from the submitted objection/appeal records.
    // No acknowledgement PDF is read from an evidence folder.
    [HttpGet]
    [Route("notice/acknowledgement/download")]
    public async Task<IActionResult> DownloadAcknowledgement(
    string objectionNo,
    string? rollSource,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectionNo))
        {
            return BadRequest(
                "The reference number is required.");
        }

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            var generated =
                await _acknowledgementDownloadService
                    .GenerateAsync(
                        objectionNo,
                        rollSource,
                        userId,
                        cancellationToken);

            if (generated.PdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "Acknowledgement generation returned an empty PDF.");
            }

            _logger.LogInformation(
                "Generated acknowledgement on demand for {ReferenceNumber}. Type={SubmissionType}",
                generated.ReferenceNumber,
                generated.SubmissionType);

            return File(
                generated.PdfBytes,
                "application/pdf",
                generated.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Acknowledgement data was not found for {ReferenceNumber}.",
                objectionNo);

            return NotFound(
                "The submitted application was not found.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(
                ex,
                "Unsupported acknowledgement reference {ReferenceNumber}.",
                objectionNo);

            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not generate acknowledgement for {ReferenceNumber}.",
                objectionNo);

            TempData["NoticeError"] =
                "The acknowledgement could not be generated. Please try again.";

            return RedirectToAction(
                "Index",
                "Dashboard");
        }
    }

    // GET /notices
    [HttpGet]
    [Route("notices")]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? "Client";

        var vm = await _notice.GetNoticesDashboardAsync(userId, displayName);
        return View(vm);   // Views/Notices/Index.cshtml
    }

    // GET /notices/download?path={encodedPath}
    // Serves the notice file (PDF or EML) to the client
    [HttpGet]
    [Route("notices/download")]
    public IActionResult Download(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("No file path specified.");

            // Decode
            var filePath = System.Uri.UnescapeDataString(path);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Notice file not found.");

            // Security: file must be within one of the configured roots
            // (prevents path traversal)
            var safePaths = new[]
            {
                HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    ["ObjectionRolls:Objection:RootPath"]       ?? "",
                HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    ["ObjectionRolls:Objection_Supp1:RootPath"] ?? "",
                HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    ["ObjectionRolls:Objection_Supp2:RootPath"] ?? "",
                HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    ["ObjectionRolls:Objection_Supp3:RootPath"] ?? "",
                HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    ["AppSettings:Section49RootPath"]            ?? "",
                HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    ["AppSettings:AppealRootPath"]               ?? "",
                HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    ["ObjectionRolls:Objection_Query:QueryRootPath"] ?? "",
            };

            var normalised = Path.GetFullPath(filePath);
            bool allowed = safePaths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Any(root => normalised.StartsWith(
                    Path.GetFullPath(root),
                    StringComparison.OrdinalIgnoreCase));

            if (!allowed)
            {
                _logger.LogWarning(
                    "[Notices] Blocked download outside safe paths: {Path}", filePath);
                return Forbid();
            }

            var ext = Path.GetExtension(filePath).ToLower();
            var contentType = ext == ".eml"
                ? "message/rfc822"
                : "application/pdf";

            var fileName = Path.GetFileName(filePath);
            var bytes = System.IO.File.ReadAllBytes(filePath);

            return File(bytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Notices] Download failed for {Path}", path);
            return StatusCode(500, "Could not retrieve the file.");
        }
    }
}

