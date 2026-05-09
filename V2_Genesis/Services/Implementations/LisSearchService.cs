using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Models.Lis;
using V2_Genesis.Models.LIS;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Lis;

namespace V2_Genesis.Services.Implementations
{
    public class LisSearchService : ILisSearchService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<LisSearchService> _logger;
        private readonly IReadOnlyDictionary<string, LisRollConfig> _registry;

        public LisSearchService(
            IConfiguration config,
            ILogger<LisSearchService> logger)
        {
            _config = config;
            _logger = logger;
            _registry = LisRollRegistry.Build();
        }

        // ── Main search — picks the right SP based on params ─────────────
        public async Task<List<LisProperty>> SearchAsync(
            string rollSource, LisSearchParams p)
        {
            if (!_registry.TryGetValue(rollSource, out var cfg))
                return new();

            var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;

            // Normalise — null = not provided, empty string treated as null
            var town = Normalise(p.SearchTownName);
            var stand = Normalise(p.SearchStand);
            var address = Normalise(p.SearchAddress);
            var scheme = Normalise(p.SearchScheme);
            var unit = Normalise(p.SearchUnit);

            var (spName, parms) = PickSp(cfg, town, stand, address, scheme, unit);

            try
            {
                await using var conn = new SqlConnection(connStr);
                var rows = await conn.QueryAsync(
                    spName, parms,
                    commandType: CommandType.StoredProcedure);

                return rows.Select(MapRow).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[LIS] Search failed on {Roll} using SP {Sp}", rollSource, spName);
                return new();
            }
        }

        // ── Town/scheme dropdown ──────────────────────────────────────────
        public async Task<List<LisProperty>> GetTownSchemesAsync(string rollSource)
        {
            if (!_registry.TryGetValue(rollSource, out var cfg))
                return new();

            var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
            try
            {
                await using var conn = new SqlConnection(connStr);
                var rows = await conn.QueryAsync(
                    cfg.TownAndScheme, commandType: CommandType.StoredProcedure);
                return rows.Select(MapRow).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[LIS] GetTownSchemes failed on {Roll}", rollSource);
                return new();
            }
        }

        // ── SP selection logic ────────────────────────────────────────────
        // Mirrors the V1 if/else chain exactly, using named parameters
        private static (string SpName, object Params) PickSp(
            LisRollConfig cfg,
            string? town, string? stand,
            string? address, string? scheme, string? unit)
        {
            // Scheme + Unit
            if (scheme != null && unit != null)
                return (cfg.SchemeUnit, new
                {
                    SearchTownName = Like(town),
                    SearchScheme = Like(scheme),
                    SearchUnit = Like(unit)
                });

            // Stand + Address + no Scheme
            if (stand != null && address != null && scheme == null)
                return (cfg.TownStandAddress, new
                {
                    SearchTownName = Like(town),
                    SearchStand = Like(stand),
                    SearchAddress = Like(address)
                });

            // Stand + Scheme + no Address
            if (stand != null && scheme != null && address == null)
                return (cfg.TownErfScheme, new
                {
                    SearchTownName = Like(town),
                    SearchStand = Like(stand),
                    SearchScheme = Like(scheme)
                });

            // Address + Scheme + no Stand
            if (address != null && scheme != null && stand == null)
                return (cfg.TownAddressScheme, new
                {
                    SearchTownName = Like(town),
                    SearchAddress = Like(address),
                    SearchScheme = Like(scheme)
                });

            // Stand only
            if (stand != null && address == null && scheme == null)
                return (cfg.TownStand, new
                {
                    SearchTownName = Like(town),
                    SearchStand = Like(stand)
                });

            // Scheme only
            if (scheme != null && stand == null && address == null)
                return (cfg.TownScheme, new
                {
                    SearchTownName = Like(town),
                    SearchScheme = Like(scheme)
                });

            // Address only
            if (address != null && stand == null && scheme == null)
                return (cfg.TownAddress, new
                {
                    SearchTownName = Like(town),
                    SearchAddress = Like(address)
                });

            // Unit only
            if (unit != null && stand == null && address == null && scheme == null)
                return (cfg.TownUnit, new
                {
                    SearchTownName = Like(town),
                    SearchUnit = Like(unit)
                });

            // Town only / fallback
            return (cfg.TownOnly, new { SearchTownName = Like(town) });
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static string? Normalise(string? val)
            => string.IsNullOrWhiteSpace(val) ? null : val.Trim();

        // Wraps value in %…% wildcard for LIKE, empty string if null
        private static string Like(string? val)
            => string.IsNullOrWhiteSpace(val) ? "%%" : $"%{val.Trim()}%";

        private static LisProperty MapRow(dynamic dr)
        {
            var d = (IDictionary<string, object>)dr;
            T Get<T>(string key) =>
                d.TryGetValue(key, out var v) && v is not DBNull ? (T)Convert.ChangeType(v, typeof(T)) : default!;

            return new LisProperty
            {
                TownNameDescription = Get<string>("TownNameDescription"),
                Erf = Get<int>("Erf"),
                Ptn = Get<int>("Ptn"),
                LisStreetAddress = Get<string>("LisStreetAddress"),
                Reason = Get<string>("Reason"),
                SchemeName = Get<string>("Schemename"),
                SchemeNumber = Get<string>("Scheme_Number"),
                UnitNo = Get<string>("UnitNo"),
                SchemeYear = Get<string>("SchemeYear"),
                UnitKey = Get<string>("UnitKey"),
                ValuationKey = Get<string>("ValuationKey"),
                MarketValue = Get<string>("MarketValue"),
                CATDescription = Get<string>("CATDescription"),
                RateableArea = Get<string>("RateableArea"),
                ValuationEffectiveDateWefDate = Get<string>("ValuationEffectiveDateWefDate"),
                AdditionalNotes = Get<string>("AdditionalNotes"),
                Re = Get<string>("Re"),
                ValuationEndDate = Get<string>("ValuationEndDate"),
                Lease = Get<string>("LeaseStatus"),
            };
        }
    }

}
