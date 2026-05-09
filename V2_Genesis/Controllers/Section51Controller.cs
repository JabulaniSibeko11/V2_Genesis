using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers;

[AllowAnonymous]
public class Section51Controller : Controller
{
    private readonly ISection51Service _s51Service;
    private readonly INoticeService _noticeService;
    private readonly ApplicationDbContext _db;

    private const string S_VALIDATED = "s51_validated";
    private const string S_ROLL = "s51_roll";
    private const string S_OBJ = "s51_objno";
    private const string S_IS_APPEAL = "s51_appeal";

    public Section51Controller(
        ISection51Service s51Service,
        INoticeService noticeService,
        ApplicationDbContext db)
    {
        _s51Service = s51Service;
        _noticeService = noticeService;
        _db = db;
    }

    // ── Roll detection ─────────────────────────────────────────────
    private static string DetectRoll(string refNo)
    {
        var u = refNo.Trim().ToUpper();
        if (u.Contains("SUP3") || u.Contains("SUPP3")) return "Objection_Supp3";
        if (u.Contains("SUP2") || u.Contains("SUPP2")) return "Objection_Supp2";
        if (u.Contains("SUP1") || u.Contains("SUPP1")) return "Objection_Supp1";
        return "Objection";
    }

    // ── GET /section51/verify ──────────────────────────────────────
    [HttpGet]
    [Route("section51/verify")]
    public async Task<IActionResult> Verify()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        return View();
    }

    // ── POST /section51/verify ─────────────────────────────────────
    [HttpPost]
    [Route("section51/verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(string objectionNo, string pin)
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

        if (string.IsNullOrWhiteSpace(objectionNo) ||
            string.IsNullOrWhiteSpace(pin))
        {
            ViewBag.Error = "Please enter both the objection number and PIN.";
            return View();
        }

        var rollSource = DetectRoll(objectionNo);

        var result = await _s51Service.ValidateAsync(
            rollSource, objectionNo.Trim(), pin.Trim());

        if (!result.IsValid && !result.AlreadyDone && !result.PastDeadline)
        {
            ViewBag.Error = result.Error;
            return View();
        }

        // Show limit page if already done or past deadline
        if (result.AlreadyDone || result.PastDeadline)
        {
            TempData["s51_limit_reason"] = result.PastDeadline
                ? "The Section 51 upload period has closed."
                : "You have already submitted Section 51 evidence for this objection.";
            return RedirectToAction(nameof(Limit));
        }

        HttpContext.Session.SetString(S_VALIDATED, "true");
        HttpContext.Session.SetString(S_ROLL, rollSource);
        HttpContext.Session.SetString(S_OBJ, objectionNo.Trim());

        return RedirectToAction(nameof(Upload));
    }

    // ── GET /section51/limit ───────────────────────────────────────
    [HttpGet]
    [Route("section51/limit")]
    public async Task<IActionResult> Limit()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.Reason = TempData["s51_limit_reason"]?.ToString()
            ?? "Section 51 uploads are no longer available for this submission.";
        return View();
    }

    // ── GET /section51/upload ──────────────────────────────────────
    [HttpGet]
    [Route("section51/upload")]
    public async Task<IActionResult> Upload()
    {
        if (HttpContext.Session.GetString(S_VALIDATED) != "true")
            return RedirectToAction(nameof(Verify));

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.ObjectionNo = HttpContext.Session.GetString(S_OBJ);
        ViewBag.RollSource = HttpContext.Session.GetString(S_ROLL);
        return View();
    }

    // ── POST /section51/upload ─────────────────────────────────────
    [HttpPost]
    [Route("section51/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        if (HttpContext.Session.GetString(S_VALIDATED) != "true")
            return RedirectToAction(nameof(Verify));

        var objNo = HttpContext.Session.GetString(S_OBJ)!;
        var roll = HttpContext.Session.GetString(S_ROLL)!;

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.ObjectionNo = objNo;
        ViewBag.RollSource = roll;

        if (files is null || !files.Any())
        {
            ViewBag.Error = "Please select at least one file.";
            return View();
        }

        var (success, error, fileCount, fileNames) =
            await _s51Service.UploadAsync(roll, objNo, files);

        if (!success)
        {
            ViewBag.Error = error;
            return View();
        }

        TempData["s51_objNo"] = objNo;
        TempData["s51_roll"] = roll;
        TempData["s51_count"] = fileCount;
        TempData["s51_files"] = System.Text.Json.JsonSerializer.Serialize(fileNames);

        return RedirectToAction(nameof(Confirmation));
    }

    // ── GET /section51/confirmation ────────────────────────────────
    [HttpGet]
    [Route("section51/confirmation")]
    public async Task<IActionResult> Confirmation()
    {
        var objNo = TempData["s51_objNo"]?.ToString();
        if (string.IsNullOrEmpty(objNo))
            return RedirectToAction(nameof(Verify));

        var fileNames = System.Text.Json.JsonSerializer
            .Deserialize<List<string>>(
                TempData["s51_files"]?.ToString() ?? "[]") ?? new();

        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        ViewBag.ObjectionNo = objNo;
        ViewBag.RollSource = TempData["s51_roll"]?.ToString();
        ViewBag.FileCount = TempData["s51_count"];
        ViewBag.FileNames = fileNames;

        TempData.Keep();
        return View();
    }

    // ── GET /section51/download ────────────────────────────────────
    [HttpGet]
    [Route("section51/download")]
    public async Task<IActionResult> Download()
    {
        var objNo = TempData["s51_objNo"]?.ToString();
        var roll = TempData["s51_roll"]?.ToString() ?? "Objection_Supp3";
        var count = Convert.ToInt32(TempData["s51_count"] ?? "0");
        var fileNames = System.Text.Json.JsonSerializer
            .Deserialize<List<string>>(
                TempData["s51_files"]?.ToString() ?? "[]") ?? new();

        if (string.IsNullOrEmpty(objNo))
            return RedirectToAction(nameof(Verify));

        TempData.Keep();

        var (pdf, fileName) = await _noticeService
            .GenerateSection51AcknowledgementAsync(objNo, roll, count, fileNames);

        return File(pdf, "application/pdf", fileName);
    }
}