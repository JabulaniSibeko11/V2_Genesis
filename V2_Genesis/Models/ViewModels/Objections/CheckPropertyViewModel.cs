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

        public bool IsAppeal { get; set; }
        public bool IsLis => SourceTable == "LIS";
        public bool IsOmission { get; set; }
        public string? PropertyFrom { get; set; }
        public CheckPropertyResult? Main => Items.FirstOrDefault();

       
        public string? OmittedTownName { get; set; }
        public string? OmittedPropertyDesc { get; set; }
        public string? OmittedAddress { get; set; }
        public string? OmittedStand { get; set; }
        public string? OmittedScheme { get; set; }
        public string? OmittedUnit { get; set; }
    }
}
