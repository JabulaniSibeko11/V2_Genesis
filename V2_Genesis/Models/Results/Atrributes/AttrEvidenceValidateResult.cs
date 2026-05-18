namespace V2_Genesis.Models.Results.Atrributes
{
    public class AttrEvidenceValidateResult
    {
        public bool IsValid { get; private set; }
        public bool IsExpired { get; private set; }
        public string? Error { get; private set; }
        public int CurrentCount { get; private set; }
        public int SlotsRemaining => 10 - CurrentCount;
        public string? AttrNo { get; private set; }
        public string? PropertyDesc { get; private set; }
        public string? RootFolder { get; private set; }
        public DateTime? ExpiryDate { get; private set; }

        public static AttrEvidenceValidateResult Ok(
            int currentCount, string attrNo, string? propertyDesc,
            string? rootFolder, DateTime? expiry) =>
            new()
            {
                IsValid = true,
                CurrentCount = currentCount,
                AttrNo = attrNo,
                PropertyDesc = propertyDesc,
                RootFolder = rootFolder,
                ExpiryDate = expiry
            };

        public static AttrEvidenceValidateResult Fail(string error) =>
            new() { IsValid = false, Error = error };

        public static AttrEvidenceValidateResult Expired() =>
            new()
            {
                IsValid = false,
                IsExpired = true,
                Error = "The 48-hour evidence upload window has closed for this submission."
            };
    }
}
