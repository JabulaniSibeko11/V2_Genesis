using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using V2_Genesis.Models.ViewModels.ValuerInspectionEvidence;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers
{
    [Route("valuer-inspection")]
    public class ValuerInspectionEvidenceController : Controller
    {
        private readonly IValuerInspectionEvidenceService _service;

        public ValuerInspectionEvidenceController(
            IValuerInspectionEvidenceService service)
        {
            _service = service;
        }

        [HttpGet("")]
        [HttpGet("verify")]
        public IActionResult Verify()
        {
            return View(new ValuerInspectionVerifyVm());
        }

        [HttpPost("today")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Today(ValuerInspectionVerifyVm vm)
        {
            try
            {
                var model = await _service.GetTodayInspectionsAsync(vm.SapNumber);

                TempData["SapNumber"] = model.SapNumber;

                return View("Today", model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View("Verify", vm);
            }
        }

        [HttpGet("upload")]
        public async Task<IActionResult> Upload(
            long inspectionId,
            string? sapNumber)
        {
            var cleanSap = string.IsNullOrWhiteSpace(sapNumber)
                ? TempData.Peek("SapNumber")?.ToString()
                : sapNumber.Trim();

            if (inspectionId <= 0 || string.IsNullOrWhiteSpace(cleanSap))
            {
                TempData["Error"] =
                    "The inspection or SAP number was not supplied. Please verify your SAP number again.";
                return RedirectToAction(nameof(Verify));
            }

            try
            {
                TempData["SapNumber"] = cleanSap;
                var model = await _service.GetInspectionForUploadAsync(
                    inspectionId,
                    cleanSap);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                var model = await _service.GetTodayInspectionsAsync(cleanSap);
                return View("Today", model);
            }
        }

        [HttpGet("today")]
        public async Task<IActionResult> Today(string? sapNumber)
        {
            var cleanSap = string.IsNullOrWhiteSpace(sapNumber)
                ? TempData.Peek("SapNumber")?.ToString()
                : sapNumber.Trim();

            if (string.IsNullOrWhiteSpace(cleanSap))
                return RedirectToAction(nameof(Verify));

            try
            {
                TempData["SapNumber"] = cleanSap;
                var model = await _service.GetTodayInspectionsAsync(cleanSap);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Verify));
            }
        }

        [HttpPost("upload")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(110_000_000)]
        public async Task<IActionResult> Upload(UploadValuerInspectionEvidenceVm vm)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName =
                    User.FindFirstValue(ClaimTypes.Name)
                    ?? User.Identity?.Name
                    ?? vm.SapNumber;

                await _service.UploadEvidenceAsync(vm, userId, userName);

                TempData["Success"] = "Inspection evidence uploaded successfully.";

                var model = await _service.GetTodayInspectionsAsync(vm.SapNumber);

                return View("Today", model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                var model = await _service.GetTodayInspectionsAsync(vm.SapNumber);

                return View("Today", model);
            }
        }
    }
}
