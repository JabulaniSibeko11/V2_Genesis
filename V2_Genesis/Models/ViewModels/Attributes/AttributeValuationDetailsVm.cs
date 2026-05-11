using System.ComponentModel.DataAnnotations;

namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributeValuationDetailsVm
    {
        [Display(Name = "Valuation Category on Roll")]
        public string? ValuationCategoryOnRoll { get; set; }

        [Display(Name = "Actual Use")]
        public string? ActualUse { get; set; }

        [Display(Name = "Mixed Use")]
        public bool IsMixedUse { get; set; }

        [Display(Name = "If mixed use, specify alternate usages")]
        public string? AlternateUsages { get; set; }

        [Display(Name = "Owners Title Deeds")]
        public string? OwnersTitleDeeds { get; set; }

        [Display(Name = "Owners Financials")]
        public string? OwnersFinancials { get; set; }
    }
}
