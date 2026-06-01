namespace V2_Genesis.Models.Results.Rebates
{
    public class RebatesSubmitResult
    {
        public string RebateNo { get; set; } = "";
         public int RebateId { get; set; }
        public string?status { get; set; }
        public int FileCount { get; set; }
         
        public string? SubmittedAt { get; set; }
        public string?[] files { get; set; } =new string[10];
    }
}
