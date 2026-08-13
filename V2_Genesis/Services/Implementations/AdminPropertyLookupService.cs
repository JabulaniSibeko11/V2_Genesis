using System.Text.RegularExpressions;
using V2_Genesis.Models;
using V2_Genesis.Models.Lis;
using V2_Genesis.Models.LIS;
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Services.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

/// <summary>
/// Searches valuation-roll property data first and LIS only as a fallback.
/// Results are filtered with AND semantics and exact numeric property tokens.
/// </summary>
public sealed class AdminPropertyLookupService : IAdminPropertyLookupService
{
    private readonly IPropertySearchService _propertySearch;
    private readonly ILisSearchService _lisSearch;
    private readonly ILogger<AdminPropertyLookupService> _logger;

    public AdminPropertyLookupService(
        IPropertySearchService propertySearch,
        ILisSearchService lisSearch,
        ILogger<AdminPropertyLookupService> logger)
    {
        _propertySearch = propertySearch;
        _lisSearch = lisSearch;
        _logger = logger;
    }

    public async Task<AdminSearchResult> SearchAsync(
        string? town,
        string? stand,
        string? address,
        string? scheme,
        string? unit,
        string? rollSource,
        CancellationToken cancellationToken = default)
    {
        var result = new AdminSearchResult
        {
            SearchType = "Property",
            SearchInput = string.Join(
                " ",
                new[] { town, stand, address, scheme, unit }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())),
            RollFilter = rollSource
        };

        town = Clean(town);
        stand = Clean(stand);
        address = Clean(address);
        scheme = Clean(scheme);
        unit = Clean(unit);

        if (string.IsNullOrWhiteSpace(town))
        {
            result.Warnings.Add(
                "Township is required so that the property search can exclude unrelated ERF and Unit numbers.");
            return result;
        }

        var rolls = SearchableRolls(rollSource).ToList();
        if (rolls.Count == 0)
        {
            result.Warnings.Add("The selected valuation roll is not supported.");
            return result;
        }

        var search = new PropertySearchParams
        {
            TownName = town,
            Stand = EmptyToNull(stand),
            Address = EmptyToNull(address),
            Scheme = EmptyToNull(scheme),
            Unit = EmptyToNull(unit)
        };

        var rollTasks = rolls.Select(x => SearchRollAsync(
            x.Key,
            search,
            town,
            stand,
            address,
            scheme,
            unit,
            cancellationToken));

        var rollRows = (await Task.WhenAll(rollTasks))
            .SelectMany(x => x)
            .ToList();

        if (rollRows.Count > 0)
        {
            result.PropertyCandidates = Deduplicate(rollRows);
            return result;
        }

        // LIS is deliberately a fallback. It is not searched when an actual
        // valuation-roll property satisfies all supplied criteria.
        var lisSearch = new LisSearchParams
        {
            SearchTownName = town,
            SearchStand = EmptyToNull(stand),
            SearchAddress = EmptyToNull(address),
            SearchScheme = EmptyToNull(scheme),
            SearchUnit = EmptyToNull(unit)
        };

        var lisTasks = rolls.Select(x => SearchLisAsync(
            x.Key,
            lisSearch,
            town,
            stand,
            address,
            scheme,
            unit,
            cancellationToken));

        var lisRows = (await Task.WhenAll(lisTasks))
            .SelectMany(x => x)
            .ToList();

        if (lisRows.Count > 0)
        {
            result.PropertyCandidates = Deduplicate(lisRows);
            return result;
        }

        result.PropertyOmissionCandidates = rolls.Select(x =>
            new AdminOmissionCandidate
            {
                RollSource = x.Key,
                RollName = RollName(x.Key),
                SourceTable = SourceTable(x.Key),
                CanLodge = true
            }).ToList();

        return result;
    }

    private async Task<IReadOnlyCollection<AdminPropertyCandidate>> SearchRollAsync(
        string rollSource,
        PropertySearchParams search,
        string town,
        string stand,
        string address,
        string scheme,
        string unit,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _propertySearch.SearchAsync(
                rollSource,
                search,
                cancellationToken);

            return rows
                .Where(x => Matches(
                    x.TownNameDesc,
                    x.PropertyDisplay,
                    x.LisStreetAddress,
                    x.SchemeName,
                    x.Erf,
                    x.UnitNo,
                    town,
                    stand,
                    address,
                    scheme,
                    unit))
                .Select(x => new AdminPropertyCandidate
                {
                    RollSource = rollSource,
                    RollName = RollName(rollSource),
                    SourceTable = SourceTable(rollSource),
                    PropertyFrom = SourceTable(rollSource),
                    PropertyDescription = x.PropertyDisplay,
                    Town = Clean(x.TownNameDesc),
                    Address = Clean(x.LisStreetAddress),
                    Category = Clean(x.CatDesc),
                    MarketValue = Clean(x.MarketValue),
                    UnitKey = Clean(x.UnitKey),
                    ValuationKey = Clean(x.ValuationKey),
                    Erf = x.Erf,
                    Portion = x.Ptn,
                    UnitNumber = x.UnitNo,
                    IsLis = false,
                    MatchScore = Score(
                        x.TownNameDesc,
                        x.PropertyDisplay,
                        x.LisStreetAddress,
                        x.SchemeName,
                        x.Erf,
                        x.UnitNo,
                        town,
                        stand,
                        address,
                        scheme,
                        unit)
                })
                .Take(100)
                .ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AdminPropertyLookup] Roll property search failed. Roll={RollSource}",
                rollSource);
            return Array.Empty<AdminPropertyCandidate>();
        }
    }

    private async Task<IReadOnlyCollection<AdminPropertyCandidate>> SearchLisAsync(
        string rollSource,
        LisSearchParams search,
        string town,
        string stand,
        string address,
        string scheme,
        string unit,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _lisSearch.SearchAsync(
                rollSource,
                search,
                cancellationToken);

            return rows
                .Where(x => Matches(
                    x.TownNameDescription,
                    PropertyDescription(x),
                    x.LisStreetAddress,
                    x.SchemeName,
                    x.Erf,
                    ParseInt(x.UnitNo),
                    town,
                    stand,
                    address,
                    scheme,
                    unit))
                .Select(x => new AdminPropertyCandidate
                {
                    RollSource = rollSource,
                    RollName = $"LIS for {RollName(rollSource)}",
                    SourceTable = SourceTable(rollSource),
                    PropertyFrom = "LIS",
                    PropertyDescription = PropertyDescription(x),
                    Town = Clean(x.TownNameDescription),
                    Address = Clean(x.LisStreetAddress),
                    Category = Clean(x.CATDescription),
                    MarketValue = Clean(x.MarketValue),
                    UnitKey = Clean(x.UnitKey),
                    ValuationKey = Clean(x.ValuationKey),
                    PropertyId = Clean(x.PropertyId),
                    Erf = x.Erf,
                    Portion = x.Ptn,
                    UnitNumber = ParseInt(x.UnitNo),
                    IsLis = true,
                    MatchScore = Score(
                        x.TownNameDescription,
                        PropertyDescription(x),
                        x.LisStreetAddress,
                        x.SchemeName,
                        x.Erf,
                        ParseInt(x.UnitNo),
                        town,
                        stand,
                        address,
                        scheme,
                        unit)
                })
                .Take(100)
                .ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AdminPropertyLookup] LIS search failed. Roll={RollSource}",
                rollSource);
            return Array.Empty<AdminPropertyCandidate>();
        }
    }

    private static bool Matches(
        string? candidateTown,
        string? description,
        string? candidateAddress,
        string? candidateScheme,
        int candidateErf,
        int candidateUnit,
        string town,
        string stand,
        string address,
        string scheme,
        string unit)
    {
        var townText = Normalise($"{candidateTown} {description}");
        if (!ContainsPhrase(townText, town))
            return false;

        if (!string.IsNullOrWhiteSpace(stand))
        {
            var standNumber = ParseInt(stand);
            var descriptionText = Normalise(description);
            var exactErf = standNumber > 0 && candidateErf == standNumber;
            var exactToken = Regex.IsMatch(
                descriptionText,
                $@"\b(?:ERF|STAND)\s*{Regex.Escape(stand)}\b",
                RegexOptions.IgnoreCase);

            if (!exactErf && !exactToken)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(address)
            && !ContainsPhrase(
                Normalise($"{candidateAddress} {description}"),
                address))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(scheme)
            && !ContainsPhrase(
                Normalise($"{candidateScheme} {description}"),
                scheme))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(unit))
        {
            var unitNumber = ParseInt(unit);
            var exactUnit = unitNumber > 0 && candidateUnit == unitNumber;
            var exactToken = Regex.IsMatch(
                Normalise(description),
                $@"\bUNIT\s*{Regex.Escape(unit)}\b",
                RegexOptions.IgnoreCase);

            if (!exactUnit && !exactToken)
                return false;
        }

        return true;
    }

    private static int Score(
        string? candidateTown,
        string? description,
        string? candidateAddress,
        string? candidateScheme,
        int candidateErf,
        int candidateUnit,
        string town,
        string stand,
        string address,
        string scheme,
        string unit)
    {
        var score = 0;
        if (Normalise(candidateTown) == Normalise(town)) score += 40;
        else if (ContainsPhrase(Normalise($"{candidateTown} {description}"), town)) score += 25;
        if (ParseInt(stand) > 0 && candidateErf == ParseInt(stand)) score += 40;
        if (!string.IsNullOrWhiteSpace(address)
            && ContainsPhrase(Normalise(candidateAddress), address)) score += 20;
        if (!string.IsNullOrWhiteSpace(scheme)
            && ContainsPhrase(Normalise(candidateScheme), scheme)) score += 20;
        if (ParseInt(unit) > 0 && candidateUnit == ParseInt(unit)) score += 20;
        return score;
    }

    private static List<AdminPropertyCandidate> Deduplicate(
        IEnumerable<AdminPropertyCandidate> rows) =>
        rows.GroupBy(
                x => string.Join(
                    '|',
                    x.RollSource,
                    x.PropertyFrom,
                    x.UnitKey,
                    x.ValuationKey,
                    x.PropertyId,
                    x.Erf,
                    x.Portion,
                    x.UnitNumber),
                StringComparer.OrdinalIgnoreCase)
            .Select(x => x.OrderByDescending(y => y.MatchScore).First())
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => RollOrder(x.RollSource))
            .ThenBy(x => x.PropertyDescription)
            .Take(250)
            .ToList();

    private static IEnumerable<KeyValuePair<string, AdminRollConfig>> SearchableRolls(
        string? selectedRoll) =>
        AdminRollRegistry.Configs
            .Where(x => !x.Key.Equals("Objection_Supp5", StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(selectedRoll)
                || x.Key.Equals(selectedRoll, StringComparison.OrdinalIgnoreCase));

    private static string PropertyDescription(LisProperty row) =>
        !string.IsNullOrWhiteSpace(row.PropertyDescription)
            ? row.PropertyDescription.Trim()
            : !string.IsNullOrWhiteSpace(row.SchemeName)
                ? $"{row.SchemeName} Unit {row.UnitNo} - {row.TownNameDescription}"
                : $"ERF {row.Erf} PTN {row.Ptn} {row.TownNameDescription}".Trim();

    private static bool ContainsPhrase(string candidate, string sought) =>
        candidate.Contains(Normalise(sought), StringComparison.OrdinalIgnoreCase);

    private static string Normalise(string? value)
    {
        var text = (value ?? string.Empty).Trim().ToUpperInvariant();
        text = Regex.Replace(text, @"([A-Z])([0-9])", "$1 $2");
        text = Regex.Replace(text, @"([0-9])([A-Z])", "$1 $2");
        return Regex.Replace(text, @"[^A-Z0-9]+", " ").Trim();
    }

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;
    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
    private static int ParseInt(string? value) =>
        int.TryParse(value?.Trim(), out var number) ? number : 0;

    private static string SourceTable(string rollSource) => rollSource switch
    {
        "Objection" => "GV23",
        "Objection_Supp1" => "GV23-SUP1",
        "Objection_Supp2" => "GV23-SUP2",
        "Objection_Supp3" => "GV23-SUP3",
        "Objection_Supp4" => "GV23-SUP4",
        _ => rollSource
    };

    private static string RollName(string rollSource) => rollSource switch
    {
        "Objection" => "GV 2023",
        "Objection_Supp1" => "Supplementary Roll 1",
        "Objection_Supp2" => "Supplementary Roll 2",
        "Objection_Supp3" => "Supplementary Roll 3",
        "Objection_Supp4" => "Supplementary Roll 4",
        _ => rollSource
    };

    private static int RollOrder(string rollSource) => rollSource switch
    {
        "Objection" => 0,
        "Objection_Supp1" => 1,
        "Objection_Supp2" => 2,
        "Objection_Supp3" => 3,
        "Objection_Supp4" => 4,
        _ => 99
    };
}
