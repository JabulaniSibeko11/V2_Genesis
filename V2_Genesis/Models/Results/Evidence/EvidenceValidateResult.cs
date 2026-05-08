namespace V2_Genesis.Models.Results.Evidence
{
    public class EvidenceValidateResult
    {
        public bool IsValid { get; set; }
        public bool IsWithin48hrs { get; set; }
        public string? Error { get; set; }
        public int CurrentCount { get; set; }
        public bool IsAppeal { get; set; }

        public static EvidenceValidateResult Fail(string error)
            => new() { IsValid = false, Error = error };

        public static EvidenceValidateResult Expired()
            => new()
            {
                IsValid = false,
                IsWithin48hrs = false,
                Error = "The 48-hour upload window for this submission has closed."
            };

        public static EvidenceValidateResult Ok(int count, bool isAppeal)
            => new()
            {
                IsValid = true,
                IsWithin48hrs = true,
                CurrentCount = count,
                IsAppeal = isAppeal
            };
    }
}
