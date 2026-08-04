using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers
{
    [Authorize]
    public sealed class SubmissionController : Controller
    {
        private readonly ISubmissionViewService _submissionViewService;

        private static readonly Regex AdminEmailRx =
            new(
                @"^val\.admin(1[0-9]?|[1-9])@joburg\.org\.za$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public SubmissionController(ISubmissionViewService submissionViewService)
        {
            _submissionViewService = submissionViewService;
        }

        [HttpGet("submissions/view/{submissionType}/{referenceNumber}", Name = "ViewSubmission")]
        public async Task<IActionResult> ViewSubmission(
            string submissionType,
            string referenceNumber,
            string? rollSource,
            string? returnUrl,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            rollSource = string.IsNullOrWhiteSpace(rollSource)
                ? HttpContext.Session.GetString("RollSource") ?? "Objection"
                : rollSource.Trim();

            var identityValue =
                User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? string.Empty;

            var adminAppEmail =
                User.FindFirstValue("AdminAppEmail")
                ?? HttpContext.Session.GetString("AdminAppEmail")
                ?? string.Empty;

            var isAdmin =
                User.IsInRole("Admin")
                || User.FindFirstValue("UMRole")?.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase) == true
                || !string.IsNullOrWhiteSpace(
                    User.FindFirstValue("SAPNumber"))
                || identityValue.Equals(
                    "AdministrationEnquiries@Joburg.org.za",
                    StringComparison.OrdinalIgnoreCase)
                || adminAppEmail.Equals(
                    "AdministrationEnquiries@Joburg.org.za",
                    StringComparison.OrdinalIgnoreCase)
                || AdminEmailRx.IsMatch(identityValue);

            var result = await _submissionViewService.GetSubmissionAsync(
                submissionType,
                referenceNumber,
                rollSource,
                userId,
                cancellationToken,
                allowAdministrativeAccess: isAdmin);

            if (!result.Success || result.Submission is null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "The submitted form could not be found.";
                return isAdmin
                    ? RedirectToAction(
                        "Index",
                        "Admin",
                        new { openRoll = rollSource })
                    : RedirectToAction(
                        "Index",
                        "Dashboard",
                        new { openRoll = rollSource });
            }

            ViewData["ReturnUrl"] =
                !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : isAdmin
                        ? Url.Action(
                            "Index",
                            "Admin",
                            new { openRoll = rollSource }) ?? "/admin"
                        : Url.Action(
                            "Index",
                            "Dashboard",
                            new { openRoll = rollSource }) ?? "/Dashboard";

            ViewData["IsAdminView"] = isAdmin;

            return View("ViewSubmission", result.Submission);
        }
    }
}
