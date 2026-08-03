using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Models.Rebates;

namespace V2_Genesis.Models.ViewModels.Submissions
{
    public sealed class SubmissionViewModel
    {
        public string SubmissionType { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RollSource { get; set; } = string.Empty;
        public string RollDisplayName { get; set; } = string.Empty;
        public string FormType { get; set; } = string.Empty;
        public string PropertyDescription { get; set; } = string.Empty;
        public string PropertyKey { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }

        public SubmissionPropertyViewModel Property { get; set; } = new();
        public SubmissionApplicantViewModel Applicant { get; set; } = new();
        public SubmissionValuationViewModel CurrentValuation { get; set; } = new();
        public SubmissionValuationViewModel RequestedValuation { get; set; } = new();
        public SubmissionReasonViewModel Reasons { get; set; } = new();
        public AppealSubmissionViewModel? Appeal { get; set; }

        // Complete submitted Attributes form reconstructed from the
        // Attributes database tables.
        public AttributeSubmissionViewModel? Attribute { get; set; }

        // Complete rebate application reconstructed from Rebate_Info and
        // its eleven submitted form-section tables.
        public RebateFormBinding? Rebate { get; set; }

        public List<MultiPurposeLineViewModel> MultiPurposeLines { get; set; } = new();
        public List<SubmissionDocumentViewModel> Documents { get; set; } = new();
        public List<SubmissionSectionViewModel> Sections { get; set; } = new();

        // New dynamic form composition.
        public List<SubmissionFormSectionViewModel> FormSections { get; set; } = new();

        public bool IsObjection =>
            SubmissionType.Equals("Objection", StringComparison.OrdinalIgnoreCase);

        public bool IsAppeal =>
            SubmissionType.Equals("Appeal", StringComparison.OrdinalIgnoreCase);

        public bool IsQuery =>
            SubmissionType.Equals("Query", StringComparison.OrdinalIgnoreCase);

        public bool IsReview =>
            SubmissionType.Equals("Review", StringComparison.OrdinalIgnoreCase);

        public bool IsAttribute =>
            SubmissionType.Equals("Attribute", StringComparison.OrdinalIgnoreCase);

        public bool IsRebate =>
            SubmissionType.Equals("Rebate", StringComparison.OrdinalIgnoreCase);

        public bool IsSection78 => IsQuery || IsReview;

        public bool IsMulti =>
            FormType.Equals("Multi", StringComparison.OrdinalIgnoreCase)
            || FormType.Equals("D", StringComparison.OrdinalIgnoreCase)
            || Property.PropertyType.Equals("Multi", StringComparison.OrdinalIgnoreCase)
            || Property.PropertyType.Equals("Multipurpose", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class SubmissionSectionViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<SubmissionFieldViewModel> Fields { get; set; } = new();
    }

    public sealed class SubmissionFieldViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsLongText { get; set; }
    }

    public sealed class SubmissionViewResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public SubmissionViewModel? Submission { get; init; }

        public static SubmissionViewResult Ok(SubmissionViewModel model) =>
            new() { Success = true, Submission = model };

        public static SubmissionViewResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    public sealed class SubmissionPropertyViewModel
    {
        public string PropertyDescription { get; set; } = string.Empty;
        public string PropertyType { get; set; } = string.Empty;
        public string PremiseId { get; set; } = string.Empty;
        public string PropertyId { get; set; } = string.Empty;
        public string UnitKey { get; set; } = string.Empty;
        public string ValuationKey { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Township { get; set; } = string.Empty;
        public string Erf { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Extent { get; set; } = string.Empty;
        public string MarketValue { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
    }

    public sealed class SubmissionValuationViewModel
    {
        public string PropertyDescription { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Extent { get; set; } = string.Empty;
        public string MarketValue { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;

        public bool HasValues =>
            !string.IsNullOrWhiteSpace(PropertyDescription)
            || !string.IsNullOrWhiteSpace(Category)
            || !string.IsNullOrWhiteSpace(Address)
            || !string.IsNullOrWhiteSpace(Extent)
            || !string.IsNullOrWhiteSpace(MarketValue)
            || !string.IsNullOrWhiteSpace(Owner);
    }

    public sealed class MultiPurposeLineViewModel
    {
        public int LineNumber { get; set; }
        public string CurrentCategory { get; set; } = string.Empty;
        public string CurrentExtent { get; set; } = string.Empty;
        public string CurrentMarketValue { get; set; } = string.Empty;
        public string RequestedCategory { get; set; } = string.Empty;
        public string RequestedExtent { get; set; } = string.Empty;
        public string RequestedMarketValue { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        public bool HasValues =>
            !string.IsNullOrWhiteSpace(CurrentCategory)
            || !string.IsNullOrWhiteSpace(CurrentExtent)
            || !string.IsNullOrWhiteSpace(CurrentMarketValue)
            || !string.IsNullOrWhiteSpace(RequestedCategory)
            || !string.IsNullOrWhiteSpace(RequestedExtent)
            || !string.IsNullOrWhiteSpace(RequestedMarketValue)
            || !string.IsNullOrWhiteSpace(Remarks);
    }
}
