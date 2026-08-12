using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using V2_Genesis.Data;
using V2_Genesis.Models.Rebates;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers;

[Authorize]
public class RebatesController : Controller
{
    private readonly IRebatesService _rebates;
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public RebatesController(
        IRebatesService rebates,
        ApplicationDbContext db,
        IConfiguration config)
    {
        _rebates = rebates;
        _db = db;
        _config = config;
    }

    // ── Helpers ───────────────────────────────────────────────────
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    private string UserEmail => User.FindFirstValue(ClaimTypes.Name) ?? "";

    private async Task SetGvList()
        => ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();

    // ════════════════════════════════════════════════════════════
    //  GET — Landing, Dashboard, Display
    // ════════════════════════════════════════════════════════════

    [HttpGet, Route("Rebates/Index")]
    public async Task<IActionResult> Index()
    {
        await SetGvList();
        return View();
    }

    [HttpGet, Route("Rebates/Display")]
    public async Task<IActionResult> Display()
    {
        await SetGvList();
        return View();
    }

    [HttpGet, Route("Rebates/RebatesDashboard")]
    public async Task<IActionResult> RebatesDashboard()
    {
        await SetGvList();
        ViewBag.Rebates = await _rebates.GetDashboardAsync(UserId);
        return View();
    }

    // ════════════════════════════════════════════════════════════
    //  VIEW SUBMITTED REBATE — returns correct partial per type
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> ViewData(string? RebateType, string? RebateNo)
    {
        if (string.IsNullOrEmpty(RebateNo)) return BadRequest();

        var data = await _rebates.GetRebateDataAsync(RebateNo);

        // Match the RebateType constant strings from RebateType static class
        return RebateType switch
        {
            "Pensioner70" => PartialView("_ViewForm70", data),
            "Pensioner60" => PartialView("_ViewForm60", data),
            "HighDensity" or "PBO" or "Disaster" => PartialView("_ViewFormDensity", data),
            "ChildHeaded" => PartialView("_ViewFormChildHeadedHH", data),
            "LifeRights" => PartialView("_LifeRights", data),
            "SportsClub" or "ProtectionAnimal" => PartialView("_SportsClub", data),
            "Heritage" => PartialView("_HeritageSites", data),
            "Education" => PartialView("_Education", data),
            _ => View(data)
        };
    }


    // ════════════════════════════════════════════════════════════
    //  DOWNLOAD ACKNOWLEDGEMENT PDF
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public IActionResult Download(string rebateNo, string? returnUrl = null)
    {
        try
        {
            var root = _config["ObjectionRolls:Rebates:RebateRooTPath"]
                        ?? throw new InvalidOperationException("RebateRooTPath missing.");
            var path = Path.Combine(root, rebateNo, $"{rebateNo}_Acknowledgement.pdf");

            if (!System.IO.File.Exists(path))
            {
                TempData["NoticeError"] = "The rebate acknowledgement was not found.";
                return RedirectAfterDownload(returnUrl, "Rebates");
            }

            return new FileStreamResult(
                new FileStream(path, FileMode.Open, FileAccess.Read),
                "application/pdf")
            {
                FileDownloadName = $"{rebateNo}_Acknowledgement.pdf"
            };
        }
        catch
        {
            TempData["NoticeError"] =
                "The rebate acknowledgement could not be downloaded.";
            return RedirectAfterDownload(returnUrl, "Rebates");
        }
    }

    private IActionResult RedirectAfterDownload(string? returnUrl, string openRoll)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        var isAdmin = User.IsInRole("Admin") ||
            User.FindFirstValue("UMRole")?.Equals(
                "Admin", StringComparison.OrdinalIgnoreCase) == true ||
            !string.IsNullOrWhiteSpace(User.FindFirstValue("SAPNumber"));

        return isAdmin
            ? RedirectToAction("Index", "Admin", new { openRoll })
            : RedirectToAction("Index", "Dashboard", new { openRoll });
    }

    // ════════════════════════════════════════════════════════════
    //  GET — All 11 form views
    // ════════════════════════════════════════════════════════════

    [HttpGet, Route("Rebates/Pensioner")]
    public async Task<IActionResult> Pensioner() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/Pensioner70")]
    public async Task<IActionResult> ViewForm70() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/Pensioner60")]
    public async Task<IActionResult> ViewForm60() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/ChildHeadedHouseHold")]
    public async Task<IActionResult> ChildHeadedHouseHold() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/PBO")]
    public async Task<IActionResult> PBO() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/DisasterRebate")]
    public async Task<IActionResult> DisasterRebate() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/LifeRights")]
    public async Task<IActionResult> LifeRights() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/HeritageSites")]
    public async Task<IActionResult> HeritageSites() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/SportsClubRebate")]
    public async Task<IActionResult> SportsClubRebate() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/ProtectionAnimal")]
    public async Task<IActionResult> ProtectionAnimal() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/EducationRebate")]
    public async Task<IActionResult> EducationRebate() { await SetGvList(); return View(); }

    [HttpGet, Route("Rebates/HighDensity")]
    public async Task<IActionResult> HighDensity() { await SetGvList(); return View(); }

    // ════════════════════════════════════════════════════════════
    //  POST — all 11 pass their view name to HandleSubmit
    //  so that on validation failure the correct view is returned
    // ════════════════════════════════════════════════════════════

    [HttpPost, Route("Rebates/Pensioner70"), ValidateAntiForgeryToken]
    public Task<IActionResult> ViewForm70Post(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.Pensioner70, "ViewForm70", b, files, fileR);

    [HttpPost, Route("Rebates/Pensioner60"), ValidateAntiForgeryToken]
    public Task<IActionResult> ViewForm60Post(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.Pensioner60, "ViewForm60", b, files, fileR);

    [HttpPost, Route("Rebates/ChildHeadedHouseHold"), ValidateAntiForgeryToken]
    public Task<IActionResult> ChildHeadedPost(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.ChildHeaded, "ChildHeadedHouseHold", b, files, fileR);

    [HttpPost, Route("Rebates/PBO"), ValidateAntiForgeryToken]
    public Task<IActionResult> PBOPost(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.PBO, "PBO", b, files, fileR);

    [HttpPost, Route("Rebates/DisasterRebate"), ValidateAntiForgeryToken]
    public Task<IActionResult> DisasterPost(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.Disaster, "DisasterRebate", b, files, fileR);

    [HttpPost, Route("Rebates/LifeRights"), ValidateAntiForgeryToken]
    public Task<IActionResult> LifeRightsPost(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.LifeRights, "LifeRights", b, files, fileR);

    [HttpPost, Route("Rebates/HeritageSites"), ValidateAntiForgeryToken]
    public Task<IActionResult> HeritagePost(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.Heritage, "HeritageSites", b, files, fileR);

    [HttpPost, Route("Rebates/SportsClubRebate"), ValidateAntiForgeryToken]
    public Task<IActionResult> SportsClubPost(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.SportsClub, "SportsClubRebate", b, files, fileR);

    [HttpPost, Route("Rebates/ProtectionAnimal"), ValidateAntiForgeryToken]
    public Task<IActionResult> ProtectionAnimalPost(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.ProtectionAnimal, "ProtectionAnimal", b, files, fileR);

    [HttpPost, Route("Rebates/EducationRebate"), ValidateAntiForgeryToken]
    public Task<IActionResult> EducationPost(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.Education, "EducationRebate", b, files, fileR);

    [HttpPost, Route("Rebates/HighDensity"), ValidateAntiForgeryToken]
    public Task<IActionResult> HighDensityPost(
        [FromForm] RebateFormBinding b, List<IFormFile> files, List<IFormFile> fileR)
        => HandleSubmit(RebateType.HighDensity, "HighDensity", b, files, fileR);

    // ════════════════════════════════════════════════════════════
    //  SHARED HANDLER
    // ════════════════════════════════════════════════════════════

    private async Task<IActionResult> HandleSubmit(
        string rebateType,
        string viewName,      // ← view to return on error
        RebateFormBinding b,
        List<IFormFile> files,
        List<IFormFile> fileR)
    {
        await SetGvList();

        try
        {
            var result = await _rebates.SubmitAsync(
                rebateType, UserId, UserEmail,
                b.Info, b.S1, b.S2, b.S3, b.S4,
                b.S5, b.S6, b.S7, b.S8, b.S9,
                b.S10, b.S11, b.Files, files, fileR);

            // ── Populate TempData for Display view ───────────────
            TempData["id"] = result.RebateNo;
            TempData["Count"] = result.FileCount;
            TempData["Date"] = result.SubmittedAt;
            TempData["Status"] = result.status;        // "Acknowledge" or "Auto Reject"
            TempData["Type"] = rebateType;           // human-readable rebate type

            for (int i = 0; i < 10; i++)
                TempData[$"File{i + 1}"] = result.files[i]; // ← capital F (was bug)

            return RedirectToAction("Display");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("",
                "Unable to save changes. Try again — if the problem persists " +
                "please contact your system administrator.");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Submission failed: {ex.Message}");
        }

        // On error — return the originating form view, not the action name
        return View(viewName, b);
    }
}
