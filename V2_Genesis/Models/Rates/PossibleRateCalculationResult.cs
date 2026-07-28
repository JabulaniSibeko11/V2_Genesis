namespace V2_Genesis.Models.Rates
{
    public sealed class PossibleRateCalculationResult
    {
        public int FinancialYearId { get; init; }

        public string FinancialYear { get; init; } = string.Empty;

        public string CategoryCode { get; init; } = string.Empty;

        public string CategoryName { get; init; } = string.Empty;

        public string? Ratio { get; init; }

        public decimal MarketValue { get; init; }

        public decimal ExclusionAmount { get; init; }

        public decimal RateableValue { get; init; }

        public decimal AnnualTariff { get; init; }

        public decimal EstimatedAnnualRates { get; init; }

        public decimal EstimatedMonthlyRates { get; init; }

        public bool IsZeroRated { get; init; }

        public bool IsMultipurpose { get; init; }

        public string Disclaimer { get; init; } = string.Empty;

        public IReadOnlyCollection<string> Warnings { get; init; }
            = Array.Empty<string>();
    }
}
