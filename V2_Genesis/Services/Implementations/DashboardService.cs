using Dapper;
using GenesisV2.Services.PropertySearch;
using System.Data;
using System.Data.SqlClient;
using V2_Genesis.Data;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.Results.Atrributes;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services.Interfaces;

public class DashboardService : IDashboardService
{
    private readonly IConfiguration _config;
    private readonly AttributesDbContext _attrDb;
    private readonly ILogger<DashboardService> _logger;

    private const string SP_LINKED = "DashboardLinked";
    private const string SP_OBJECTED = "DashboardObjection";
    private const string SP_APPEALS = "DashboardAppeal";
    private const string SP_NOTIFICATIONS = "DashboardNotification";
    private const string SP_ATTR_LINKED = "Attr_DashboardLinked";
    private readonly string SP_QUERY_LINKED = "DashboardLinkedQ";
    private readonly string SP_QUERY_OBJECTED = "DashboardObjectionQ";
    private readonly string SP_QUERY_APPEAL = "DashboardAppeal";
    private readonly string SP_QUERY_NOTIFICATION = "DashboardNotification";
    public DashboardService(
        IConfiguration config,
        AttributesDbContext attrDb,
        ILogger<DashboardService> logger)
    {
        _config = config;
        _attrDb = attrDb;
        _logger = logger;
    }

    // ── Roll data — unchanged ─────────────────────────────────────────
    public async Task<RollData> GetRollDataAsync(
         string rollSource,
         string userId,
         string userEmail)
    {
        var rollData =
            new RollData();

        if (string.IsNullOrWhiteSpace(rollSource))
            return rollData;

        if (string.IsNullOrWhiteSpace(userId))
            return rollData;

        if (!RollSearchRegistry.Configs.TryGetValue(
                rollSource,
                out var config))
        {
            _logger.LogWarning(
                "Dashboard requested for unknown roll source {RollSource}",
                rollSource);

            return rollData;
        }

        var connectionString =
            _config.GetConnectionString(
                config.ConnectionKey)
            ?? _config.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError(
                "No dashboard connection string was found for roll {RollSource} using key {ConnectionKey}",
                rollSource,
                config.ConnectionKey);

            return rollData;
        }

        var isQuery =
            config.IsQuery ||
            rollSource.Equals(
                "Query",
                StringComparison.OrdinalIgnoreCase);

        var linkedProcedure =
            isQuery
                ? SP_QUERY_LINKED
                : SP_LINKED;

        var objectedProcedure =
            isQuery
                ? SP_QUERY_OBJECTED
                : SP_OBJECTED;

        await using var conn =
            new SqlConnection(connectionString);

        await conn.OpenAsync();

        // ─────────────────────────────────────────────────────────
        // Linked properties
        // ─────────────────────────────────────────────────────────

        try
        {
            var linked =
                await conn.QueryAsync<LinkedPropertyResult>(
                    linkedProcedure,
                    new
                    {
                        userName = userId
                    },
                    commandType:
                        CommandType.StoredProcedure,
                    commandTimeout: 60);

            var linkedProperties =
                linked.ToList();

            if (isQuery)
            {
                NormaliseQueryLinkedProperties(
                    linkedProperties);
            }

            rollData.LinkedProperties =
                linkedProperties;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{StoredProcedure} failed for roll {RollSource} and user {UserId}",
                linkedProcedure,
                rollSource,
                userId);
        }

        // ─────────────────────────────────────────────────────────
        // Submitted Queries / Reviews / Objections
        // ─────────────────────────────────────────────────────────

        try
        {
            var objected =
                await conn.QueryAsync<ObjectedPropertyResult>(
                    objectedProcedure,
                    new
                    {
                        userName = userId
                    },
                    commandType:
                        CommandType.StoredProcedure,
                    commandTimeout: 60);

            var objectedProperties =
                objected.ToList();

            if (isQuery)
            {
                NormaliseSubmittedQueryProperties(
                    objectedProperties);
            }

            rollData.ObjectedProperties =
                objectedProperties;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{StoredProcedure} failed for roll {RollSource} and user {UserId}",
                objectedProcedure,
                rollSource,
                userId);
        }

        // ─────────────────────────────────────────────────────────
        // Appeals
        // ─────────────────────────────────────────────────────────

        try
        {
            var appeals =
                await conn.QueryAsync<AppealResult>(
                    SP_APPEALS,
                    new
                    {
                        userName = userId
                    },
                    commandType:
                        CommandType.StoredProcedure,
                    commandTimeout: 60);

            rollData.Appeals =
                appeals.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{StoredProcedure} failed for roll {RollSource} and user {UserId}",
                SP_APPEALS,
                rollSource,
                userId);
        }

        // ─────────────────────────────────────────────────────────
        // Notifications
        // ─────────────────────────────────────────────────────────

        try
        {
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                var notifications =
                    await conn.QueryAsync<NotificationResult>(
                        SP_NOTIFICATIONS,
                        new
                        {
                            userEmail
                        },
                        commandType:
                            CommandType.StoredProcedure,
                        commandTimeout: 60);

                rollData.Notifications =
                    notifications.ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{StoredProcedure} failed for roll {RollSource} and email {UserEmail}",
                SP_NOTIFICATIONS,
                rollSource,
                userEmail);
        }

        return rollData;
    }

    // ─────────────────────────────────────────────────────────────
    // Section 78 Query dashboard safeguards
    // ─────────────────────────────────────────────────────────────

    private static void NormaliseQueryLinkedProperties(
        List<LinkedPropertyResult> properties)
    {
        foreach (var property in properties)
        {
            property.Review_Status =
                NormaliseReviewStatus(
                    property.Review_Status,
                    property.Review_Close_Date);

            /*
             * The updated DashboardLinkedQ procedure should return
             * AvailableAction. This fallback protects the UI if an
             * older row or partial result does not contain it.
             */
            if (string.IsNullOrWhiteSpace(
                    property.AvailableAction))
            {
                property.AvailableAction =
                    ResolveAvailableAction(
                        property.Review_Status,
                        property.HasCompletedQuery);
            }
        }
    }

    private static void NormaliseSubmittedQueryProperties(
        List<ObjectedPropertyResult> properties)
    {
        foreach (var property in properties)
        {
            property.Review_Status =
                NormaliseReviewStatus(
                    property.Review_Status,
                    property.Review_Close_Date);

            property.CanLodgeReview =
                string.Equals(
                    property.Review_Status,
                    "Open",
                    StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(
                    property.ReviewActionText))
            {
                property.ReviewActionText =
                    property.CanLodgeReview
                        ? "Lodge Review"
                        : "Review Closed";
            }
        }
    }

    private static string NormaliseReviewStatus(
        string? databaseStatus,
        DateTime? reviewCloseDate)
    {
        /*
         * The persisted Review_Status is the primary value.
         */
        if (string.Equals(
                databaseStatus,
                "Closed",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Closed";
        }

        /*
         * A past closing date must never remain open in the UI,
         * even if the SQL Agent job has not run yet.
         */
        if (reviewCloseDate.HasValue &&
            reviewCloseDate.Value.Date < DateTime.Today)
        {
            return "Closed";
        }

        /*
         * NULL closing date means the initial Query process is
         * still available.
         */
        return "Open";
    }

    private static string ResolveAvailableAction(
        string? reviewStatus,
        bool hasCompletedQuery)
    {
        if (string.Equals(
                reviewStatus,
                "Closed",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Closed";
        }

        return hasCompletedQuery
            ? "Review"
            : "Query";
    }

    // ── Attributes linked properties ──────────────────────────────────
    // Calls Attr_DashboardLinked SP on GenesisAttributes DB.
    // Returns full property details + FormType + HasSubmission.
    public async Task<List<AttrLinkedPropertyResult>> GetAttributesLinkedAsync(string userId)
    {
        var results = new List<AttrLinkedPropertyResult>();

        try
        {
            var connString = _config.GetConnectionString("AttributesConnection");

            if (string.IsNullOrWhiteSpace(connString))
            {
                Console.Error.WriteLine("[Dashboard] AttributesConnection is missing.");
                return results;
            }

            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();

            // 1. Get only the linked properties for this user
            var linkedRows = await conn.QueryAsync<AttrLinkedPropertyResult>(@"
SELECT
    ID AS Id,
    IDProperty,
    ISNULL(PropertyFrom, 'Attributes') AS PropertyFrom
FROM dbo.LinkedProperties_Attr
WHERE UserID = @UserId
ORDER BY ID DESC;",
                new { UserId = userId },
                commandTimeout: 60);

            foreach (var linked in linkedRows)
            {
                // 2. Pull the full property detail using the same backend SP used by the Attributes form
                var detail = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "Attr_GetPropertyForCheck",
                    new { UnitKey = linked.IDProperty },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60);

                if (detail == null)
                {
                    results.Add(new AttrLinkedPropertyResult
                    {
                        Id = linked.Id,
                        IDProperty = linked.IDProperty,
                        PropertyFrom = linked.PropertyFrom,
                        PropertyDesc = linked.IDProperty,
                        FormType = "Residential",
                        HasSubmission = await HasAttributeSubmissionAsync(conn, userId, linked.IDProperty)
                    });

                    continue;
                }

                var catDesc = GetValue(detail, "CatDesc");
                var schemeName = GetValue(detail, "SchemeName");
                var unitNoText = GetValue(detail, "UnitNo");

                var item = new AttrLinkedPropertyResult
                {
                    Id = linked.Id,
                    IDProperty = linked.IDProperty,
                    PropertyFrom = linked.PropertyFrom,

                    PropertyDesc = BuildAttributePropertyDescription(detail),
                    CatDesc = catDesc,
                    TownNameDesc = GetValue(detail, "TownNameDesc"),
                    MarketValue = FormatMoney(GetValue(detail, "MarketValue")),
                    RateableArea = GetValue(detail, "RateableArea"),
                    LisStreetAddress = GetValue(detail, "LisStreetAddress"),
                    SchemeName = schemeName,

                    FormType = ResolveAttributeFormType(catDesc, schemeName, unitNoText),
                    HasSubmission = await HasAttributeSubmissionAsync(conn, userId, linked.IDProperty)
                };

                var erfText = GetValue(detail, "Erf");

                if (int.TryParse(erfText, out int erf))
                    item.Erf = erf;

                if (int.TryParse(unitNoText, out int unitNo))
                    item.UnitNo = unitNo;
               

                results.Add(item);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Dashboard] GetAttributesLinkedAsync failed for user {userId}: {ex.Message}");
        }

        return results;
    }

    private static async Task<bool> HasAttributeSubmissionAsync(
    SqlConnection conn,
    string userId,
    string unitKey)
    {
        var count = await conn.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM dbo.Attr_Property_Info
WHERE SubmittedByUserId = @UserId
  AND Unit_key = @UnitKey
  AND ISNULL(IsActive, 1) = 1;",
            new
            {
                UserId = userId,
                UnitKey = unitKey
            },
            commandTimeout: 60);

        return count > 0;
    }

    private static string GetValue(dynamic row, string name)
    {
        if (row == null) return "";

        var dict = row as IDictionary<string, object>;

        if (dict != null && dict.TryGetValue(name, out var value))
            return value?.ToString() ?? "";

        return "";
    }

    private static string BuildAttributePropertyDescription(dynamic detail)
    {
        var propertyDesc = GetValue(detail, "PropertyDesc");

        if (!string.IsNullOrWhiteSpace(propertyDesc))
            return propertyDesc;

        var erf = GetValue(detail, "Erf");
        var ptn = GetValue(detail, "Ptn");
        var re = GetValue(detail, "Re");
        var town = GetValue(detail, "TownNameDesc");
        var scheme = GetValue(detail, "SchemeName");
        var unitNo = GetValue(detail, "UnitNo");

        // Sectional title:
        // Scheme UNIT 28, MULBARTON GARDENS, BEVERLEY EXT.100
        if (!string.IsNullOrWhiteSpace(scheme) ||
            (!string.IsNullOrWhiteSpace(unitNo) && unitNo != "0"))
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(unitNo) && unitNo != "0")
                parts.Add($"UNIT {unitNo}");

            if (!string.IsNullOrWhiteSpace(scheme))
                parts.Add(scheme);

            if (!string.IsNullOrWhiteSpace(town))
                parts.Add(town);

            return "Scheme " + string.Join(", ", parts);
        }

        // Portion:
        // PORTION 42 RUIMSIG 265-IQ
        if (!string.IsNullOrWhiteSpace(ptn) &&
            ptn != "0" &&
            !string.IsNullOrWhiteSpace(town))
        {
            if (!string.IsNullOrWhiteSpace(re) &&
                re.Equals("RE", StringComparison.OrdinalIgnoreCase))
            {
                return $"RE PORTION {ptn} {town}";
            }

            return $"PORTION {ptn} {town}";
        }

        // Full title:
        // Full Title ERF 334 LINBRO PARK EXT.181
        if (!string.IsNullOrWhiteSpace(erf) &&
            erf != "0" &&
            !string.IsNullOrWhiteSpace(town))
        {
            return $"Full Title ERF {erf} {town}";
        }

        if (!string.IsNullOrWhiteSpace(town))
            return town;

        return "";
    }

    private static string ResolveAttributeFormType(
        string? catDesc,
        string? schemeName,
        string? unitNo)
    {
        var cat = (catDesc ?? "").Trim().ToLower();

        // Sectional Title Residential
        if (!string.IsNullOrWhiteSpace(schemeName) ||
            (!string.IsNullOrWhiteSpace(unitNo) && unitNo != "0"))
        {
            return "ResidentialST";
        }

        // DRC Method
        if (cat.Contains("public service") ||
            cat.Contains("municipal") ||
            cat.Contains("religious") ||
            cat.Contains("mining") ||
            cat.Contains("agricultural") ||
            cat.Contains("vacant") ||
            cat.Contains("drc"))
        {
            return "DRCMethod";
        }

        // Non-Residential / Business
        if (cat.Contains("business") ||
            cat.Contains("commercial") ||
            cat.Contains("industrial") ||
            cat.Contains("retail") ||
            cat.Contains("office"))
        {
            return "BusinessCommercial";
        }

        return "Residential";
    }

    private static string FormatMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var clean = value
            .Replace("R", "", StringComparison.OrdinalIgnoreCase)
            .Replace(",", "")
            .Replace(" ", "")
            .Trim();

        if (!decimal.TryParse(clean, out var amount))
            return value;

        return "R " + amount.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture)
            .Replace(",", " ");
    }
    // ── FormType — 4 types based on property category ─────────────────
    private static string ResolveFormType(string? catDesc)
    {
        if (string.IsNullOrWhiteSpace(catDesc)) return "Residential";

        var cat = catDesc.Trim().ToLower();

        // Sectional title — form expects "ResidentialST"
        if (cat.Contains("sectional") ||
            cat.Contains("residential-st") ||
            cat.Contains("st ") || cat.Contains("unit"))
            return "ResidentialST";

        // Business / commercial — form expects "BusinessCommercial"
        if (cat.Contains("business") ||
            cat.Contains("commercial") ||
            cat.Contains("industrial") ||
            cat.Contains("retail") ||
            cat.Contains("office"))
            return "BusinessCommercial";

        // DRC — form expects "DRCMethod"
        if (cat.Contains("drc") ||
            cat.Contains("public service") ||
            cat.Contains("institutional"))
            return "DRCMethod";

        // Default
        return "Residential";
    }



}