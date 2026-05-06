using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GenesisV2.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAnnouncementService _announcement;

    public AdminController(IAnnouncementService announcement)
        => _announcement = announcement;

    [HttpGet]
    [Route("admin")]
    public IActionResult Index()
    {
        var sapClaim = User.FindFirstValue("SAPNumber") ?? string.Empty;
        var roleClaim = User.FindFirstValue("UMRole") ?? "Admin";

        var vm = new AdminDashboardViewModel
        {
            AdminName = sapClaim,
            Role = roleClaim,
            Announcement = _announcement.GetAnnouncement(),
            // TODO: replace with real DB queries
            TotalSubmissions = 0,
            PendingReview = 0,
            Approved = 0,
            Rejected = 0,
            TotalUsers = 0,
            TotalProperties = 0
        };

        return View(vm);
    }
}