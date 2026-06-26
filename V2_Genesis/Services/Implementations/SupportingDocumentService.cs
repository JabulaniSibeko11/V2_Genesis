using V2_Genesis.Models.ViewModels;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class SupportingDocumentService : ISupportingDocumentService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SupportingDocumentService> _logger;

        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".heic", ".heif"
        };

        public SupportingDocumentService(
            IConfiguration config,
            ILogger<SupportingDocumentService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public Task<List<SupportingDocumentViewModel>> GetDocumentsAsync(
            string referenceNo,
            string? rollSource)
        {
            var folder = GetReferenceFolder(referenceNo, rollSource);

            if (!Directory.Exists(folder))
                return Task.FromResult(new List<SupportingDocumentViewModel>());

            var files = Directory.GetFiles(folder)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();

                    if (!AllowedExtensions.Contains(ext))
                        return false;

                    var name = Path.GetFileName(f);

                    // Hide generated system PDFs from the supporting documents list
                    if (name.Contains("Acknowledgement", StringComparison.OrdinalIgnoreCase)) return false;
                    if (name.Contains("Submitted_Form", StringComparison.OrdinalIgnoreCase)) return false;
                    if (name.Contains("Section49", StringComparison.OrdinalIgnoreCase)) return false;
                    if (name.Contains("Section51", StringComparison.OrdinalIgnoreCase)) return false;
                    if (name.Contains("Section53", StringComparison.OrdinalIgnoreCase)) return false;

                    return true;
                })
                .Select(f =>
                {
                    var info = new FileInfo(f);

                    return new SupportingDocumentViewModel
                    {
                        FileName = info.Name,
                        SizeBytes = info.Length,
                        UploadedDate = info.CreationTime,
                        DownloadUrl =
                            $"/supporting-documents/download" +
                            $"?referenceNo={Uri.EscapeDataString(referenceNo)}" +
                            $"&rollSource={Uri.EscapeDataString(rollSource ?? "")}" +
                            $"&fileName={Uri.EscapeDataString(info.Name)}"
                    };
                })
                .OrderByDescending(x => x.UploadedDate)
                .ToList();

            return Task.FromResult(files);
        }

        public async Task<(bool Success, string? Error)> AddDocumentsAsync(
            string referenceNo,
            string? rollSource,
            List<IFormFile> files,
            string? uploadedBy)
        {
            if (string.IsNullOrWhiteSpace(referenceNo))
                return (false, "Reference number is missing.");

            if (files == null || files.Count == 0)
                return (false, "Please select at least one document.");

            var existing = await GetDocumentsAsync(referenceNo, rollSource);

            if (existing.Count + files.Count > 10)
            {
                return (false, $"Only 10 supporting documents are allowed. You already have {existing.Count} uploaded.");
            }

            var folder = GetReferenceFolder(referenceNo, rollSource);
            Directory.CreateDirectory(folder);

            foreach (var file in files)
            {
                if (file.Length <= 0)
                    continue;

                if (file.Length > 10 * 1024 * 1024)
                    return (false, $"{file.FileName} is bigger than 10MB.");

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!AllowedExtensions.Contains(ext))
                    return (false, $"{file.FileName} is not allowed. Only PDF, JPG, JPEG, PNG, HEIC are allowed.");

                var originalName = Path.GetFileNameWithoutExtension(file.FileName);
                var safeOriginal = MakeSafeFileName(originalName);

                var savedName = $"{safeOriginal}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                var path = Path.Combine(folder, savedName);

                await using var stream = new FileStream(path, FileMode.CreateNew);
                await file.CopyToAsync(stream);
            }

            return (true, null);
        }

        private string GetReferenceFolder(string referenceNo, string? rollSource)
        {
            var root = _config["FileStorage:EvidenceRoot"];

            if (string.IsNullOrWhiteSpace(root))
                root = Path.Combine(Directory.GetCurrentDirectory(), "Evidence");

            var safeRef = MakeSafeFileName(referenceNo);
            var safeRoll = MakeSafeFileName(rollSource ?? "General");

            return Path.Combine(root, safeRoll, safeRef);
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            return value.Trim();
        }
    }
}