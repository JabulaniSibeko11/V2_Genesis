namespace V2_Genesis.Models.ViewModels.ValuerInspectionEvidence
{
    public class UploadValuerInspectionEvidenceVm
    {
        public long InspectionRequestId { get; set; }

        public string SapNumber { get; set; } = string.Empty;

        public string? InspectionOutcome { get; set; }

        public string? InspectionOutcomeComment { get; set; }

        public List<IFormFile> EvidenceFiles { get; set; } = new();
    }
}
