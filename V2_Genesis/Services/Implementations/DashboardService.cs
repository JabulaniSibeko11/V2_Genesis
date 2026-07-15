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
        AttributesDbContext attrDb)
    {
        _config = config;
        _attrDb = attrDb;
    }

    // ── Roll data — unchanged ─────────────────────────────────────────
    public async Task<RollData> GetRollDataAsync(
          string rollSource, string userId, string userEmail)
    {
        var rollData = new RollData();

        if (!RollSearchRegistry.Configs.TryGetValue(rollSource, out var config))
            return rollData;

        var connString = _config.GetConnectionString(config.ConnectionKey)
                         ?? _config.GetConnectionString("DefaultConnection")!;

        // ── Detect if this is a Section 78 Query roll ─────────────────
        bool isQuery = config.IsQuery;   // flag on RollSearchConfig
                                         // OR: rollSource.Contains("Query")

        var spLinked = isQuery ? SP_QUERY_LINKED : SP_LINKED;
        var spObjected = isQuery ? SP_QUERY_OBJECTED : SP_OBJECTED;

        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync();

        // ── Linked properties ─────────────────────────────────────────
        try
        {
            var linked = await conn.QueryAsync<LinkedPropertyResult>(
                spLinked,
                new { userName = userId },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);
            rollData.LinkedProperties = linked.ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Dashboard] {spLinked} failed for {rollSource}: {ex.Message}");
        }

        // ── Submitted queries / objections ────────────────────────────
        try
        {
            var objected = await conn.QueryAsync<ObjectedPropertyResult>(
                spObjected,
                new { userName = userId },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);
            rollData.ObjectedProperties = objected.ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Dashboard] {spObjected} failed for {rollSource}: {ex.Message}");
        }

        // ── Appeals — same SP for both roll types ─────────────────────
        try
        {
            var appeals = await conn.QueryAsync<AppealResult>(
                SP_APPEALS,
                new { userName = userId },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);
            rollData.Appeals = appeals.ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Dashboard] {SP_APPEALS} failed for {rollSource}: {ex.Message}");
        }

        // ── Notifications — uses email, same SP for both ───────────────
        try
        {
            var notifications = await conn.QueryAsync<NotificationResult>(
                SP_NOTIFICATIONS,
                new { userEmail = userEmail },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);
            rollData.Notifications = notifications.ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Dashboard] {SP_NOTIFICATIONS} failed for {rollSource}: {ex.Message}");
        }

        return rollData;
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