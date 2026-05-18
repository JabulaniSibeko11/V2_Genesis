

namespace V2_Genesis.Models.Results
{

    public class PropertyDetailViewModel
    {
        public List<PropertyDetailResult> Items { get; set; } = new();
        public GvList Roll { get; set; } = null!;
        public DateTime? OpenDate { get; set; }
        public DateTime? VisibleUntil { get; set; }

        // ── Convenience accessors to first item (main record) ─────────────
        public PropertyDetailResult? Main => Items.FirstOrDefault();

        // ── Decides which action buttons to show ──────────────────────────
        public bool IsWithinObjectionPeriod =>
            DateTime.Now > OpenDate && DateTime.Now < VisibleUntil;

        public bool IsAttributes { get; set; }
    }
}