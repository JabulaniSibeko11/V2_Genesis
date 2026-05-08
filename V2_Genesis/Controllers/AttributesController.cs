using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;

namespace V2_Genesis.Controllers;

[Authorize]
public class AttributesController : Controller
{
    private readonly ApplicationDbContext _db;

    public AttributesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ── GET /attributes/about ─────────────────────────────────────────
    [HttpGet]
    [Route("attributes/about")]
    public async Task<IActionResult> About()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        return View();
    }

    // ── GET /attributes/search (placeholder — build next) ────────────
    [HttpGet]
    [Route("attributes/search")]
    public async Task<IActionResult> Search()
    {
        ViewBag.GvList = await _db.GvList.OrderBy(r => r.ID).ToListAsync();
        return View();
    }
}