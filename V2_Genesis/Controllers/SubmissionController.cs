using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers
{
    [Authorize]
    public sealed class SubmissionController : Controller
    {
        private readonly ISubmissionViewService _submissionViewService;

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

            var result = await _submissionViewService.GetSubmissionAsync(
                submissionType,
                referenceNumber,
                rollSource,
                userId,
                cancellationToken);

            if (!result.Success || result.Submission is null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "The submitted form could not be found.";
                return RedirectToAction("Index", "Dashboard", new { openRoll = rollSource });
            }

            ViewData["ReturnUrl"] =
                !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : Url.Action("Index", "Dashboard", new { openRoll = rollSource }) ?? "/Dashboard";

            return View("ViewSubmission", result.Submission);
        }
    }
}
