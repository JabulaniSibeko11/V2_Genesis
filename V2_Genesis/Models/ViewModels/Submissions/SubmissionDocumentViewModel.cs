namespace V2_Genesis.Models.ViewModels.Submissions
{
    public sealed class SubmissionDocumentViewModel
    {
        public long Id { get; set; }

        public string ReferenceNumber { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string StoredFileName { get; set; } = string.Empty;

        public string FileExtension { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public string DocumentType { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string DownloadUrl { get; set; } = string.Empty;

        public string PreviewUrl { get; set; } = string.Empty;

        public DateTime? UploadedAt { get; set; }

        public string UploadedBy { get; set; } = string.Empty;

        public bool Exists { get; set; }

        public bool CanPreview =>
            FileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            || FileExtension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || FileExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || FileExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);

        public string DisplayFileSize
        {
            get
            {
                if (FileSizeBytes <= 0)
                    return string.Empty;

                if (FileSizeBytes < 1024)
                    return $"{FileSizeBytes} B";

                if (FileSizeBytes < 1024 * 1024)
                    return $"{FileSizeBytes / 1024d:0.0} KB";

                return $"{FileSizeBytes / 1024d / 1024d:0.0} MB";
            }
        }
    }
}