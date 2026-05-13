namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributeAcknowledgementVm
    {
        public long AttrId { get; set; }

        public string? AttrNo { get; set; }

        public string? PropertyDescription { get; set; }

        public string? PropertyCategory { get; set; }

        public string? PhysicalAddress { get; set; }

        public string? MarketValue { get; set; }

        public string? Extent { get; set; }

        public string? OwnerName { get; set; }

        public string? Pin { get; set; }

        public DateTime SubmissionDate { get; set; }

        public DateTime? EvidenceDeadline { get; set; }

        public int EvidenceCount { get; set; }

        public string? AcknowledgementFileName { get; set; }

        public string? AcknowledgementPath { get; set; }

        public List<string> UploadedDocuments { get; set; } = new();
    }
}
