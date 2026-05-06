using Dapper;
using GenesisV2.Services.PropertySearch;
using Microsoft.Data.SqlClient;
using System.Data;

using V2_Genesis.Models.Results;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IConfiguration _config;

    // SP names are the same across all roll databases
    private const string SP_LINKED = "DashboardLinked";
    private const string SP_OBJECTED = "DashboardObjection";
    private const string SP_APPEALS = "DashboardAppeal";
    private const string SP_NOTIFICATIONS = "DashboardNotification";

    public DashboardService(IConfiguration config)
        => _config = config;

    public async Task<RollData> GetRollDataAsync(
        string rollSource,
        string userId,
        string userEmail)
    {
        var rollData = new RollData();

        // Resolve the roll's connection string from registry
        if (!RollSearchRegistry.Configs.TryGetValue(rollSource, out var config))
            return rollData;

        var connString = _config.GetConnectionString(config.ConnectionKey)
                         ?? _config.GetConnectionString("DefaultConnection")!;

        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync();

        // ── 1. Linked Properties ─────────────────────────────────────
        try
        {
            var linked = await conn.QueryAsync<LinkedPropertyResult>(
                SP_LINKED,
                new { userName = userId },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);

            rollData.LinkedProperties = linked.ToList();
        }
        catch (Exception ex)
        {
            // Log and continue — don't break whole dashboard for one roll
            Console.Error.WriteLine(
                $"[Dashboard] {SP_LINKED} failed for {rollSource}: {ex.Message}");
        }

        // ── 2. Objections ─────────────────────────────────────────────
        try
        {
            var objected = await conn.QueryAsync<ObjectedPropertyResult>(
                SP_OBJECTED,
                new { userName = userId },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);

            rollData.ObjectedProperties = objected.ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Dashboard] {SP_OBJECTED} failed for {rollSource}: {ex.Message}");
        }

        // ── 3. Appeals ────────────────────────────────────────────────
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

        // ── 4. Notifications (uses email, not userId) ─────────────────
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
}