using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Models.Admin;
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Services.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(IConfiguration config,
        ILogger<AdminDashboardService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private SqlConnection GetConn(string rollSource)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            throw new InvalidOperationException($"Unknown roll: {rollSource}");
        var cs = _config.GetConnectionString(cfg.ConnectionKey)!;
        return new SqlConnection(cs);
    }

    // ── Stats ─────────────────────────────────────────────────────────
    public async Task<AdminRollStats> GetStatsAsync(string rollSource)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new AdminRollStats { RollSource = rollSource };

        try
        {
            await using var conn = GetConn(rollSource);
            var result = await conn.QueryFirstOrDefaultAsync<AdminRollStats>(
                cfg.StatsSp,
                commandType: CommandType.StoredProcedure);

            if (result is not null)
            {
                result.RollSource = rollSource;
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] Stats SP failed for {Roll}", rollSource);
        }

        return new AdminRollStats { RollSource = rollSource };
    }

    // ── Search Objections ─────────────────────────────────────────────
    public async Task<List<AdminObjectionResult>> SearchObjectionsAsync(
        string rollSource, string searchValue)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new();

        try
        {
            await using var conn = GetConn(rollSource);
            var results = await conn.QueryAsync<AdminObjectionResult>(
                cfg.ObjSearchSp,
                new { SearchValue = searchValue },
                commandType: CommandType.StoredProcedure);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] Search SP failed for {Roll}", rollSource);
            return new();
        }
    }

    // ── Search Appeals ────────────────────────────────────────────────
    public async Task<List<AdminAppealResult>> SearchAppealsAsync(
        string rollSource, string searchValue)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new();

        // Determine SP: appeal no search vs property search
        bool isAppeal = searchValue.Trim().ToUpper().StartsWith("APP");
        var sp = isAppeal ? cfg.AppSearchSp : cfg.PropSearchSp;

        try
        {
            await using var conn = GetConn(rollSource);
            var results = await conn.QueryAsync<AdminAppealResult>(
                sp,
                new { searchAppealValue = searchValue },
                commandType: CommandType.StoredProcedure);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] Appeal search SP failed for {Roll}", rollSource);
            return new();
        }
    }
}