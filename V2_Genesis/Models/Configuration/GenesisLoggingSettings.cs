namespace V2_Genesis.Models.Configuration
{
    public sealed class GenesisLoggingSettings
    {
        public bool Enabled { get; set; } = true;
        public string RootPath { get; set; } = @"C:\Genesis Log";
        public int RetainedYears { get; set; } = 5;
        public int FileSizeLimitMB { get; set; } = 20;
        public bool LogRequests { get; set; } = true;
        public bool LogControllerActions { get; set; } = true;
        public bool AuditEnabled { get; set; } = true;
    }
}
