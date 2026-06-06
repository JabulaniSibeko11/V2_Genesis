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
                        return d.TryGetValue("TsOnlyName", out var v)
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
                        return d.TryGetValue("SchemeName", out var v)
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

        public (string PropertyDesc, string SourceTable, string ControllerName)
            BuildOmissionDescription(
                string rollSource,
                string propType,
                string? town,
                string? erf,
                string? portion,
                string? re,
                string? right,
                string? scheme,
                string? schemeNumber,
                string? schemeYear,
                string? unit,
                string? stRight)
        {
            // ── 1. Derive target controller and sourceTable ────────────────────
            // This is the KEY fix — maps rollSource → correct roll DB / controller
            var controllerName = ObjectionService.RollSourceToController
                                     .GetValueOrDefault(rollSource, "Sup3");
            var sourceTable = ObjectionService.RollSourceToSourceTable
                                     .GetValueOrDefault(rollSource, "GV23-SUP3");

            // ── 2. Build PropertyDesc matching V1 format ──────────────────────
            string propertyDesc;

            if (propType == "ST")
            {
                // {Right}{Scheme} ({SchemeNum}/{SchemeYear}), Unit {Unit}, {Town}
                var r = stRight?.Trim() ?? "";
                var s = scheme?.Trim() ?? "";
                var n = schemeNumber?.Trim() ?? "";
                var y = schemeYear?.Trim() ?? "";
                var u = unit?.Trim() ?? "";
                var t = town?.Trim() ?? "";
                propertyDesc = $"{r}{s} ({n}/{y}), Unit {u}, {t}".Trim();
            }
            else
            {
                // Freehold: {Right}{Town} Erf {ERF} Portion {Portion} RE   (RE == "RE")
                //           {Right}{Town} Erf {ERF} Portion {Portion}      (RE == "00")
                var r = right?.Trim() ?? "";
                var t = town?.Trim() ?? "";
                var e = erf?.Trim() ?? "";
                var p = string.IsNullOrWhiteSpace(portion) ? "0" : portion.Trim();
                var rev = re?.Trim() ?? "";

                propertyDesc = rev == "RE"
                    ? $"{r}{t} Erf {e} Portion {p} RE".Trim()
                    : $"{r}{t} Erf {e} Portion {p}".Trim();
            }

            return (propertyDesc, sourceTable, controllerName);
        }
    }
}
