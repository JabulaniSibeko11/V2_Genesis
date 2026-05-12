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

    public DashboardService(
        IConfiguration config,
        AttributesDbContext attrDb)
    {
        _config = config;
        _attrDb = attrDb;
    }

    // ── Roll data — unchanged ─────────────────────────────────────────
    public async Task<RollData> GetRollDataAsync(
        string rollSource,
        string userId,
        string userEmail)
    {
        var rollData = new RollData();

        if (!RollSearchRegistry.Configs.TryGetValue(rollSource, out var config))
            return rollData;

        var connString = _config.GetConnectionString(config.ConnectionKey)
                         ?? _config.GetConnectionString("DefaultConnection")!;

        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync();

        try
        {
            var linked = await conn.QueryAsync<LinkedPropertyResult>(
                SP_LINKED, new { userName = userId },
                commandType: CommandType.StoredProcedure, commandTimeout: 60);
            rollData.LinkedProperties = linked.ToList();
        }
        catch (Exception ex) { Console.Error.WriteLine($"[Dashboard] {SP_LINKED} failed for {rollSource}: {ex.Message}"); }

        try
        {
            var objected = await conn.QueryAsync<ObjectedPropertyResult>(
                SP_OBJECTED, new { userName = userId },
                commandType: CommandType.StoredProcedure, commandTimeout: 60);
            rollData.ObjectedProperties = objected.ToList();
        }
        catch (Exception ex) { Console.Error.WriteLine($"[Dashboard] {SP_OBJECTED} failed for {rollSource}: {ex.Message}"); }

        try
        {
            var appeals = await conn.QueryAsync<AppealResult>(
                SP_APPEALS, new { userName = userId },
                commandType: CommandType.StoredProcedure, commandTimeout: 60);
            rollData.Appeals = appeals.ToList();
        }
        catch (Exception ex) { Console.Error.WriteLine($"[Dashboard] {SP_APPEALS} failed for {rollSource}: {ex.Message}"); }

        try
        {
            var notifications = await conn.QueryAsync<NotificationResult>(
                SP_NOTIFICATIONS, new { userEmail = userEmail },
                commandType: CommandType.StoredProcedure, commandTimeout: 60);
            rollData.Notifications = notifications.ToList();
        }
        catch (Exception ex) { Console.Error.WriteLine($"[Dashboard] {SP_NOTIFICATIONS} failed for {rollSource}: {ex.Message}"); }

        return rollData;
    }

    // ── Attributes linked properties ──────────────────────────────────
    // Calls Attr_DashboardLinked SP on GenesisAttributes DB.
    // Returns full property details + FormType + HasSubmission.
    public async Task<List<AttrLinkedPropertyResult>> GetAttributesLinkedAsync(
        string userId)
    {
        try
        {
            var connString = _config.GetConnectionString("AttributesConnection")!;
            await using var conn = new SqlConnection(connString);

            var rows = await conn.QueryAsync<AttrLinkedPropertyResult>(
                SP_ATTR_LINKED,
                new { UserName = userId },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);

            var results = rows.ToList();

            // Resolve FormType from CatDesc after query
            foreach (var r in results)
                r.FormType = ResolveFormType(r.CatDesc);

            return results;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Dashboard] {SP_ATTR_LINKED} failed for user {userId}: {ex.Message}");
            return new();
        }
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