using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using V2_Genesis.Models.ViewModels.Home;
using V2_Genesis.Services;
using V2_Genesis.Services.Interfaces;


namespace GenesisV2.Controllers;

public class HomeController : Controller
{
    private readonly IAnnouncementService _announcement;
    private readonly DisclaimerSettings _disclaimer;
    private readonly ValuationRollSettings _roll;
    private readonly IHomeSearchService _homeSearchService;

    public HomeController(
        IAnnouncementService announcement,
        IOptions<DisclaimerSettings> disclaimerOpts,
        IOptions<ValuationRollSettings> rollOpts,
        IHomeSearchService homeSearchService)
    {
        _announcement = announcement;
        _disclaimer = disclaimerOpts.Value;
        _roll = rollOpts.Value;
        _homeSearchService = homeSearchService;
    }

    [HttpGet]
    [Route("/")]
    public IActionResult Index()
    {
        // Authenticated users → their dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return User.IsInRole("Admin")
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Dashboard");
        }

        var showDisclaimer = !Request.Cookies.ContainsKey(_disclaimer.CookieName);

        var vm = new LandingViewModel
        {
            Announcement = _announcement.GetAnnouncement(),
            Disclaimer = _disclaimer,
            Roll = _roll,
            ShowDisclaimer = showDisclaimer
        };

        return View(vm);
    }

    [HttpPost]
    [Route("accept-disclaimer")]
    public IActionResult AcceptDisclaimer()
    {
        Response.Cookies.Append(
            _disclaimer.CookieName,
            "1",
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddHours(_disclaimer.CookieExpiryHours),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

        return Ok();
    }

    // ════════════════════════════════════════════════════════
    //  ADD TO HomeController.cs
    //  Inject IHomeSearchService in constructor
    // ════════════════════════════════════════════════════════

    // Replace GET /home/towns with GET /home/townships
    [HttpGet]
    [AllowAnonymous]
    [Route("home/townships")]
    public async Task<IActionResult> GetTownships()
    {
        var (towns, schemes) = await _homeSearchService.GetTownsAndSchemesAsync();
        return Json(new { towns, schemes });
    }
    // ── POST /home/search — searches all rolls, returns results partial ───
    [HttpPost]
    [AllowAnonymous]
    [Route("home/search")]
    public async Task<IActionResult> Search(
        string? SearchTownName,
        string? SearchStand,
        string? SearchAddress,
        string? SearchScheme,
        string? SearchUnit)
    {
        if (string.IsNullOrWhiteSpace(SearchTownName) &&
            string.IsNullOrWhiteSpace(SearchStand) &&
            string.IsNullOrWhiteSpace(SearchAddress) &&
            string.IsNullOrWhiteSpace(SearchScheme) &&
            string.IsNullOrWhiteSpace(SearchUnit))
        {
            return PartialView("_HomeSearchEmpty");
        }

        var p = new HomeSearchParams
        {
            SearchTownName = SearchTownName?.Trim(),
            SearchStand = SearchStand?.Trim(),
            SearchAddress = SearchAddress?.Trim(),
            SearchScheme = SearchScheme?.Trim(),
            SearchUnit = SearchUnit?.Trim(),
        };

        var results = await _homeSearchService.SearchAllRollsAsync(p);
        var isAuth = User.Identity?.IsAuthenticated == true;

        ViewBag.IsAuthenticated = isAuth;
        ViewBag.ResultCount = results.Count;

        if (!results.Any())
            return PartialView("_HomeSearchNoResults");

        return PartialView("_HomeSearchResults", results);
    }
}