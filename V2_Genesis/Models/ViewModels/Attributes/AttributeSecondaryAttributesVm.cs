namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributeSecondaryAttributesVm
    {
        public int? Storeys { get; set; }

        public string? Security { get; set; }

        public string? Noise { get; set; }

        public string? Topography { get; set; }

        public string? Quality { get; set; }

        public string? Condition { get; set; }

        public bool? SwimmingPool { get; set; }

        public bool? TennisCourt { get; set; }

        // Residential ST
        public int? STCondition { get; set; }

        public int? STFloor { get; set; }
    }
}
