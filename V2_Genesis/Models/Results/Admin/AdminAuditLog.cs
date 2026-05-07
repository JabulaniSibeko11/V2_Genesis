
    namespace V2_Genesis.Models.Admin;

    public class AdminAuditLog
    {
        public int Id { get; set; }
        public string AdminEmail { get; set; } = string.Empty;
        public string? SapNumber { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? RollSource { get; set; }
        public string? SearchValue { get; set; }
        public string? EntityRef { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>Predefined action names — keeps audit logs consistent.</summary>
    public static class AuditActions
    {
        public const string Search = "Search";
        public const string SearchAppeal = "SearchAppeal";
        public const string ViewForm = "ViewForm";
        public const string ViewDashboard = "ViewDashboard";
        public const string DownloadAck = "DownloadAcknowledgement";
        public const string DownloadDecision = "DownloadDecision";
        public const string AddEvidence = "AddEvidence";
        public const string Withdraw = "Withdraw";
        public const string Appeal = "Appeal";
        public const string Export = "Export";
    }

