namespace V2_Genesis.Helpers
{
    public static class DashboardNoticeStatusHelper
    {
        public static string Normalise(string? status) => status?.Trim() ?? string.Empty;

        public static bool Is(string? status, params string[] expected)
        {
            var value = Normalise(status);
            return expected.Any(x =>
                string.Equals(value, x, StringComparison.OrdinalIgnoreCase));
        }

        public static bool CanDownloadSection51(string? status) =>
            Is(status, "Obj-Section51");

        public static bool CanDownloadSection53(string? status) =>
            Is(status, "Notice-Sent", "Appeal-Closed");

        public static bool CanDownloadDearOwner(string? status) =>
            Is(status, "Notice-Sent-Dear-Johnny");

        public static bool CanDownloadInvalid(string? status) =>
            Is(status, "Notice-Sent-Invalid-Objection", "Notice-Sent-Invalid-Omission");

        public static bool IsInvalidOmission(string? status) =>
            Is(status, "Notice-Sent-Invalid-Omission");

        public static bool CanDownloadAppealOutcome(string? status) =>
            Is(status, "App-Finalized", "App-Finalised");

        public static bool CanDownloadSection78Outcome(string? status) =>
            Is(status, "Query-Finalized", "Review-Finalized", "Notice-Sent");
    }

}
