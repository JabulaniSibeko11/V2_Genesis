using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers;

// ── No [Authorize] — evidence is public-access ────────────────────────
public class EvidenceController : Controller
{
    private readonly IEvidenceService _evidenceService;
    private readonly INoticeService _noticeService;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EvidenceController> _logger;

    private const string SESSION_VALIDATED = "ev_validated";
    private const string SESSION_ROLL = "ev_roll";
    private const string SESSION_OBJ = "ev_objno";
    private const string SESSION_COUNT = "ev_count";
    private const string SESSION_IS_APPEAL = "ev_appeal";

    public EvidenceController(
        IEvidenceService evidenceService,
        INoticeService noticeService,
        IEmailService emailService,
        ApplicationDbContext db,
        ILogger<EvidenceController> logger)
    {
        _evidenceService = evidenceService;
        _noticeService = noticeService;
        _emailService = emailService;
        _db = db;
        _logger = logger;
    }

    // ── GET /evidence/verify ──────────────────────────────────────────
    // Replaces the old parameterless Verify() — DELETE the old one
    [HttpGet]
    [AllowAnonymous]
    [Route("evidence/VerifyObj")]
    [Route("evidence/verify")]
    public async Task<IActionResult> VerifyObj(
        string? objectionNo = null,
        string? rollSource = null)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        if (!string.IsNullOrWhiteSpace(objectionNo) &&
            string.IsNullOrWhiteSpace(rollSource))
        {
            rollSource = DetectRollSource(objectionNo);
        }

        ViewBag.PrefilledRef = objectionNo;
        ViewBag.PrefilledRoll = rollSource;
        ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated == true;
        ViewBag.IsAppeal = objectionNo?.Trim().ToUpper().StartsWith("APP") == true;

        return View();
    }

    // ── POST /evidence/verify ─────────────────────────────────────────
    [HttpPost]
    [AllowAnonymous]
    [Route("evidence/verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(string refNo, string pin)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        if (string.IsNullOrWhiteSpace(refNo) || string.IsNullOrWhiteSpace(pin))
        {
            ViewBag.Error = "Please enter both the reference number and PIN.";
            ViewBag.PrefilledRef = refNo;
            ViewBag.PrefilledRoll = null;
            return View(nameof(VerifyObj));
        }

        var rollSource = DetectRollSource(refNo);

        var result = await _evidenceService.ValidateAsync(
            rollSource, refNo.Trim(), pin.Trim());

        // Validate the reference, PIN, status and 48-hour evidence window.
        if (!result.IsValid)
        {
            ViewBag.Error = result.Error;
            ViewBag.PrefilledRef = refNo;
            ViewBag.PrefilledRoll = rollSource;
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated == true;
            ViewBag.IsAppeal = refNo.Trim().ToUpperInvariant().StartsWith("APP");
            return View(nameof(VerifyObj));
        }

        HttpContext.Session.SetString(SESSION_VALIDATED, "true");
        HttpContext.Session.SetString(SESSION_ROLL, rollSource);
        HttpContext.Session.SetString(SESSION_OBJ, refNo.Trim());
        HttpContext.Session.SetInt32(SESSION_COUNT, result.CurrentCount);
        HttpContext.Session.SetString(SESSION_IS_APPEAL, result.IsAppeal.ToString());

        return RedirectToAction(nameof(Upload));
    }

    // ── GET /evidence/upload ──────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    [Route("evidence/upload")]
    public async Task<IActionResult> Upload()
    {
        if (HttpContext.Session.GetString(SESSION_VALIDATED) != "true")
            return RedirectToAction(nameof(VerifyObj));

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.ObjectionNo = HttpContext.Session.GetString(SESSION_OBJ);
        ViewBag.RollSource = HttpContext.Session.GetString(SESSION_ROLL);
        ViewBag.CurrentCount = HttpContext.Session.GetInt32(SESSION_COUNT) ?? 0;
        ViewBag.Remaining = 10 - (HttpContext.Session.GetInt32(SESSION_COUNT) ?? 0);
        ViewBag.IsAppeal = bool.Parse(
            HttpContext.Session.GetString(SESSION_IS_APPEAL) ?? "false");

        return View();
    }

    // ── POST /evidence/upload ─────────────────────────────────────────
    [HttpPost]
    [AllowAnonymous]
    [Route("evidence/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        if (HttpContext.Session.GetString(SESSION_VALIDATED) != "true")
            return RedirectToAction(nameof(VerifyObj));

        var objNo = HttpContext.Session.GetString(SESSION_OBJ)!;
        var roll = HttpContext.Session.GetString(SESSION_ROLL)!;
        var count = HttpContext.Session.GetInt32(SESSION_COUNT) ?? 0;
        bool appeal = bool.Parse(
            HttpContext.Session.GetString(SESSION_IS_APPEAL) ?? "false");

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.ObjectionNo = objNo;
        ViewBag.RollSource = roll;
        ViewBag.CurrentCount = count;
        ViewBag.IsAppeal = appeal;

        if (files is null || !files.Any())
        {
            ViewBag.Error = "Please select at least one file.";
            ViewBag.Remaining = 10 - count;
            return View();
        }

        var (success, error, newCount, fileNames) =
            await _evidenceService.UploadAsync(
                roll, objNo, appeal, count, files);

        if (!success)
        {
            ViewBag.Error = error;
            ViewBag.Remaining = 10 - count;
            return View();
        }

        HttpContext.Session.SetInt32(SESSION_COUNT, newCount);

        var uploadedAt = DateTime.Now;
        var remainingSlots = Math.Max(0, 10 - newCount);

        try
        {
            await _emailService.SendEvidenceUploadConfirmationAsync(
                objNo,
                roll,
                appeal,
                fileNames,
                uploadedAt,
                remainingSlots);
        }
        catch (Exception ex)
        {
            // Evidence has already been uploaded. An email failure must not
            // roll back the saved files or the database evidence count.
            _logger.LogError(
                ex,
                "[Evidence Email] Upload succeeded but confirmation email failed for {ReferenceNo} on {RollSource}",
                objNo,
                roll);

            TempData["ev_emailWarning"] =
                "Your evidence was uploaded successfully, but the confirmation email could not be sent.";
        }

        TempData["ev_objNo"] = objNo;
        TempData["ev_roll"] = roll;
        TempData["ev_newCount"] = newCount;
        TempData["ev_fileNames"] = System.Text.Json.JsonSerializer.Serialize(fileNames);

        return RedirectToAction(nameof(Confirmation));
    }

    // ── GET /evidence/confirmation ────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    [Route("evidence/confirmation")]
    public async Task<IActionResult> Confirmation()
    {
        var objNo = TempData["ev_objNo"]?.ToString();
        if (string.IsNullOrEmpty(objNo))
            return RedirectToAction(nameof(VerifyObj));

        var fileNamesJson = TempData["ev_fileNames"]?.ToString() ?? "[]";
        var fileNames = System.Text.Json.JsonSerializer
            .Deserialize<List<string>>(fileNamesJson) ?? new();

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.ObjectionNo = objNo;
        ViewBag.RollSource = TempData["ev_roll"]?.ToString();
        ViewBag.NewCount = TempData["ev_newCount"];
        ViewBag.FileNames = fileNames;
        ViewBag.EmailWarning = TempData["ev_emailWarning"]?.ToString();

        TempData.Keep();
        return View();
    }

    // ── GET /evidence/download ────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    [Route("evidence/download")]
    public async Task<IActionResult> Download()
    {
        var objNo = TempData["ev_objNo"]?.ToString();
        var roll = TempData["ev_roll"]?.ToString() ?? "Objection_Supp3";
        var newCount = Convert.ToInt32(TempData["ev_newCount"] ?? "0");
        var names = System.Text.Json.JsonSerializer
            .Deserialize<List<string>>(
                TempData["ev_fileNames"]?.ToString() ?? "[]") ?? new();

        if (string.IsNullOrEmpty(objNo))
            return RedirectToAction(nameof(VerifyObj));

        TempData.Keep();

        var (pdf, fileName) = await _noticeService
            .GenerateAttachmentConfirmationAsync(objNo, roll, newCount, names);

        return File(pdf, "application/pdf", fileName);
    }

    // ── Roll detection ────────────────────────────────────────────────
    private static string DetectRollSource(string refNo)
    {
        if (string.IsNullOrWhiteSpace(refNo))
            return "Objection";

        var value = refNo
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", "-");

        // Check the highest supplementary roll first.
        if (value.Contains("SUP4") ||
            value.Contains("SUPP4") ||
            value.Contains("SUP-4") ||
            value.Contains("SUPP-4"))
        {
            return "Objection_Supp4";
        }

        if (value.Contains("SUP3") ||
            value.Contains("SUPP3") ||
            value.Contains("SUP-3") ||
            value.Contains("SUPP-3"))
        {
            return "Objection_Supp3";
        }

        if (value.Contains("SUP2") ||
            value.Contains("SUPP2") ||
            value.Contains("SUP-2") ||
            value.Contains("SUPP-2"))
        {
            return "Objection_Supp2";
        }

        if (value.Contains("SUP1") ||
            value.Contains("SUPP1") ||
            value.Contains("SUP-1") ||
            value.Contains("SUPP-1"))
        {
            return "Objection_Supp1";
        }

        return "Objection";
    }
}
