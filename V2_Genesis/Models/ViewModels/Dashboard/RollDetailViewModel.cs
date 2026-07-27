using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Models.ViewModels.Dashboard
{
    public class RollDetailViewModel
    {
        public GvList Roll { get; set; } = new();
        public RollData Data { get; set; } = new();
        public RollDateEntry? Dates { get; set; }
        public string PeriodStatus { get; set; } = "unknown";
        public bool CanLodgeObjectionForRoll { get; set; }
    }
}
