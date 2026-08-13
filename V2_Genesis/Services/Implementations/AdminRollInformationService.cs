using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using V2_Genesis.Models.Lis;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Services.Admin;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Services.Implementations;

/// <summary>
/// Loads the selected property's actual valuation-roll records, linked state
/// and LIS fallback. It does not insert submissions or alter roll data.
/// </summary>
public sealed class AdminRollInformationService : IAdminRollInformationService
{
    private readonly IPropertySearchService _propertySearch;
    private readonly ILisSearchService _lisSearch;
    private readonly IConfiguration _configuration;
    private readonly RollDatesSettings _rollDates;
    private readonly ILogger<AdminRollInformationService> _logger;
    private readonly int _commandTimeoutSeconds;

    public AdminRollInformationService(
        IPropertySearchService propertySearch,
        ILisSearchService lisSearch,
        IConfiguration configuration,
        IOptions<RollDatesSettings> rollDates,
        ILogger<AdminRollInformationService> logger)
    {
        _propertySearch = propertySearch;
        _lisSearch = lisSearch;
        _configuration = configuration;
        _rollDates = rollDates.Value;
        _logger = logger;
        _commandTimeoutSeconds = Math.Clamp(
            configuration.GetValue("AdminSearch:CommandTimeoutSeconds", 8),
            3,
            30);
    }

    public async Task<AdminRollInformation> GetAsync(
        AdminEnquiryFoundation foundation,
        string? selectedRollSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(foundation);

        var property = foundation.Property;
        var account = foundation.AccountInformation.SubmittingAccount;
        var rolls = SearchableRolls().ToList();

        if (!property.HasStableIdentity)
        {
            return new AdminRollInformation
            {
                OmissionCandidates = BuildOmissionCandidates(
                    rolls,
                    selectedRollSource)
            };
        }

        var rollTasks = rolls.Select(x => LoadRollAsync(
            x.Key,
            x.Value,
            property,
            account,
            cancellationToken));

        var rollResults = (await Task.WhenAll(rollTasks))
            .SelectMany(x => x)
            .ToList();

        if (rollResults.Count > 0)
        {
            return new AdminRollInformation
            {
                Properties = Deduplicate(rollResults)
            };
        }

        // LIS is a fallback only. It is never queried when the property was
        // found on at least one actual valuation roll.
        var lisRolls = PrioritiseRolls(rolls, selectedRollSource, foundation.Reference.RollSource);
        var lisTasks = lisRolls.Select(x => LoadLisAsync(
            x.Key,
            x.Value,
            property,
            account,
            cancellationToken));

        var lisResults = (await Task.WhenAll(lisTasks))
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        if (lisResults.Count > 0)
        {
            return new AdminRollInformation
            {
                UsedLisFallback = true,
                Properties = Deduplicate(lisResults)
            };
        }

        return new AdminRollInformation
        {
            OmissionCandidates = BuildOmissionCandidates(
                rolls,
                selectedRollSource)
        };
    }

    private async Task<IReadOnlyCollection<AdminRollPropertyItem>> LoadRollAsync(
        string rollSource,
        AdminRollConfig config,
        AdminCanonicalProperty property,
        AdminSubmittingAccount account,
        CancellationToken cancellationToken)
    {
        try
        {
            var details = await _propertySearch.GetPropertyDetailsAsync(
                rollSource,
                property.UnitKey,
                property.ValuationKey,
                cancellationToken);

            var items = details
                .Where(x => SameProperty(x, property))
                .Take(20)
                .Select(x => MapRollProperty(rollSource, x, account))
                .ToList();

            await PopulateLinkedStateAsync(
                items,
                config.ConnectionKey,
                account,
                cancellationToken);

            return items;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AdminRollInformation] Roll detail lookup failed. Roll={RollSource}",
                rollSource);
            return Array.Empty<AdminRollPropertyItem>();
        }
    }

    private async Task<AdminRollPropertyItem?> LoadLisAsync(
        string rollSource,
        AdminRollConfig config,
        AdminCanonicalProperty property,
        AdminSubmittingAccount account,
        CancellationToken cancellationToken)
    {
        try
        {
            var lis = await _lisSearch.GetPropertyByKeysAsync(
                rollSource,
                property.UnitKey,
                property.ValuationKey,
                cancellationToken);

            if (lis is null || !SameProperty(lis, property))
                return null;

            var item = MapLisProperty(rollSource, lis, account);
            await PopulateLinkedStateAsync(
                new[] { item },
                config.ConnectionKey,
                account,
                cancellationToken);
            return item;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AdminRollInformation] LIS fallback failed. Roll={RollSource}",
                rollSource);
            return null;
        }
    }

    private async Task PopulateLinkedStateAsync(
        IEnumerable<AdminRollPropertyItem> items,
        string connectionKey,
        AdminSubmittingAccount account,
        CancellationToken cancellationToken)
    {
        var list = items.ToList();
        if (list.Count == 0)
            return;

        if (!account.CanOwnLinkedProperties)
        {
            foreach (var item in list)
            {
                item.CanLink = false;
                item.LinkUnavailableReason = account.IsAdministrativeAccount
                    ? "The resolved login is an Administration account. Select or resolve the genuine client account before linking."
                    : "No genuine client account was resolved for this submission.";
                ApplyLodgementState(item, account);
            }
            return;
        }

        var keys = list
            .Select(x => x.IdProperty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var linkedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var linkedStateAvailable = true;

        if (keys.Count > 0)
        {
            const string sql = """
                SELECT IDProperty
                FROM dbo.LinkedProperties
                WHERE UserID = @UserId
                  AND IDProperty IN @Keys;
                """;

            try
            {
                var connectionString = _configuration.GetConnectionString(connectionKey)
                    ?? throw new InvalidOperationException(
                        $"Connection string '{connectionKey}' was not found.");

                await using var connection = new SqlConnection(connectionString);
                var rows = await connection.QueryAsync<string>(
                    new CommandDefinition(
                        sql,
                        new { UserId = account.UserId, Keys = keys },
                        commandTimeout: _commandTimeoutSeconds,
                        cancellationToken: cancellationToken));

                linkedKeys.UnionWith(rows.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                linkedStateAvailable = false;
                _logger.LogWarning(
                    ex,
                    "[AdminRollInformation] Linked-property state was unavailable. Connection={ConnectionKey}",
                    connectionKey);
            }
        }

        foreach (var item in list)
        {
            item.IsLinked = linkedStateAvailable
                && linkedKeys.Contains(item.IdProperty);
            item.CanLink = linkedStateAvailable
                && !item.IsLinked
                && !string.IsNullOrWhiteSpace(item.IdProperty);
            item.LinkUnavailableReason = !linkedStateAvailable
                ? "The linked-property status is temporarily unavailable. No change has been made."
                : item.IsLinked
                ? "This property is already linked to the client account."
                : string.IsNullOrWhiteSpace(item.IdProperty)
                    ? "The property cannot be linked because its roll key is missing."
                    : string.Empty;
            ApplyLodgementState(item, account);
        }
    }

    private void ApplyLodgementState(
        AdminRollPropertyItem item,
        AdminSubmittingAccount account)
    {
        var dates = _rollDates.For(item.RollSource);

        item.ObjectionOpenDate = dates?.OpenDate;
        item.ObjectionCloseDate = dates?.VisibleUntil;
        item.CanLodgeObjection = account.CanOwnLinkedProperties
            && item.IsLinked;
        item.CanStartQuery = account.CanOwnLinkedProperties
            && item.IsLinked;
        item.CanViewSection49 = !item.IsLis;

        item.ObjectionUnavailableReason = !account.CanOwnLinkedProperties
            ? "A genuine client account is required before starting a submission."
            : !item.IsLinked
                ? "Link the property to the client account first."
                : string.Empty;
    }

    private AdminRollPropertyItem MapRollProperty(
        string rollSource,
        PropertyDetailResult row,
        AdminSubmittingAccount account)
    {
        var item = new AdminRollPropertyItem
        {
            RollSource = rollSource,
            RollName = RollName(rollSource),
            SourceTable = SourceTable(rollSource),
            PropertyFrom = SourceTable(rollSource),
            IdProperty = First(row.Id, row.PropertyId, row.UnitKey, row.ValuationKey),
            PropertyId = Clean(row.PropertyId),
            PropertyDescription = Clean(row.PropertyDesc),
            PremiseId = Clean(row.PremiseId),
            UnitKey = Clean(row.UnitKey),
            ValuationKey = Clean(row.ValuationKey),
            Town = Clean(row.TownNameDesc),
            Category = Clean(row.CatDesc),
            MarketValue = Clean(row.MarketValue),
            IsLis = false
        };
        ApplyLodgementState(item, account);
        return item;
    }

    private AdminRollPropertyItem MapLisProperty(
        string rollSource,
        LisProperty row,
        AdminSubmittingAccount account)
    {
        var item = new AdminRollPropertyItem
        {
            RollSource = rollSource,
            RollName = $"LIS for {RollName(rollSource)}",
            SourceTable = SourceTable(rollSource),
            PropertyFrom = "LIS",
            IdProperty = First(row.PropertyId, row.UnitKey, row.ValuationKey),
            PropertyId = Clean(row.PropertyId),
            PropertyDescription = First(
                row.PropertyDescription,
                BuildLisDescription(row)),
            PremiseId = Clean(row.PremiseId),
            UnitKey = Clean(row.UnitKey),
            ValuationKey = Clean(row.ValuationKey),
            Town = Clean(row.TownNameDescription),
            Category = Clean(row.CATDescription),
            MarketValue = Clean(row.MarketValue),
            IsLis = true,
            CanViewSection49 = false
        };
        ApplyLodgementState(item, account);
        item.CanViewSection49 = false;
        return item;
    }

    private List<AdminOmissionCandidate> BuildOmissionCandidates(
        IReadOnlyCollection<KeyValuePair<string, AdminRollConfig>> rolls,
        string? selectedRollSource)
    {
        var selectedIsValuationRoll = !string.IsNullOrWhiteSpace(selectedRollSource)
            && rolls.Any(x => x.Key.Equals(
                selectedRollSource,
                StringComparison.OrdinalIgnoreCase));

        var candidates = rolls
            .Where(x => !selectedIsValuationRoll
                || x.Key.Equals(selectedRollSource, StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var dates = _rollDates.For(x.Key);

                return new AdminOmissionCandidate
                {
                    RollSource = x.Key,
                    RollName = RollName(x.Key),
                    SourceTable = SourceTable(x.Key),
                    // Admin lodging is intentionally not restricted by the
                    // public objection period.
                    CanLodge = true,
                    OpenDate = dates?.OpenDate,
                    CloseDate = dates?.VisibleUntil,
                    UnavailableReason = string.Empty
                };
            })
            .ToList();

        return candidates;
    }

    private static bool SameProperty(
        PropertyDetailResult row,
        AdminCanonicalProperty property) =>
        SameStableKey(row.PremiseId, row.UnitKey, row.ValuationKey, property);

    private static bool SameProperty(
        LisProperty row,
        AdminCanonicalProperty property) =>
        SameStableKey(row.PremiseId, row.UnitKey, row.ValuationKey, property);

    private static bool SameStableKey(
        string? premiseId,
        string? unitKey,
        string? valuationKey,
        AdminCanonicalProperty property)
    {
        if (!string.IsNullOrWhiteSpace(property.PremiseId)
            && Clean(premiseId).Equals(
                property.PremiseId,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(property.UnitKey)
            && !string.IsNullOrWhiteSpace(property.ValuationKey)
            && Clean(unitKey).Equals(property.UnitKey, StringComparison.OrdinalIgnoreCase)
            && Clean(valuationKey).Equals(property.ValuationKey, StringComparison.OrdinalIgnoreCase);
    }

    private static List<AdminRollPropertyItem> Deduplicate(
        IEnumerable<AdminRollPropertyItem> rows) =>
        rows.GroupBy(
                x => string.Join('|',
                    x.RollSource,
                    x.PropertyFrom,
                    x.IdProperty,
                    x.UnitKey,
                    x.ValuationKey,
                    x.Category,
                    Digits(x.MarketValue)),
                StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => RollOrder(x.RollSource))
            .ThenBy(x => x.IsLis)
            .ToList();

    private static IEnumerable<KeyValuePair<string, AdminRollConfig>> PrioritiseRolls(
        IReadOnlyCollection<KeyValuePair<string, AdminRollConfig>> rolls,
        string? selected,
        string? referenceRoll) =>
        rolls.OrderBy(x => x.Key.Equals(selected, StringComparison.OrdinalIgnoreCase) ? 0
            : x.Key.Equals(referenceRoll, StringComparison.OrdinalIgnoreCase) ? 1
            : 2)
            .ThenBy(x => RollOrder(x.Key));

    private static IEnumerable<KeyValuePair<string, AdminRollConfig>> SearchableRolls() =>
        AdminRollRegistry.Configs.Where(x => !x.Key.Equals(
            "Objection_Supp5",
            StringComparison.OrdinalIgnoreCase));

    private static string BuildLisDescription(LisProperty row) =>
        !string.IsNullOrWhiteSpace(row.SchemeName)
            ? $"{row.SchemeName} Unit {row.UnitNo} - {row.TownNameDescription}"
            : $"ERF {row.Erf} PTN {row.Ptn} {row.TownNameDescription}".Trim();

    private static string First(params string?[] values) =>
        values.Select(Clean).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
        ?? string.Empty;

    private static string Digits(string? value) =>
        new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

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
