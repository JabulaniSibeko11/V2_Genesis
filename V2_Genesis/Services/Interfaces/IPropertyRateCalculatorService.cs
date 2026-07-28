using V2_Genesis.Models.Rates;

namespace V2_Genesis.Services.Interfaces
{
    public interface IPropertyRateCalculatorService
    {
        Task<IReadOnlyCollection<PropertyRateTariff>>
            GetActiveTariffsAsync(
                CancellationToken cancellationToken = default);

        Task<PossibleRateCalculationResult>
            CalculateAsync(
                PossibleRateCalculationRequest request,
                CancellationToken cancellationToken = default);
    }
}
