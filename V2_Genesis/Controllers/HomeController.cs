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
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IAnnouncementService announcement,
        IOptions<DisclaimerSettings> disclaimerOpts,
        IOptions<ValuationRollSettings> rollOpts,
        IHomeSearchService homeSearchService,ILogger<HomeController> logger)
    {
        _announcement = announcement;
        _disclaimer = disclaimerOpts.Value;
        _roll = rollOpts.Value;
        _homeSearchService = homeSearchService;
        _logger = logger;
    }
    public IActionResult Contact() { 
    return View();
    }
    public IActionResult FAQ()
    {
        return View();
    }
    public IActionResult Privacy()
    {
        return View();
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

    [HttpGet]
    [AllowAnonymous]
    [Route("home/townships")]
    public async Task<IActionResult> GetTownships()
    {
        var (towns, schemes) = await _homeSearchService.GetTownsAndSchemesAsync();

        _logger.LogInformation(
            "[HomeController] /home/townships → {T} towns, {S} schemes",
            towns.Count, schemes.Count);

        return Json(new { towns, schemes });
    }

    // ── POST /home/search ────────────────────────────────────────────────
    // Searches all rolls and returns the results partial.
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
        // At least one field required
        if (string.IsNullOrWhiteSpace(SearchTownName) &&
            string.IsNullOrWhiteSpace(SearchStand) &&
            string.IsNullOrWhiteSpace(SearchAddress) &&
            string.IsNullOrWhiteSpace(SearchScheme) &&
            string.IsNullOrWhiteSpace(SearchUnit))
        {
            return Content(string.Empty);
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

        ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated == true;

        if (!results.Any())
            return PartialView("_HomeSearchNoResults");

        return PartialView("_HomeSearchResults", results);
    }
}