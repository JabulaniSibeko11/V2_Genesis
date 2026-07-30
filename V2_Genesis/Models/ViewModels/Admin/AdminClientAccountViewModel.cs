using V2_Genesis.Models;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.Results.Atrributes;
using V2_Genesis.Services.Attributes;

namespace V2_Genesis.Models.ViewModels.Admin;

public sealed class AdminClientAccountViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? AccountCreatedAt { get; set; }
    public bool EmailConfirmed { get; set; }

    public List<AdminClientPropertyViewModel> Properties { get; set; } = new();
    public List<AdminClientSubmissionViewModel> Submissions { get; set; } = new();
    public List<Rebate_View_Model> Rebates { get; set; } = new();

    public int PropertyCount => Properties.Count;
    public int SubmissionCount => Submissions.Count;
    public int ObjectionCount => Submissions.Count(x => x.SubmissionType == "Objection");
    public int AppealCount => Submissions.Count(x => x.SubmissionType == "Appeal");
    public int QueryCount => Submissions.Count(x => x.SubmissionType == "Query");
    public int ReviewCount => Submissions.Count(x => x.SubmissionType == "Review");
    public int AttributeCount => Submissions.Count(x => x.SubmissionType == "Attribute");

    public List<AdminClientSubmissionViewModel> Objections =>
        Submissions.Where(x => x.SubmissionType == "Objection").ToList();

    public List<AdminClientSubmissionViewModel> Appeals =>
        Submissions.Where(x => x.SubmissionType == "Appeal").ToList();

    public List<AdminClientSubmissionViewModel> Queries =>
        Submissions.Where(x => x.SubmissionType == "Query").ToList();

    public List<AdminClientSubmissionViewModel> Reviews =>
        Submissions.Where(x => x.SubmissionType == "Review").ToList();

    public List<AdminClientSubmissionViewModel> Attributes =>
        Submissions.Where(x => x.SubmissionType == "Attribute").ToList();
}

public sealed class AdminClientPropertyViewModel
{
    public string PropertyKey { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string Town { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MarketValue { get; set; } = string.Empty;
    public string PremiseId { get; set; } = string.Empty;
    public string UnitKey { get; set; } = string.Empty;
    public string ValuationKey { get; set; } = string.Empty;
    public string RollSource { get; set; } = string.Empty;
    public string RollName { get; set; } = string.Empty;
    public string PropertyFrom { get; set; } = string.Empty;
    public bool IsLinked { get; set; }

    public List<AdminClientSubmissionViewModel> Submissions { get; set; } = new();

    public int SubmissionCount => Submissions.Count;
}

public sealed class AdminClientSubmissionViewModel
{
    public string SubmissionType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RollSource { get; set; } = string.Empty;
    public string RollName { get; set; } = string.Empty;

    public string PropertyKey { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string Town { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MarketValue { get; set; } = string.Empty;
    public string UnitKey { get; set; } = string.Empty;
    public string ValuationKey { get; set; } = string.Empty;

    public DateTime? SubmittedAt { get; set; }

    public long? InspectionRequestId { get; set; }
    public string InspectionAppointmentRef { get; set; } = string.Empty;
    public string InspectionStatus { get; set; } = string.Empty;
    public DateTime? InspectionDate { get; set; }
    public string InspectionTimeSlot { get; set; } = string.Empty;
    public string InspectionValuerName { get; set; } = string.Empty;

    public bool HasInspection =>
        InspectionRequestId.HasValue
        || !string.IsNullOrWhiteSpace(InspectionAppointmentRef)
        || !string.IsNullOrWhiteSpace(InspectionStatus);
}
