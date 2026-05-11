using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Models.Results.Atrributes;
using V2_Genesis.Services.Attributes;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Models.ViewModels.Dashboard
{
    public class AdminDashboardViewModel
    {
        // ── Identity ──────────────────────────────────────────────────────
        public string AdminEmail { get; set; } = string.Empty;
        public string SapNumber { get; set; } = string.Empty;

        // ── Rolls + client-style roll data (same SPs as client) ───────────
        public List<GvList> Rolls { get; set; } = new();
        public Dictionary<string, RollData> RollData { get; set; } = new();
        public Dictionary<string, RollDateEntry> RollDates { get; set; } = new();

        // ── Admin search state ────────────────────────────────────────────
        public string? SearchValue { get; set; }
        public string? FilterRoll { get; set; }
        public string? FilterStatus { get; set; }
        public bool HasSearch => !string.IsNullOrWhiteSpace(SearchValue);
        // Add to existing properties:
        public AttributesDashboardData AttributesData { get; set; } = new();

        // ── Admin-only: search results ────────────────────────────────────
        public Dictionary<string, List<AdminObjectionResult>> SearchObjections { get; set; } = new();
        public Dictionary<string, List<AdminAppealResult>> SearchAppeals { get; set; } = new();

        // ── Helpers ───────────────────────────────────────────────────────
        public RollData DataFor(string rollSource) =>
            RollData.TryGetValue(rollSource, out var d) ? d : new RollData();

        public RollDateEntry? DatesFor(string rollSource) =>
            RollDates.TryGetValue(rollSource, out var d) ? d : null;

        public List<AdminObjectionResult> ObjectionsFor(string rollSource) =>
            SearchObjections.TryGetValue(rollSource, out var o) ? o : new();

        public List<AdminAppealResult> AppealsFor(string rollSource) =>
            SearchAppeals.TryGetValue(rollSource, out var a) ? a : new();
        // New — Attributes linked properties
     
        public List<AttrLinkedPropertyResult> AttributesLinked { get; set; } = new();
        string PeriodStatus(RollDateEntry? d)
        {
            if (d is null) return "unknown";
            var now = DateTime.Now;
            if (now < d.OpenDate) return "upcoming";
            if (now <= d.VisibleUntil) return "active";
            return "closed";
        }

        public string GetPeriodStatus(string rollSource) =>
            PeriodStatus(DatesFor(rollSource));
    }
}
