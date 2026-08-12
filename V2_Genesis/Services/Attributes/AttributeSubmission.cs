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
        private DateTime EffectiveEditDeadline =>
            EditDeadline ?? SubmittedAt.AddHours(48);

        public bool IsEditable
        {
            get
            {
                var statusKey = new string((Status ?? string.Empty)
                    .Where(char.IsLetterOrDigit)
                    .Select(char.ToUpperInvariant)
                    .ToArray());

                // Additional evidence is only editable while the submission
                // is genuinely in its evidence stage. Once AIVS has moved it
                // to the sector inbox/review/inspection workflow, Genesis
                // must show the submission as locked even if 48 hours have
                // not elapsed yet.
                var evidenceWindowStatus = statusKey is
                    "EVIDENCEOPEN" or
                    "SUBMITTED";

                return evidenceWindowStatus &&
                       EffectiveEditDeadline > DateTime.Now;
            }
        }

        public TimeSpan? EditTimeLeft => IsEditable
            ? EffectiveEditDeadline - DateTime.Now
            : null;
    }
}
