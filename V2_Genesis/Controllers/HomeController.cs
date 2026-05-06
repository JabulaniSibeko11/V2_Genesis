using V2_Genesis.Models.ViewModels.Home;
using V2_Genesis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace GenesisV2.Controllers;

public class HomeController : Controller
{
    private readonly IAnnouncementService _announcement;
    private readonly DisclaimerSettings _disclaimer;
    private readonly ValuationRollSettings _roll;

    public HomeController(
        IAnnouncementService announcement,
        IOptions<DisclaimerSettings> disclaimerOpts,
        IOptions<ValuationRollSettings> rollOpts)
    {
        _announcement = announcement;
        _disclaimer = disclaimerOpts.Value;
        _roll = rollOpts.Value;
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
}