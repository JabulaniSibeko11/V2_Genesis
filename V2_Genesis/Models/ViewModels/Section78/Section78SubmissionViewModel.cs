namespace V2_Genesis.Models.ViewModels.Section78
{
    public class Section78SubmissionViewModel
    {
        // ── From CheckProperty → Section78Query route ─────────────────
        public string? QueryType { get; set; }   // "Query" or "Review"
        public string? Direct { get; set; }   // "" or "Multi"
        public string? PropDesc { get; set; }
        public string? Address { get; set; }
        public string? Category { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? ObjectorType { get; set; }   // "Owner" or "Representative"

        // ── Form fields (to be extended when form is built) ───────────
        public string? ClientComment { get; set; }
        public string? QueryReason { get; set; }
        public string? ProposedValue { get; set; }
        public string? ProposedCategory
        {
            get; set;
        }
    }
}