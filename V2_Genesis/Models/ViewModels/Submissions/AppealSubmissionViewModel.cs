namespace V2_Genesis.Models.ViewModels.Submissions
{
    public sealed class AppealSubmissionViewModel
    {
        public string AppealNumber { get; set; } = string.Empty;

        public string ObjectionNumber { get; set; } = string.Empty;

        public string AppealType { get; set; } = string.Empty;

        public string AppealStatus { get; set; } = string.Empty;

        public DateTime? AppealDate { get; set; }

        public string ObjectionOutcome { get; set; } = string.Empty;

        public string AppellantRequest { get; set; } = string.Empty;

        public string GroundsOfAppeal { get; set; } = string.Empty;

        public string DecisionBeingAppealed { get; set; } = string.Empty;

        public string RequestedDecision { get; set; } = string.Empty;

        public string AppealMotivation { get; set; } = string.Empty;

        public string BoardStatus { get; set; } = string.Empty;

        public DateTime? HearingDate { get; set; }

        public string AppealBoard { get; set; } = string.Empty;

        public string Chairperson { get; set; } = string.Empty;

        public string BoardMember1 { get; set; } = string.Empty;

        public string BoardMember2 { get; set; } = string.Empty;

        public string ExternalValuer { get; set; } = string.Empty;

        public string Decision { get; set; } = string.Empty;

        public string DecisionComment { get; set; } = string.Empty;

        public DateTime? DecisionDate { get; set; }

        public bool HasHearingInformation =>
            HearingDate.HasValue
            || !string.IsNullOrWhiteSpace(AppealBoard)
            || !string.IsNullOrWhiteSpace(Chairperson)
            || !string.IsNullOrWhiteSpace(BoardMember1)
            || !string.IsNullOrWhiteSpace(BoardMember2)
            || !string.IsNullOrWhiteSpace(ExternalValuer);

        public bool HasDecision =>
            !string.IsNullOrWhiteSpace(Decision)
            || !string.IsNullOrWhiteSpace(DecisionComment)
            || DecisionDate.HasValue;
    }
}
