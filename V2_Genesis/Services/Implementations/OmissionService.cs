using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Omission;

namespace V2_Genesis.Services.Implementations
{
    public class OmissionService : IOmissionService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<OmissionService> _logger;
        private readonly IReadOnlyDictionary<string, OmissionRollConfig> _registry;

        public OmissionService(
            IConfiguration config,
            ILogger<OmissionService> logger)
        {
            _config = config;
            _logger = logger;
            _registry = OmissionRollRegistry.Build();
        }

        public async Task<List<string>> GetTownsAsync(string rollSource)
        {
            if (!_registry.TryGetValue(rollSource, out var cfg))
                return new();

            try
            {
                var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
                await using var conn = new SqlConnection(connStr);

                var rows = await conn.QueryAsync(
                    cfg.TownSp,
                    commandType: CommandType.StoredProcedure);

                return rows
                    .Select(r =>
                    {
                        var d = (IDictionary<string, object>)r;
                        return d.TryGetValue("Town_Name_Description", out var v)
                            ? v?.ToString() ?? "" : "";
                    })
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .OrderBy(t => t)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Omission] GetTowns failed for {Roll}", rollSource);
                return new();
            }
        }

        public async Task<List<string>> GetSchemesAsync(string rollSource)
        {
            if (!_registry.TryGetValue(rollSource, out var cfg))
                return new();

            try
            {
                var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
                await using var conn = new SqlConnection(connStr);

                var rows = await conn.QueryAsync(
                    cfg.SchemeSp,
                    commandType: CommandType.StoredProcedure);

                return rows
                    .Select(r =>
                    {
                        var d = (IDictionary<string, object>)r;
                        return d.TryGetValue("Scheme_Name", out var v)
                            ? v?.ToString() ?? "" : "";
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .OrderBy(s => s)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Omission] GetSchemes failed for {Roll}", rollSource);
                return new();
            }
        }
    }
}
