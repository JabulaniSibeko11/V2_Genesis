namespace V2_Genesis.Models.Results
{
    public class SubmittedFormPdfResult
    {
        public string ReferenceNumber { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
        public string SubmissionType { get; set; } = string.Empty;
    }
}
