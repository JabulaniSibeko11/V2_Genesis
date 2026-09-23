using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Models.Notice;
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
    [Authorize]
    [Route("notice/appeal-outcome/download")]
    public async Task<IActionResult> DownloadAppealOutcome(
        string rollSource,
        string referenceNumber,
        string? returnUrl,
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
                IsAdministrativeUser(),
                cancellationToken);

            return File(generated.Pdf, "application/pdf", generated.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex,
                "Final appeal outcome not found. Roll={RollSource}, Reference={ReferenceNumber}",
                rollSource,
                referenceNumber);
            TempData["NoticeError"] =
                "The final appeal outcome could not be found.";
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["NoticeError"] =
                "This document belongs to a different account, so it cannot be downloaded here.";
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Final appeal outcome generation failed. Roll={RollSource}, Reference={ReferenceNumber}",
                rollSource,
                referenceNumber);
            TempData["NoticeError"] =
                WithDetail("The final appeal outcome could not be generated. Please try again.", ex);
            return RedirectToDashboard(returnUrl, rollSource);
        }
    }

    [HttpGet]
    [Authorize]
    [Route("notice/invalid-outcome/download")]
    public async Task<IActionResult> DownloadInvalidOutcome(
        string rollSource,
        string objectionNo,
        string? returnUrl,
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
                IsAdministrativeUser(),
                cancellationToken);

            return File(generated.Pdf, "application/pdf", generated.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex,
                "Invalid outcome notice not found. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            TempData["NoticeError"] =
                "The objection outcome notice could not be found.";
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["NoticeError"] =
                "This document belongs to a different account, so it cannot be downloaded here.";
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Invalid outcome generation failed. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            TempData["NoticeError"] =
                WithDetail("The objection outcome notice could not be generated. Please try again.", ex);
            return RedirectToDashboard(returnUrl, rollSource);
        }
    }

    [HttpGet]
    [Authorize]
    [Route("notice/objection-outcome/download")]
    public async Task<IActionResult> DownloadPreviousProcessOutcome(
        string rollSource,
        string objectionNo,
        string? returnUrl,
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
                IsAdministrativeUser(),
                cancellationToken);

            return File(generated.Pdf, "application/pdf", generated.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex,
                "Objection outcome notice not found. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            TempData["NoticeError"] =
                "The objection outcome notice could not be found.";
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["NoticeError"] =
                "This document belongs to a different account, so it cannot be downloaded here.";
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Objection outcome generation failed. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            TempData["NoticeError"] =
                WithDetail("The objection outcome notice could not be generated. Please try again.", ex);
            return RedirectToDashboard(returnUrl, rollSource);
        }
    }

    [HttpGet]
    [Authorize]
    [Route("notice/section53/download")]
    public async Task<IActionResult> DownloadSection53(
        string rollSource,
        string objectionNo,
        string? returnUrl,
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
                IsAdministrativeUser(),
                cancellationToken);

            return File(generated.Pdf, "application/pdf", generated.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex,
                "Section 53 notice not found. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            TempData["NoticeError"] =
                "The Section 53 notice could not be found.";
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["NoticeError"] =
                "This document belongs to a different account, so it cannot be downloaded here.";
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Section 53 generation failed. Roll={RollSource}, Objection={ObjectionNo}",
                rollSource,
                objectionNo);
            TempData["NoticeError"] = ex.Message;
            return RedirectToDashboard(returnUrl, rollSource);
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
    string? returnUrl,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectionNo) || objectionNo.Trim() == "—")
        {
            TempData["NoticeError"] =
                "The reference number is missing, so the acknowledgement cannot be generated. " +
                "You can download it from your dashboard.";
            return RedirectToDashboard(returnUrl, rollSource);
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
                        IsAdministrativeUser(),
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

            TempData["NoticeError"] =
                "The submitted application was not found.";
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["NoticeError"] =
                "This document belongs to a different account, so it cannot be downloaded here.";
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(
                ex,
                "Unsupported acknowledgement reference {ReferenceNumber}.",
                objectionNo);

            TempData["NoticeError"] = ex.Message;
            return RedirectToDashboard(returnUrl, rollSource);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not generate acknowledgement for {ReferenceNumber}.",
                objectionNo);

            TempData["NoticeError"] =
                WithDetail("The acknowledgement could not be generated. Please try again.", ex);

            return RedirectToDashboard(returnUrl, rollSource);
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
            {
                TempData["NoticeError"] = "No notice was selected.";
                return RedirectToDashboard(null, null);
            }

            // Decode
            var filePath = System.Uri.UnescapeDataString(path);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["NoticeError"] = "The notice file could not be found on the server.";
                return RedirectToDashboard(null, null);
            }

            // Security: file must be within one of the configured roots
            // (prevents path traversal)
            var safePaths = NoticeStoragePaths.AllRoots(
                HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>());

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
                TempData["NoticeError"] =
                    "This notice is stored outside the configured notice folders, so it cannot be downloaded.";
                return RedirectToDashboard(null, null);
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
            TempData["NoticeError"] =
                WithDetail("The notice could not be downloaded. Please try again.", ex);
            return RedirectToDashboard(null, null);
        }
    }

    // Downloads a notice only after resolving it from the signed-in
    // client's own notice list. This is the endpoint used by the client
    // dashboard and My Notices page; the browser never needs a server path.
    [HttpGet]
    [Authorize]
    [Route("notices/download-available")]
    public async Task<IActionResult> DownloadAvailable(
        string referenceNo,
        NoticeType type,
        string? rollSource,
        string? returnUrl,
        string? ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(referenceNo))
            return BadRequest("The reference number is required.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            var noticeOwnerUserId = IsAdministrativeUser() &&
                !string.IsNullOrWhiteSpace(ownerUserId)
                    ? ownerUserId.Trim()
                    : userId;

            var displayName = User.FindFirstValue(ClaimTypes.Name) ?? "Client";
            var notices = await _notice.GetNoticesDashboardAsync(
                noticeOwnerUserId,
                displayName);

            var item = notices.ObjectionNotices
                .Concat(notices.AppealNotices)
                .Concat(notices.QueryNotices)
                .FirstOrDefault(x =>
                    x.Type == type &&
                    string.Equals(
                        x.ReferenceNo?.Trim(),
                        referenceNo.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (item is null || !item.FileExists || string.IsNullOrWhiteSpace(item.FilePath))
            {
                TempData["NoticeError"] =
                    "The notice is not available for your account yet.";
                return RedirectToDashboard(returnUrl, rollSource);
            }

            return Download(Uri.EscapeDataString(item.FilePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Available notice download failed. Reference={ReferenceNo}, Type={NoticeType}",
                referenceNo,
                type);

            TempData["NoticeError"] =
                WithDetail("The notice could not be downloaded. Please try again.", ex);
            return RedirectToDashboard(returnUrl, rollSource);
        }
    }

    // Local path + query of the page that sent the request, or null.
    private string? RefererPath()
    {
        var referer = Request.Headers.Referer.ToString();
        if (string.IsNullOrWhiteSpace(referer) ||
            !Uri.TryCreate(referer, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var local = uri.PathAndQuery;

        // Never bounce back into a download URL (would loop).
        if (local.Contains("download", StringComparison.OrdinalIgnoreCase))
            return null;

        return Url.IsLocalUrl(local) ? local : null;
    }

    // Admins (and Development) see the real reason a document failed,
    // clients see the friendly message only.
    private string WithDetail(string message, Exception ex)
    {
        var env = HttpContext.RequestServices
            .GetService<IWebHostEnvironment>();

        return IsAdministrativeUser() || env?.IsDevelopment() == true
            ? $"{message} ({ex.Message})"
            : message;
    }

    private bool IsAdministrativeUser() =>
        User.IsInRole("Admin") ||
        User.FindFirstValue("UMRole")?.Equals(
            "Admin",
            StringComparison.OrdinalIgnoreCase) == true ||
        !string.IsNullOrWhiteSpace(User.FindFirstValue("SAPNumber"));

    private IActionResult RedirectToDashboard(
        string? returnUrl,
        string? rollSource)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        // No returnUrl: go back to the page the button was clicked on, so the
        // message is shown where the person was (Display page, My Notices,
        // Section 49 page, ...), not on the dashboard landing page.
        var back = RefererPath();
        if (back is not null)
            return LocalRedirect(back);

        var openRoll = string.IsNullOrWhiteSpace(rollSource)
            ? "Objection"
            : rollSource.Trim();

        return IsAdministrativeUser()
            ? RedirectToAction("Index", "Admin", new { openRoll })
            : RedirectToAction("Index", "Dashboard", new { openRoll });
    }
}
