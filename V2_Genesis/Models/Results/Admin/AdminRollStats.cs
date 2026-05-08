namespace V2_Genesis.Models.Results.Admin
{
    public class AdminRollStats
    {
        public string RollSource { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Lodging { get; set; }
        public int Closed { get; set; }
        public int TotalAppeals { get; set; }
    }
}
