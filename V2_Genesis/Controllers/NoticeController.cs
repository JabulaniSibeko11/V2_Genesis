using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Controllers;

[Authorize]
public class NoticeController : Controller
{
    private readonly INoticeService _notice;
    private readonly IPropertySearchService _search;
    private readonly ApplicationDbContext _db;
    private readonly RollDatesSettings _rollDates;
    public NoticeController(
        INoticeService notice,
        IPropertySearchService search,
        ApplicationDbContext db, IOptions<RollDatesSettings> rollDatesOpts)
    {
        _notice = notice;
        _search = search;
        _db = db;
        _rollDates = rollDatesOpts.Value;
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
}