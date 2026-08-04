using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using V2_Genesis.Models.Rates;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public sealed class PropertyRateCalculatorService
    : IPropertyRateCalculatorService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PropertyRateCalculatorService> _logger;

    public PropertyRateCalculatorService(
        ApplicationDbContext db,
        ILogger<PropertyRateCalculatorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════
    // GET ACTIVE TARIFFS
    // Reads directly from the Objection database.
    // ════════════════════════════════════════════════════════════════
    public async Task<IReadOnlyCollection<PropertyRateTariff>>
        GetActiveTariffsAsync(
            CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateTime.Today;

            return await _db.PropertyRateTariffs
                .AsNoTracking()
                .Include(tariff => tariff.FinancialYear)
                .Where(tariff =>
                    tariff.IsActive &&
                    tariff.FinancialYear.IsActive &&
                    today >= tariff.EffectiveFrom.Date &&
                    today <= tariff.EffectiveTo.Date)
                .OrderBy(tariff => tariff.CategoryName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load active tariffs from the Objection database.");

            throw;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // CALCULATE POSSIBLE RATES
    // ════════════════════════════════════════════════════════════════
    public async Task<PossibleRateCalculationResult>
        CalculateAsync(
            PossibleRateCalculationRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var categoryCode =
            request.CategoryCode.Trim().ToUpperInvariant();

        var tariff = await GetTariffAsync(
            categoryCode,
            request.FinancialYearId,
            cancellationToken);

        if (tariff is null)
        {
            throw new InvalidOperationException(
                "No active tariff is configured for the selected category.");
        }

        if (tariff.FinancialYear is null)
        {
            throw new InvalidOperationException(
                "The selected tariff does not have a financial-year configuration.");
        }

        if (tariff.IsMultipurpose)
        {
            return BuildMultipurposeResult(
                request,
                tariff);
        }

        var exclusionAmount =
            GetApplicableExclusion(
                request,
                tariff);

        var rateableValue = Math.Max(
            0m,
            request.MarketValue - exclusionAmount);

        var annualRates = tariff.IsZeroRated
            ? 0m
            : rateableValue * tariff.AnnualTariff;

        var monthlyRates =
            annualRates / 12m;

        var warnings =
            BuildWarnings(
                request,
                tariff);

        return new PossibleRateCalculationResult
        {
            FinancialYearId =
                tariff.FinancialYearId,

            FinancialYear =
                tariff.FinancialYear.FinancialYear,

            CategoryCode =
                tariff.CategoryCode,

            CategoryName =
                tariff.CategoryName,

            Ratio =
                tariff.Ratio,

            MarketValue =
                RoundMoney(request.MarketValue),

            ExclusionAmount =
                RoundMoney(exclusionAmount),

            RateableValue =
                RoundMoney(rateableValue),

            AnnualTariff =
                tariff.AnnualTariff,

            EstimatedAnnualRates =
                RoundMoney(annualRates),

            EstimatedMonthlyRates =
                RoundMoney(monthlyRates),

            IsZeroRated =
                tariff.IsZeroRated,

            IsMultipurpose =
                tariff.IsMultipurpose,

            Disclaimer =
                GetDisclaimer(),

            Warnings =
                warnings
        };
    }

    // ════════════════════════════════════════════════════════════════
    // GET ONE TARIFF
    // ════════════════════════════════════════════════════════════════
    private async Task<PropertyRateTariff?>
        GetTariffAsync(
            string categoryCode,
            int? financialYearId,
            CancellationToken cancellationToken)
    {
        var query = _db.PropertyRateTariffs
            .AsNoTracking()
            .Include(tariff => tariff.FinancialYear)
            .Where(tariff =>
                tariff.IsActive &&
                tariff.FinancialYear.IsActive &&
                tariff.CategoryCode.Trim().ToUpper() == categoryCode);

        if (financialYearId.HasValue)
        {
            query = query.Where(tariff =>
                tariff.FinancialYearId == financialYearId.Value);
        }
        else
        {
            var today = DateTime.Today;
            query = query.Where(tariff =>
                today >= tariff.EffectiveFrom.Date &&
                today <= tariff.EffectiveTo.Date);
        }

        return await query
            .OrderByDescending(tariff => tariff.EffectiveFrom)
            .ThenByDescending(tariff => tariff.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // ════════════════════════════════════════════════════════════════
    // VALIDATION
    // ════════════════════════════════════════════════════════════════
    private static void ValidateRequest(
        PossibleRateCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.MarketValue <= 0)
        {
            throw new ArgumentException(
                "Market value must be greater than zero.",
                nameof(request.MarketValue));
        }

        if (string.IsNullOrWhiteSpace(
                request.CategoryCode))
        {
            throw new ArgumentException(
                "A property category must be selected.",
                nameof(request.CategoryCode));
        }
    }

    // ════════════════════════════════════════════════════════════════
    // RESIDENTIAL EXCLUSION
    // ════════════════════════════════════════════════════════════════
    private static decimal GetApplicableExclusion(
        PossibleRateCalculationRequest request,
        PropertyRateTariff tariff)
    {
        if (!request.ApplyResidentialExclusion)
        {
            return 0m;
        }

        if (!tariff.CategoryCode.Equals(
                "RES",
                StringComparison.OrdinalIgnoreCase))
        {
            return 0m;
        }

        var configuredExclusion =
            tariff.FinancialYear.ResidentialExclusion;

        return Math.Min(
            request.MarketValue,
            configuredExclusion);
    }

    // ════════════════════════════════════════════════════════════════
    // MULTIPURPOSE
    // ════════════════════════════════════════════════════════════════
    private static PossibleRateCalculationResult
        BuildMultipurposeResult(
            PossibleRateCalculationRequest request,
            PropertyRateTariff tariff)
    {
        return new PossibleRateCalculationResult
        {
            FinancialYearId =
                tariff.FinancialYearId,

            FinancialYear =
                tariff.FinancialYear.FinancialYear,

            CategoryCode =
                tariff.CategoryCode,

            CategoryName =
                tariff.CategoryName,

            Ratio =
                tariff.Ratio,

            MarketValue =
                RoundMoney(request.MarketValue),

            AnnualTariff =
                tariff.AnnualTariff,

            IsMultipurpose =
                true,

            Disclaimer =
                GetDisclaimer(),

            Warnings = new[]
            {
                "Multipurpose properties cannot be estimated using one tariff.",
                "The market value must be divided into the different property-use categories and each portion calculated separately."
            }
        };
    }

    // ════════════════════════════════════════════════════════════════
    // WARNINGS
    // ════════════════════════════════════════════════════════════════
    private static IReadOnlyCollection<string>
        BuildWarnings(
            PossibleRateCalculationRequest request,
            PropertyRateTariff tariff)
    {
        var warnings =
            new List<string>();

        if (
            tariff.CategoryCode.Equals(
                "RES",
                StringComparison.OrdinalIgnoreCase)
            &&
            request.ApplyResidentialExclusion
        )
        {
            warnings.Add(
                "The residential exclusion is subject to the applicable Rates Policy and qualification rules.");
        }

        if (tariff.IsPenaltyTariff)
        {
            warnings.Add(
                "The selected category uses a penalty tariff.");
        }

        if (tariff.IsZeroRated)
        {
            warnings.Add(
                "This category has a zero property-rate tariff. Other municipal charges may still apply.");
        }

        return warnings;
    }

    private static decimal RoundMoney(
        decimal amount)
    {
        return decimal.Round(
            amount,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static string GetDisclaimer()
    {
        return
            "This calculation is an estimate for information purposes only. " +
            "It covers property rates only and does not represent an official " +
            "municipal account or change any property information.";
    }
}
