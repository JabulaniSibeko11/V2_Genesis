namespace V2_Genesis.Models.ViewModels.ValuerInspectionEvidence
{
    public class ValuerInspectionTodayVm
    {
        public string SapNumber { get; set; } = string.Empty;

        public string? ValuerName { get; set; }

        public List<ValuerInspectionItemVm> Inspections { get; set; } = new();
    }

    public class ValuerInspectionItemVm
    {
        public long InspectionRequestId { get; set; }

        // Your new view uses InspectionId, so expose it as an alias.
        public long InspectionId
        {
            get => InspectionRequestId;
            set => InspectionRequestId = value;
        }

        public long AttrId { get; set; }

        public string? AttrNo { get; set; }

        // Your new view uses PropertyRef.
        public string? PropertyRef
        {
            get => AttrNo;
            set => AttrNo = value;
        }

        public string? PropertyDescription { get; set; }

        public string? PremiseId { get; set; }

        public string? InspectionAddress { get; set; }

        // Your new view uses PropertyAddress.
        public string? PropertyAddress
        {
            get => !string.IsNullOrWhiteSpace(InspectionAddress)
                ? InspectionAddress
                : PropertyDescription;

            set => InspectionAddress = value;
        }

        public DateTime? ConfirmedDateTime { get; set; }

        // Your new view uses ScheduledTime as DateTime.
        public DateTime ScheduledTime
        {
            get => ConfirmedDateTime ?? DateTime.MinValue;
            set => ConfirmedDateTime = value;
        }

        public string? Status { get; set; }

        public string? ValuerName { get; set; }

        public string? EmailAddress { get; set; }

        public string? CellNumber { get; set; }

        public string? OwnerName { get; set; }

        public string? ContactNumber { get; set; }

        public string? Notes { get; set; }

        public string? MapUrl { get; set; }

        public bool EvidenceUploaded { get; set; }

        public int EvidenceCount { get; set; }
    }
}