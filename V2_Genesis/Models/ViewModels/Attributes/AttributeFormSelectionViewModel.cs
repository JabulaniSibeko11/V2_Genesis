namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributeSelectViewModel
    {
        // ── Passed from dashboard link ─────────────────────────────
        // /attributes/select-form?unitKey=@item.IDProperty
        public string UnitKey { get; set; } = string.Empty;

        // ── Property details (populated by SelectForm GET action) ──
        public string? PropertyDesc { get; set; }
        public string? CatDesc { get; set; }   // Category
        public string? TownNameDesc { get; set; }   // Town / Township
        public string? LisStreetAddress { get; set; }   // Street address
        public string? MarketValue { get; set; }
        public string? RateableArea { get; set; }   // Extent / GBA
        public int Erf { get; set; }
        public string? Ptn { get; set; }
        public string? Re { get; set; }
        public string? SchemeName { get; set; }
        public string? SchemeNumber { get; set; }
        public string? SchemeYear { get; set; }
        public int UnitNo { get; set; }
        public string? LeaseDesc { get; set; }
        public string? OwnerName { get; set; }
        public string? ValuationDate { get; set; }
        public string? Zoning { get; set; }
        public string? Reason { get; set; }

        // ── Form type ──────────────────────────────────────────────
        // Suggested: shown as a hint to the client (not enforced)
        public string? SuggestedFormType { get; set; }

        // Selected: the radio button value the client picks
        public string? SelectedFormType { get; set; }

        // ── Submitter type ─────────────────────────────────────────
        // "Owner" or "Representative" — chosen on this page
        public string DeclarationType { get; set; } = string.Empty;
    }
}
