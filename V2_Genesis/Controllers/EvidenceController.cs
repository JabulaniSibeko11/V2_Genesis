using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers;

[Authorize]
public class EvidenceController : Controller
{
    private readonly IEvidenceService _evidenceService;
    private readonly INoticeService _noticeService;
    private readonly ApplicationDbContext _db;

    private const string SESSION_VALIDATED = "ev_validated";
    private const string SESSION_ROLL = "ev_roll";
    private const string SESSION_OBJ = "ev_objno";
    private const string SESSION_COUNT = "ev_count";
    private const string SESSION_IS_APPEAL = "ev_appeal";

    public EvidenceController(
        IEvidenceService evidenceService,
        INoticeService noticeService,
        ApplicationDbContext db)
    {
        _evidenceService = evidenceService;
        _noticeService = noticeService;
        _db = db;
    }

    // ── GET /evidence/verify ──────────────────────────────────────────
    [HttpGet]
    [Route("evidence/verify")]
    public async Task<IActionResult> Verify(
        string objectionNo, string rollSource)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.ObjectionNo = objectionNo;
        ViewBag.RollSource = rollSource;
        ViewBag.IsAppeal = objectionNo.Trim().ToUpper().StartsWith("APP");
        return View();
    }

    // ── POST /evidence/verify ─────────────────────────────────────────
    [HttpPost]
    [Route("evidence/verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(
        string objectionNo, string rollSource, string pin)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.ObjectionNo = objectionNo;
        ViewBag.RollSource = rollSource;
        ViewBag.IsAppeal = objectionNo.Trim().ToUpper().StartsWith("APP");

        var result = await _evidenceService.ValidateAsync(
            rollSource, objectionNo, pin);

        //if (!result.IsValid)
        //{
        //    ViewBag.Error = result.Error;
        //    return View();
        //}

        // Store validated state in session
        HttpContext.Session.SetString(SESSION_VALIDATED, "true");
        HttpContext.Session.SetString(SESSION_ROLL, rollSource);
        HttpContext.Session.SetString(SESSION_OBJ, objectionNo);
        HttpContext.Session.SetInt32(SESSION_COUNT, result.CurrentCount);
        HttpContext.Session.SetString(SESSION_IS_APPEAL, result.IsAppeal.ToString());

        return RedirectToAction(nameof(Upload));
    }

    // ── GET /evidence/upload ──────────────────────────────────────────
    [HttpGet]
    [Route("evidence/upload")]
    public async Task<IActionResult> Upload()
    {
        if (HttpContext.Session.GetString(SESSION_VALIDATED) != "true")
            return RedirectToAction("Index", "Dashboard");

        var objNo = HttpContext.Session.GetString(SESSION_OBJ)!;
        var roll = HttpContext.Session.GetString(SESSION_ROLL)!;
        var count = HttpContext.Session.GetInt32(SESSION_COUNT) ?? 0;
        bool appeal = bool.Parse(
            HttpContext.Session.GetString(SESSION_IS_APPEAL) ?? "false");

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.ObjectionNo = objNo;
        ViewBag.RollSource = roll;
        ViewBag.CurrentCount = count;
        ViewBag.Remaining = 10 - count;
        ViewBag.IsAppeal = appeal;

        return View();
    }

    // ── POST /evidence/upload ─────────────────────────────────────────
    [HttpPost]
    [Route("evidence/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        if (HttpContext.Session.GetString(SESSION_VALIDATED) != "true")
            return RedirectToAction("Index", "Dashboard");

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

        // Update session count
        HttpContext.Session.SetInt32(SESSION_COUNT, newCount);

        // Pass confirmation data
        TempData["ev_objNo"] = objNo;
        TempData["ev_roll"] = roll;
        TempData["ev_newCount"] = newCount;
        TempData["ev_fileNames"] = System.Text.Json.JsonSerializer
            .Serialize(fileNames);

        return RedirectToAction(nameof(Confirmation));
    }

    // ── GET /evidence/confirmation ────────────────────────────────────
    [HttpGet]
    [Route("evidence/confirmation")]
    public async Task<IActionResult> Confirmation()
    {
        var objNo = TempData["ev_objNo"]?.ToString();
        if (string.IsNullOrEmpty(objNo))
            return RedirectToAction("Index", "Dashboard");

        var fileNamesJson = TempData["ev_fileNames"]?.ToString() ?? "[]";
        var fileNames = System.Text.Json.JsonSerializer
            .Deserialize<List<string>>(fileNamesJson) ?? new();

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.ObjectionNo = objNo;
        ViewBag.RollSource = TempData["ev_roll"]?.ToString();
        ViewBag.NewCount = TempData["ev_newCount"];
        ViewBag.FileNames = fileNames;

        TempData.Keep();
        return View();
    }

    // ── GET /evidence/download ────────────────────────────────────────
    [HttpGet]
    [Route("evidence/download")]
    public async Task<IActionResult> Download()
    {
        var objNo = TempData["ev_objNo"]?.ToString();
        var roll = TempData["ev_roll"]?.ToString();
        var newCount = Convert.ToInt32(TempData["ev_newCount"] ?? "0");
        var namesJson = TempData["ev_fileNames"]?.ToString() ?? "[]";
        var fileNames = System.Text.Json.JsonSerializer
            .Deserialize<List<string>>(namesJson) ?? new();

        if (string.IsNullOrEmpty(objNo))
            return RedirectToAction("Index", "Dashboard");

        TempData.Keep();

        var (pdf, fileName) = await _noticeService
            .GenerateAttachmentConfirmationAsync(
                objNo, roll ?? "Objection_Supp3", newCount, fileNames);

        return File(pdf, "application/pdf", fileName);
    }
}