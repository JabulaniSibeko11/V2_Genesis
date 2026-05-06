namespace V2_Genesis.Models.ViewModels.Dashboard
{
    public class AdminDashboardViewModel
    {
        public string AdminName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalSubmissions { get; set; }
        public int PendingReview { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int TotalUsers { get; set; }
        public int TotalProperties { get; set; }
        public V2_Genesis.Services.AnnouncementResult Announcement { get; set; } = new();
    }
}
