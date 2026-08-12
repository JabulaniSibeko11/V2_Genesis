namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class UnlinkViewModel
    {
        public long Id { get; set; }
        public string IDProperty { get; set; } = string.Empty;
        public string? PropertyDesc { get; set; }
        public string? TownNameDesc { get; set; }
        public string? CatDesc { get; set; }
        public string? MarketValue { get; set; }
        public string? Address { get; set; }
        public string ReturnUrl { get; set; } = "/Dashboard?openRoll=attributes";
    }
}
