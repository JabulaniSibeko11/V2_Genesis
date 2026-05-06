namespace V2_Genesis.Services
{
    public class SessionSettings
    {
        public int TimeoutMinutes { get; set; } = 30;
        public string AdminPendingKey { get; set; } = "AdminLoginPending";
        public string AdminEmailKey { get; set; } = "AdminPendingEmail";
    }
}
