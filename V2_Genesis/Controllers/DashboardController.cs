

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Entities;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services;

namespace V2_Genesis.Controllers;

[Authorize(Roles = "Client")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAnnouncementService _announcement;

    public DashboardController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAnnouncementService announcement)
    {
        _db = db;
        _userManager = userManager;
        _announcement = announcement;
    }

    [HttpGet]
    [Route("dashboard")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        // ── Load all rolls from GV_LIST ordered by ID ─────────────────────
        var rolls = await _db.GvList
            .OrderBy(r => r.ID)
            .ToListAsync();

        // ── Pass rolls to layout for dynamic navbar ────────────────────────
        ViewBag.GvList = rolls;

        // ── Build per-roll data buckets (stubs — replace with real queries) ─
        var rollData = new Dictionary<string, RollData>();
        foreach (var roll in rolls)
        {
            rollData[roll.Source] = await GetRollDataAsync(roll, user.Id);
        }

        var vm = new ClientDashboardViewModel
        {
            DisplayName = user.DisplayName,
            IsCompany = user.IsCompany,
            UserId = user.Id,
            Announcement = _announcement.GetAnnouncement(),
            Rolls = rolls,
            RollData = rollData
        };

        return View(vm);
    }

    // ── STUB — replace each case with real DB query when ready ─────────────
    private Task<RollData> GetRollDataAsync(GvList roll, string userId)
    {
        // TODO: You will provide the actual query per roll.
        // Pattern: query the linked-properties table filtered by userId + roll.Source
        // and return the three lists.
        //
        // Example:
        // case "Objection_Supp3":
        //     var linked = await _db.LinkedSup3
        //         .Where(x => x.UserId == userId)
        //         .ToListAsync();
        //     ...

        return Task.FromResult(new RollData());   // empty stub
    }
}