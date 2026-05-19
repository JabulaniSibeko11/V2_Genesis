namespace V2_Genesis.Models.Results.Section78
{
    public class Section78SubmitResult
    {
        public string QueryRef { get; set; } = "";
        public long QueryId { get; set; }
        public string? RandomPin { get; set; }
        public bool IsReview { get; set; }
        public bool IsMulti { get; set; }
        public int FileCount { get; set; }
        public string?[] Files { get; set; } = new string?[10];
        public Obj_Section6Model? Section6 { get; set; }
    }
}
