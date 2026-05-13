namespace V2_Genesis.Models.Results.Atrributes
{
    public class AttributeDocumentSaveResult
    {
        public string AttrFolderPath { get; set; } = string.Empty;

        public string PdfFileName { get; set; } = string.Empty;

        public string PdfFullPath { get; set; } = string.Empty;

        public string? RepLetterFileName { get; set; }

        public string? Files1 { get; set; }
        public string? Files2 { get; set; }
        public string? Files3 { get; set; }
        public string? Files4 { get; set; }
        public string? Files5 { get; set; }
        public string? Files6 { get; set; }
        public string? Files7 { get; set; }
        public string? Files8 { get; set; }
        public string? Files9 { get; set; }
        public string? Files10 { get; set; }

        public int EvidenceCount { get; set; }

        public string AcknowledgementFileName { get; set; } = string.Empty;

        public string AcknowledgementFullPath { get; set; } = string.Empty;
    }
}
