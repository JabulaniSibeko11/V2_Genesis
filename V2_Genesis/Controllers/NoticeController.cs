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
    private readonly IPropertySearchService _search;
    private readonly ApplicationDbContext _db;
    private readonly RollDatesSettings _rollDates;
    private readonly ILogger<NoticeController> _logger;
    public NoticeController(
        INoticeService notice,
        IPropertySearchService search,
        ApplicationDbContext db, IOptions<RollDatesSettings> rollDatesOpts,ILogger<NoticeController>logger)
    {
        _notice = notice;
        _search = search;
        _db = db;
        _rollDates = rollDatesOpts.Value;
        _logger = logger;
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
    // ── GET /notice/acknowledgement/download ──────────────────────────────
    // Called from dashboard "Acknowledgement" button with objectionNo + rollSource
    [HttpGet]
    [Route("notice/acknowledgement/download")]
    public async Task<IActionResult> DownloadAcknowledgement(
        string objectionNo,
        string rollSource)
    {
        // Build from TempData if available (post-submission flow)
        AcknowledgementData data;

        if (TempData.ContainsKey("pin") || TempData.ContainsKey("objection_ref"))
        {
            data = AcknowledgementData.FromTempData(TempData, rollSource);

            // Keep TempData alive for page refresh
            foreach (var key in new[]
            {
            "Id","pin","Count","desc","time","section51pin",
            "new_Property_Description","new_Category","new2_Category","new3_Category",
            "new_Address","new_Extent","new2_Extent","new3_Extent",
            "new_Market_Value","new2_Market_Value","new3_Market_Value",
            "new_Owner","objection_ref","objection_reason",
            "Old_Property_Description","Old_Category","Old2_Category","Old3_Category",
            "Old_Address","Old_Extent","Old2_Extent","Old3_Extent",
            "Old_Market_Value","Old2_Market_Value","Old3_Market_Value","Old_Owner"
        })
                TempData.Keep(key);
        }
        else
        {
            // No TempData — override with what we know
            // (SP fetch can be added later per roll)
            data = new AcknowledgementData
            {
                ObjectionNo = objectionNo,
                ObjectionRef = objectionNo,
                RollSource = rollSource,
                SubmissionTime = DateTime.Now.ToString("dd MMMM yyyy HH:mm")
            };
        }

        // Ensure objection number is set
        if (string.IsNullOrWhiteSpace(data.ObjectionNo))
            data.ObjectionNo = objectionNo;

        try
        {
            var (pdf, fileName) = await _notice.GenerateAcknowledgementAsync(data);
            return File(pdf, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            TempData["NoticeError"] = $"Could not generate acknowledgement: {ex.Message}";
            return RedirectToAction("Index", "Dashboard");
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
 
