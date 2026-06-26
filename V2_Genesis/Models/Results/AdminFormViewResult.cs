using V2_Genesis.Models.ViewModels;

namespace V2_Genesis.Models.Results
{
    public class AdminFormViewResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        public string ReferenceNo { get; set; } = "";
        public string RollSource { get; set; } = "";
        public string SourceTable { get; set; } = "";
        public string PropertyType { get; set; } = "";
        public string PartialViewName { get; set; } = "";

        public bool IsAppeal { get; set; }
        public bool IsQuery { get; set; }

        public List<ObjectionTB> Items { get; set; } = new();

        public Section78FormViewModel? Section78 { get; set; }
    }
}
