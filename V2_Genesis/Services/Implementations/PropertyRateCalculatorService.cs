using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Models.Rates;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public sealed class PropertyRateCalculatorService
    : IPropertyRateCalculatorService
{
    private readonly string _connectionString;
    private readonly ILogger<PropertyRateCalculatorService> _logger;

    public PropertyRateCalculatorService(
        IConfiguration configuration,
        ILogger<PropertyRateCalculatorService> logger)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is missing from appsettings.json.");

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
        const string sql = """
            SELECT
                t.Id,
                t.FinancialYearId,
                t.CategoryCode,
                t.CategoryName,
                t.Ratio,
                t.AnnualTariff,
                t.IsZeroRated,
                t.IsMultipurpose,
                t.IsPenaltyTariff,
                t.IsActive,
                t.EffectiveFrom,
                t.EffectiveTo,
                t.CreatedBy,
                t.CreatedAtUtc,
                t.UpdatedBy,
                t.UpdatedAtUtc,

                fy.Id,
                fy.FinancialYear,
                fy.StartDate,
                fy.EndDate,
                fy.ResidentialExclusion,
                fy.AdditionalPropertyReduction,
                fy.IsActive,
                fy.CreatedBy,
                fy.CreatedAtUtc,
                fy.UpdatedBy,
                fy.UpdatedAtUtc

            FROM [Objection].[dbo].[PropertyRateTariffs] AS t

            INNER JOIN [Objection].[dbo].[RateFinancialYears] AS fy
                ON fy.Id = t.FinancialYearId

            WHERE
                t.IsActive = 1
                AND fy.IsActive = 1
                AND CAST(GETDATE() AS date)
                    BETWEEN t.EffectiveFrom AND t.EffectiveTo

            ORDER BY
                t.CategoryName;
            """;

        try
        {
            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);

            var command = new CommandDefinition(
                commandText: sql,
                cancellationToken: cancellationToken);

            var tariffs = await connection.QueryAsync<
                PropertyRateTariff,
                RateFinancialYear,
                PropertyRateTariff>(
                command,
                map: (tariff, financialYear) =>
                {
                    tariff.FinancialYear = financialYear;

                    return tariff;
                },
                splitOn: "Id");

            return tariffs.ToList();
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
        const string sql = """
            SELECT TOP (1)
                t.Id,
                t.FinancialYearId,
                t.CategoryCode,
                t.CategoryName,
                t.Ratio,
                t.AnnualTariff,
                t.IsZeroRated,
                t.IsMultipurpose,
                t.IsPenaltyTariff,
                t.IsActive,
                t.EffectiveFrom,
                t.EffectiveTo,
                t.CreatedBy,
                t.CreatedAtUtc,
                t.UpdatedBy,
                t.UpdatedAtUtc,

                fy.Id,
                fy.FinancialYear,
                fy.StartDate,
                fy.EndDate,
                fy.ResidentialExclusion,
                fy.AdditionalPropertyReduction,
                fy.IsActive,
                fy.CreatedBy,
                fy.CreatedAtUtc,
                fy.UpdatedBy,
                fy.UpdatedAtUtc

            FROM [Objection].[dbo].[PropertyRateTariffs] AS t

            INNER JOIN [Objection].[dbo].[RateFinancialYears] AS fy
                ON fy.Id = t.FinancialYearId

            WHERE
                t.IsActive = 1
                AND fy.IsActive = 1
                AND UPPER(LTRIM(RTRIM(t.CategoryCode))) =
                    UPPER(LTRIM(RTRIM(@CategoryCode)))

                AND
                (
                    @FinancialYearId IS NOT NULL
                    AND t.FinancialYearId = @FinancialYearId
                OR
                    @FinancialYearId IS NULL
                    AND CAST(GETDATE() AS date)
                        BETWEEN t.EffectiveFrom AND t.EffectiveTo
                )

            ORDER BY
                t.EffectiveFrom DESC,
                t.Id DESC;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new
            {
                CategoryCode = categoryCode,
                FinancialYearId = financialYearId
            },
            cancellationToken: cancellationToken);

        PropertyRateTariff? result = null;

        await connection.QueryAsync<
            PropertyRateTariff,
            RateFinancialYear,
            PropertyRateTariff>(
            command,
            map: (tariff, financialYear) =>
            {
                tariff.FinancialYear = financialYear;
                result = tariff;

                return tariff;
            },
            splitOn: "Id");

        return result;
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