using V2_Genesis.Services;

namespace V2_Genesis.Models.ViewModels.Home
{
    public class LandingViewModel
    {
        public AnnouncementResult Announcement { get; set; } = new();
        public DisclaimerSettings Disclaimer { get; set; } = new();
        public ValuationRollSettings Roll { get; set; } = new();
        public bool ShowDisclaimer { get; set; }
    }

}
