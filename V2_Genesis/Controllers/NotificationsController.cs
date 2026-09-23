using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notifications;

        public NotificationsController(INotificationService notifications)
        {
            _notifications = notifications;
        }
        private bool IsAdmin(string email) =>
        User.IsInRole("Admin")
        || email.Equals("AdministrationEnquiries@Joburg.org.za", StringComparison.OrdinalIgnoreCase)
        || Regex.IsMatch(email, @"^val\.admin(1[0-9]?|[1-9])@joburg\.org\.za$", RegexOptions.IgnoreCase);

        // GET /notifications/{id}/open
        [HttpGet]
        [Route("notifications/{id:long}/open")]
        public async Task<IActionResult> Open(long id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail =
                User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.Identity?.Name
                ?? string.Empty;

            var isAdmin = IsAdmin(userEmail);

            var target = await _notifications.OpenAsync(id, userId, userEmail, isAdmin);

            // Never redirect off-site, whatever is stored in the Url column.
            if (!string.IsNullOrWhiteSpace(target) && Url.IsLocalUrl(target))
                return LocalRedirect(target);

            return LocalRedirect(isAdmin ? "/admin" : "/dashboard");
        }

    }
}
