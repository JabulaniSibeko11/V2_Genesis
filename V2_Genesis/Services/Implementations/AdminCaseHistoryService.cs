using Dapper;
using Microsoft.Data.SqlClient;
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Services.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

/// <summary>
/// Builds one read-only, property-centred history across objections, appeals,
/// Section 78 and Attributes. All searches use stable property keys.
/// </summary>
public sealed class AdminCaseHistoryService : IAdminCaseHistoryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminCaseHistoryService> _logger;
    private readonly int _commandTimeoutSeconds;

    public AdminCaseHistoryService(
        IConfiguration configuration,
        ILogger<AdminCaseHistoryService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _commandTimeoutSeconds = Math.Clamp(
            configuration.GetValue("AdminSearch:CommandTimeoutSeconds", 8),
            3,
            30);
    }

    public async Task<AdminCaseHistory> GetAsync(
        AdminEnquiryFoundation foundation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(foundation);

        if (!foundation.Property.HasStableIdentity)
            return new AdminCaseHistory();

        var rollTasks = SearchableRolls().Select(x => LoadRollCasesAsync(
            x.Key,
            x.Value,
            foundation.Property,
            cancellationToken));

        var queryTask = LoadQueryCasesAsync(
            foundation.Property,
            cancellationToken);
        var attributeTask = LoadAttributeCasesAsync(
            foundation.Property,
            cancellationToken);

        var rollResults = await Task.WhenAll(rollTasks);
        await Task.WhenAll(queryTask, attributeTask);

        var cases = rollResults
            .SelectMany(x => x)
            .Concat(queryTask.Result)
            .Concat(attributeTask.Result)
            .GroupBy(
                x => $"{x.CaseType}|{x.ReferenceNumber}",
                StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderByDescending(x => x.SubmittedAt)
            .ThenBy(x => CaseOrder(x.CaseType))
            .ToList();

        ApplyAppealEligibility(cases);

        return new AdminCaseHistory { Cases = cases };
    }

    private async Task<IReadOnlyCollection<AdminCaseHistoryItem>> LoadRollCasesAsync(
        string rollSource,
        AdminRollConfig config,
        AdminCanonicalProperty property,
        CancellationToken cancellationToken)
    {
        const string objectionSql = """
            SELECT TOP (50)
                Objection_No AS ReferenceNumber,
                objection_Status AS Status,
                Property_Desc AS PropertyDescription,
                Property_Type AS PropertyType,
                Premise_id AS PremiseId,
                Unit_key AS UnitKey,
                Valuation_Key AS ValuationKey,
                PropertyFrom,
                UserID AS UserId,
                Objector_Type AS ObjectorType,
                Objection_Start_DateTime AS SubmittedAt
            FROM dbo.Obj_Property_Info
            WHERE
                (@PremiseId <> '' AND Premise_id = @PremiseId)
                OR
                (@PremiseId = '' AND @UnitKey <> '' AND @ValuationKey <> ''
                 AND Unit_key = @UnitKey AND Valuation_Key = @ValuationKey)
            ORDER BY Objection_ID DESC;
            """;

        const string appealSql = """
            SELECT TOP (50)
                Appeal_No AS ReferenceNumber,
                Obj_Ref AS RelatedReferenceNumber,
                Appeal_Status AS Status,
                A_Property_Desc AS PropertyDescription,
                A_Property_Type AS PropertyType,
                A_Premise_id AS PremiseId,
                A_Unit_key AS UnitKey,
                A_Valuation_Key AS ValuationKey,
                A_UserID AS UserId,
                Appeal_Type AS ObjectorType,
                Appeal_Start_DateTime AS SubmittedAt
            FROM dbo.Obj_Property_Info_Appeal
            WHERE
                (@PremiseId <> '' AND A_Premise_id = @PremiseId)
                OR
                (@PremiseId = '' AND @UnitKey <> '' AND @ValuationKey <> ''
                 AND A_Unit_key = @UnitKey AND A_Valuation_Key = @ValuationKey)
                OR Obj_Ref IN @ObjectionReferences
            ORDER BY Appeal_ID DESC;
            """;

        try
        {
            await using var connection = CreateConnection(config.ConnectionKey);
            var parameters = new
            {
                property.PremiseId,
                property.UnitKey,
                property.ValuationKey
            };

            var objections = (await connection.QueryAsync<CaseRow>(
                Command(objectionSql, parameters, cancellationToken))).ToList();

            var objectionReferences = objections
                .Select(x => Clean(x.ReferenceNumber))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var appeals = await connection.QueryAsync<CaseRow>(
                Command(
                    appealSql,
                    new
                    {
                        property.PremiseId,
                        property.UnitKey,
                        property.ValuationKey,
                        ObjectionReferences = objectionReferences
                    },
                    cancellationToken));

            return objections
                .Select(x => Map(x, "Objection", rollSource))
                .Concat(appeals.Select(x => Map(x, "Appeal", rollSource)))
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
                "[AdminCaseHistory] Roll history failed. Roll={RollSource}",
                rollSource);
            return Array.Empty<AdminCaseHistoryItem>();
        }
    }

    private async Task<IReadOnlyCollection<AdminCaseHistoryItem>> LoadQueryCasesAsync(
        AdminCanonicalProperty property,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (50)
                QUERY_No AS ReferenceNumber,
                QUERY_Status AS Status,
                Property_Desc AS PropertyDescription,
                Property_Type AS PropertyType,
                Premise_id AS PremiseId,
                Unit_key AS UnitKey,
                Valuation_Key AS ValuationKey,
                UserID AS UserId,
                QUERY_Type AS ObjectorType,
                QUERY_Start_DateTime AS SubmittedAt,
                Sub_typ AS SubType
            FROM dbo.QUE_Property_Info
            WHERE
                (@PremiseId <> '' AND Premise_id = @PremiseId)
                OR
                (@PremiseId = '' AND @UnitKey <> '' AND @ValuationKey <> ''
                 AND Unit_key = @UnitKey AND Valuation_Key = @ValuationKey)
            ORDER BY QUERY_ID DESC;
            """;

        try
        {
            await using var connection = CreateConnection("QueryConnection");
            var rows = await connection.QueryAsync<CaseRow>(
                Command(
                    sql,
                    new
                    {
                        property.PremiseId,
                        property.UnitKey,
                        property.ValuationKey
                    },
                    cancellationToken));

            return rows.Select(x =>
            {
                var type = x.SubType == 1 ? "Review" : "Query";
                if (type == "Review"
                    && !Clean(x.ReferenceNumber).EndsWith(
                        "-R",
                        StringComparison.OrdinalIgnoreCase))
                {
                    x.ReferenceNumber = Clean(x.ReferenceNumber) + "-R";
                }

                return Map(x, type, "Objection_Query");
            }).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AdminCaseHistory] Query history failed.");
            return Array.Empty<AdminCaseHistoryItem>();
        }
    }

    private async Task<IReadOnlyCollection<AdminCaseHistoryItem>> LoadAttributeCasesAsync(
        AdminCanonicalProperty property,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (50)
                Attr_No AS ReferenceNumber,
                Attr_Status AS Status,
                Property_Desc AS PropertyDescription,
                Property_Type AS PropertyType,
                Premise_id AS PremiseId,
                Unit_key AS UnitKey,
                Valuation_Key AS ValuationKey,
                SubmittedByUserId AS UserId,
                Objector_Type AS ObjectorType,
                SubmissionDateTime AS SubmittedAt,
                RollDescription AS RollName
            FROM dbo.Attr_Property_Info
            WHERE
                (@PremiseId <> '' AND Premise_id = @PremiseId)
                OR
                (@PremiseId = '' AND @UnitKey <> '' AND @ValuationKey <> ''
                 AND Unit_key = @UnitKey AND Valuation_Key = @ValuationKey)
            ORDER BY Attr_ID DESC;
            """;

        try
        {
            await using var connection = CreateConnection("AttributesConnection");
            var rows = await connection.QueryAsync<CaseRow>(
                Command(
                    sql,
                    new
                    {
                        property.PremiseId,
                        property.UnitKey,
                        property.ValuationKey
                    },
                    cancellationToken));

            return rows.Select(x => Map(x, "Attributes", "Attributes")).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AdminCaseHistory] Attributes history failed.");
            return Array.Empty<AdminCaseHistoryItem>();
        }
    }

    private static AdminCaseHistoryItem Map(
        CaseRow row,
        string caseType,
        string rollSource)
    {
        var reference = Clean(row.ReferenceNumber);
        var item = new AdminCaseHistoryItem
        {
            CaseType = caseType,
            ReferenceNumber = reference,
            RelatedReferenceNumber = Clean(row.RelatedReferenceNumber),
            RollSource = rollSource,
            RollName = string.IsNullOrWhiteSpace(row.RollName)
                ? RollName(rollSource)
                : row.RollName.Trim(),
            SourceTable = SourceTable(rollSource),
            Status = Clean(row.Status),
            PropertyDescription = Clean(row.PropertyDescription),
            PropertyType = Clean(row.PropertyType),
            PremiseId = Clean(row.PremiseId),
            UnitKey = Clean(row.UnitKey),
            ValuationKey = Clean(row.ValuationKey),
            PropertyFrom = caseType == "Attributes"
                ? "Attributes"
                : string.IsNullOrWhiteSpace(row.PropertyFrom)
                    ? SourceTable(rollSource)
                    : row.PropertyFrom.Trim(),
            UserId = Clean(row.UserId),
            ObjectorType = Clean(row.ObjectorType),
            SubmittedAt = row.SubmittedAt
        };

        item.ViewUrl = BuildViewUrl(item);
        return item;
    }

    private static void ApplyAppealEligibility(List<AdminCaseHistoryItem> cases)
    {
        var appealedObjections = cases
            .Where(x => x.CaseType == "Appeal")
            .Select(x => x.RelatedReferenceNumber)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in cases.Where(x => x.CaseType == "Objection"))
        {
            var noticeSent = item.Status.Equals(
                "Notice-Sent",
                StringComparison.OrdinalIgnoreCase);
            var alreadyAppealed = appealedObjections.Contains(item.ReferenceNumber);

            item.CanLodgeAppeal = noticeSent && !alreadyAppealed;
            item.AppealUnavailableReason = alreadyAppealed
                ? "An Appeal is already linked to this Objection."
                : !noticeSent
                    ? "Appeal becomes available when the Objection status is Notice-Sent."
                    : string.Empty;
        }
    }

    private SqlConnection CreateConnection(string connectionKey)
    {
        var connectionString = _configuration.GetConnectionString(connectionKey)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionKey}' was not found.");
        return new SqlConnection(connectionString);
    }

    private CommandDefinition Command(
        string sql,
        object parameters,
        CancellationToken cancellationToken) =>
        new(
            sql,
            parameters,
            commandTimeout: _commandTimeoutSeconds,
            cancellationToken: cancellationToken);

    private static IEnumerable<KeyValuePair<string, AdminRollConfig>> SearchableRolls() =>
        AdminRollRegistry.Configs.Where(x => !x.Key.Equals(
            "Objection_Supp5",
            StringComparison.OrdinalIgnoreCase));

    private static string BuildViewUrl(AdminCaseHistoryItem item) =>
        $"/submissions/view/{Uri.EscapeDataString(item.CaseType)}/" +
        $"{Uri.EscapeDataString(item.ReferenceNumber)}?rollSource=" +
        $"{Uri.EscapeDataString(item.RollSource)}&returnUrl=%2Fadmin%2Fsearch";

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

    private static int CaseOrder(string type) => type switch
    {
        "Objection" => 0,
        "Appeal" => 1,
        "Query" => 2,
        "Review" => 3,
        "Attributes" => 4,
        _ => 99
    };

    private static string SourceTable(string rollSource) => rollSource switch
    {
        "Objection" => "GV23",
        "Objection_Supp1" => "GV23-SUP1",
        "Objection_Supp2" => "GV23-SUP2",
        "Objection_Supp3" => "GV23-SUP3",
        "Objection_Supp4" => "GV23-SUP4",
        "Objection_Query" => "Query",
        "Attributes" => "Attributes",
        _ => rollSource
    };

    private static string RollName(string rollSource) => rollSource switch
    {
        "Objection" => "GV 2023",
        "Objection_Supp1" => "Supplementary Roll 1",
        "Objection_Supp2" => "Supplementary Roll 2",
        "Objection_Supp3" => "Supplementary Roll 3",
        "Objection_Supp4" => "Supplementary Roll 4",
        "Objection_Query" => "Section 78 Query / Review",
        "Attributes" => "Property Attributes",
        _ => rollSource
    };

    private sealed class CaseRow
    {
        public string? ReferenceNumber { get; set; }
        public string? RelatedReferenceNumber { get; set; }
        public string? Status { get; set; }
        public string? PropertyDescription { get; set; }
        public string? PropertyType { get; set; }
        public string? PremiseId { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PropertyFrom { get; set; }
        public string? UserId { get; set; }
        public string? ObjectorType { get; set; }
        public string? RollName { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int SubType { get; set; }
    }
}
