using V2_Genesis.Models.Objections;

namespace V2_Genesis.Models.ViewModels.Objections
{
    public class CheckPropertyViewModel
    {
        public List<CheckPropertyResult> Items { get; set; } = new();
        public string SourceTable { get; set; } = string.Empty;
        public string RollSource { get; set; } = string.Empty;
        public string AppealStatus { get; set; } = "False";
        public string ControllerName { get; set; } = string.Empty;

        public bool IsAppeal => AppealStatus == "True";
        public bool IsLis => SourceTable == "LIS";
        public bool IsOmission { get; set; }

        public CheckPropertyResult? Main => Items.FirstOrDefault();
    }
}
