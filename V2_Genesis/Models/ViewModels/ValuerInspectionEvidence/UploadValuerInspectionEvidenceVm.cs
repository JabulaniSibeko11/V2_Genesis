namespace V2_Genesis.Models.ViewModels.ValuerInspectionEvidence
{
    public class UploadValuerInspectionEvidenceVm
    {
        public long InspectionRequestId { get; set; }

        public string SapNumber { get; set; } = string.Empty;

        public string? AttrNo { get; set; }

        public string? PropertyDescription { get; set; }

        public string? InspectionAddress { get; set; }

        public DateTime? ConfirmedDateTime { get; set; }

        public string? ValuerName { get; set; }

        public string? InspectionOutcome { get; set; }

        public string? InspectionOutcomeComment { get; set; }

        public List<IFormFile> EvidenceFiles { get; set; } = new();
    }
}
