using V2_Genesis.Models.ViewModels.Attributes;

namespace V2_Genesis.Models.ViewModels.Attributes;

public class AttributeAcknowledgementViewModel
{
    public long AttrId { get; set; }

    public string? AttrNo { get; set; }

    public string? Pin { get; set; }

    public string? PropertyDesc { get; set; }

    public string? FormType { get; set; }

    public string? Status { get; set; }

    public DateTime? SubmissionDateTime { get; set; }

    public DateTime? EvidenceDeadline { get; set; }

    public string? AcknowledgementFileName { get; set; }

    public string? AcknowledgementPath { get; set; }

    public string? SubmittedByName { get; set; }

    public int EvidenceCount { get; set; }

    // Full submitted form data for the HTML acknowledgement display
    public AttributeSubmissionViewModel? Submission { get; set; }
}