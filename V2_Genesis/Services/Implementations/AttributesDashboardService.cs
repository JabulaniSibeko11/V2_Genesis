using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

using V2_Genesis.Services.Attributes;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class AttributesDashboardService : IAttributesDashboardService
{
    private readonly string _connString;
    private readonly ILogger<AttributesDashboardService> _logger;

    public AttributesDashboardService(
        IConfiguration config,
        ILogger<AttributesDashboardService> logger)
    {
        _connString = config.GetConnectionString("AttributesConnection")!;
        _logger = logger;
    }

    public async Task<AttributesDashboardData> GetDashboardDataAsync(string userId)
    {
        var data = new AttributesDashboardData();

        try
        {
            await using var conn = new SqlConnection(_connString);

            // ── Linked properties ─────────────────────────────────────
            try
            {
                var linked = await conn.QueryAsync<AttributeLinkedProperty>(
                    "Attr_DashboardLinked",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);
                data.LinkedProperties = linked.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Attributes] Attr_DashboardLinked failed for {User}", userId);
            }

            // ── Submissions ───────────────────────────────────────────
            try
            {
                var subs = await conn.QueryAsync<AttributeSubmission>(
                    "Attr_DashboardSubmissions",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);
                data.Submissions = subs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Attributes] Attr_DashboardSubmissions failed for {User}", userId);
            }

            // ── Appointments ──────────────────────────────────────────
            try
            {
                var appts = await conn.QueryAsync<AttributeAppointment>(
                    "Attr_DashboardAppointments",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);
                data.Appointments = appts.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Attributes] Attr_DashboardAppointments failed for {User}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Attributes] Dashboard connection failed for {User}", userId);
        }

        return data;
    }
}