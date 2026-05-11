using System.ComponentModel.DataAnnotations;

namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributePropertyDetailsVm
    {
        [Display(Name = "H Area")]
        public string? HArea { get; set; }

        [Display(Name = "Data Controller")]
        public string? DataController { get; set; }

        [Display(Name = "Collection Block")]
        public string? CollectionBlock { get; set; }

        [Display(Name = "Data Collector")]
        public string? DataCollector { get; set; }

        [Display(Name = "SG Number")]
        public string? SGNumber { get; set; }

        public string? Centroid { get; set; }

        public string? Erf { get; set; }

        public string? Extent { get; set; }

        [Display(Name = "Sectional Title")]
        public string? SectionalTitle { get; set; }

        [Display(Name = "Land Use Financials")]
        public string? LandUseFinancials { get; set; }

        public string? Municipality { get; set; }

        public string? Ward { get; set; }

        public string? Township { get; set; }

        public string? Zoning { get; set; }

        public string? Sources { get; set; }

        public string? Address { get; set; }

        public string? PropertyDesc { get; set; }

        public string? PremiseId { get; set; }

        public string? UnitKey { get; set; }

        public string? PropertyId { get; set; }

        public string? ValuationKey { get; set; }

        public string? Sector { get; set; }

        public string? RollType { get; set; }

        public string? RollDescription { get; set; }
    }
}
