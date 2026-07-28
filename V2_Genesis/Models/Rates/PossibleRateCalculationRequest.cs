namespace V2_Genesis.Models.Rates
{
    public sealed class PossibleRateCalculationRequest
    {
        public decimal MarketValue { get; init; }

        public string CategoryCode { get; init; } = string.Empty;

        public int? FinancialYearId { get; init; }

        public bool ApplyResidentialExclusion { get; init; } = true;
    }
}
