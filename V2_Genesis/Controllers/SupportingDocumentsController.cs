using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers
{
    [Authorize]
    public class SupportingDocumentsController : Controller
    {
        private readonly ISupportingDocumentService _documentService;
        private readonly IConfiguration _config;

        public SupportingDocumentsController(
            ISupportingDocumentService documentService,
            IConfiguration config)
        {
            _documentService = documentService;
            _config = config;
        }

        [HttpGet]
        [Route("supporting-documents/download")]
        public IActionResult Download(
            string referenceNo,
            string? rollSource,
            string fileName)
        {
            if (string.IsNullOrWhiteSpace(referenceNo) ||
                string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("Invalid document request.");
            }

            var root = _config["FileStorage:EvidenceRoot"];

            if (string.IsNullOrWhiteSpace(root))
                root = Path.Combine(Directory.GetCurrentDirectory(), "Evidence");

            var safeRoll = MakeSafeFileName(rollSource ?? "General");
            var safeRef = MakeSafeFileName(referenceNo);
            var safeFile = Path.GetFileName(fileName);

            var folder = Path.Combine(root, safeRoll, safeRef);
            var path = Path.Combine(folder, safeFile);

            if (!System.IO.File.Exists(path))
                return NotFound("Document was not found.");

            var fullFolder = Path.GetFullPath(folder);
            var fullPath = Path.GetFullPath(path);

            if (!fullPath.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file path.");

            var contentType = GetContentType(Path.GetExtension(path));

            return PhysicalFile(path, contentType, safeFile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("supporting-documents/add")]
        public async Task<IActionResult> Add(
            string referenceNo,
            string? rollSource,
            List<IFormFile> files)
        {
            var user = User.Identity?.Name;

            var result = await _documentService.AddDocumentsAsync(
                referenceNo,
                rollSource,
                files,
                user);

            if (!result.Success)
                TempData["DocumentError"] = result.Error;
            else
                TempData["DocumentSuccess"] = "Supporting document(s) uploaded successfully.";

            return RedirectToAction("Display", "Objection", new
            {
                referenceNo,
                rollSource
            });
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            return value.Trim();
        }

        private static string GetContentType(string ext)
        {
            return ext.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".heic" => "image/heic",
                ".heif" => "image/heif",
                _ => "application/octet-stream"
            };
        }
    }
}