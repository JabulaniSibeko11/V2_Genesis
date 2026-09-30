using V2_Genesis.Services;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Models.ViewModels.Home
{
    public class LandingViewModel
    {
        public AnnouncementResult Announcement { get; set; } = new();
        public DisclaimerSettings Disclaimer { get; set; } = new();
        public ValuationRollSettings Roll { get; set; } = new();
        public bool ShowDisclaimer { get; set; }

        /// Valuation rolls (GV / Supp1-4 / Query) for the public dashboard cards.
        public List<V2_Genesis.Models.GvList> Rolls { get; set; } = new();

        /// RollDates:{roll} from appsettings — drives OPEN / CLOSED and the
        /// inspection & objection period text on the public dashboard.
        public Dictionary<string, RollDateEntry> RollDates { get; set; } = new();
    }

}
