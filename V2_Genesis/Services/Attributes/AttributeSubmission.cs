namespace V2_Genesis.Services.Attributes
{
    public class AttributeSubmission
    {
        public int Id { get; set; }
        public string? SubmissionRef { get; set; }
        public string? PropertyDesc { get; set; }
        public string? FormType { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? Status { get; set; }  // Pending / Under Review / Approved / Rejected

        public string? RevisionReason { get; set; }

        public bool? RevisionRequired { get; set; }

       

        public string? RevisedBy { get; set; }

        public DateTime? RevisedDateTime { get; set; }

        public string? RevisionComment { get; set; }

        public DateTime? EditDeadline { get; set; }

        public int? EditTimeLeftSeconds { get; set; }
        public bool IsEditable =>
            Status is not ("Approved" or "Rejected" or "Withdrawn" or "Extracted to OVVIO")
            && SubmittedAt.AddHours(48) > DateTime.Now;

        public TimeSpan? EditTimeLeft => IsEditable
            ? SubmittedAt.AddHours(48) - DateTime.Now
            : null;
    }
}
