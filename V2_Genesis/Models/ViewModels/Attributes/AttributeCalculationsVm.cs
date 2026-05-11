namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class AttributeCalculationsVm
    {
        public string? CalcUpdateTla { get; set; }

        public decimal? Tla { get; set; }

        public string? CalcUpdateWgba { get; set; }

        public decimal? AdjustedWgba { get; set; }

        public decimal? TotalValueNonRes { get; set; }

        public decimal? TotalValueUnutilisedLand { get; set; }

        public decimal? DRCFinalValue { get; set; }

        public string? CalculationStatus { get; set; }
    }
}
