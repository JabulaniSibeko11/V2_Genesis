namespace V2_Genesis.Models.ViewModels.Submissions
{
    public sealed class SubmissionReasonViewModel
    {
        public string PrimaryReason { get; set; } = string.Empty;

        public string AdditionalReason { get; set; } = string.Empty;

        public string Motivation { get; set; } = string.Empty;

        public string RequestedOutcome { get; set; } = string.Empty;

        public string ValuationReason { get; set; } = string.Empty;

        public string PropertyDescriptionReason { get; set; } = string.Empty;

        public string CategoryReason { get; set; } = string.Empty;

        public string ExtentReason { get; set; } = string.Empty;

        public string MarketValueReason { get; set; } = string.Empty;

        public string OwnerReason { get; set; } = string.Empty;

        public string AddressReason { get; set; } = string.Empty;

        public string OtherReason { get; set; } = string.Empty;

        public bool HasReasons =>
            !string.IsNullOrWhiteSpace(PrimaryReason)
            || !string.IsNullOrWhiteSpace(AdditionalReason)
            || !string.IsNullOrWhiteSpace(Motivation)
            || !string.IsNullOrWhiteSpace(RequestedOutcome)
            || !string.IsNullOrWhiteSpace(ValuationReason)
            || !string.IsNullOrWhiteSpace(PropertyDescriptionReason)
            || !string.IsNullOrWhiteSpace(CategoryReason)
            || !string.IsNullOrWhiteSpace(ExtentReason)
            || !string.IsNullOrWhiteSpace(MarketValueReason)
            || !string.IsNullOrWhiteSpace(OwnerReason)
            || !string.IsNullOrWhiteSpace(AddressReason)
            || !string.IsNullOrWhiteSpace(OtherReason);
    }
}
