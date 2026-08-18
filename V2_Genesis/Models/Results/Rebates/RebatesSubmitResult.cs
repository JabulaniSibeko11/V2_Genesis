namespace V2_Genesis.Models.Results.Rebates
{
    public class RebatesSubmitResult
    {
        public string RebateNo { get; set; } = "";
        public int RebateId { get; set; }
        public string? status { get; set; }
        public int FileCount { get; set; }
        public string? SubmittedAt { get; set; }
        public string?[] files { get; set; } = new string[10];

        // Acknowledgement display fields. These are populated from the
        // already-captured rebate form and do not change the database schema.
        public string? RebateType { get; set; }
        public string? ApplicantName { get; set; }
        public string? AccountNumber { get; set; }
        public string? Email { get; set; }
        public string? PropertyAddress { get; set; }
    }
}
