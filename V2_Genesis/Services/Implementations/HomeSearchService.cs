using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using V2_Genesis.Models.Lis;
using V2_Genesis.Models.LIS;
using V2_Genesis.Models.Results.Home;
using V2_Genesis.Models.ViewModels.Home;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class HomeSearchService : IHomeSearchService
{
    private readonly IConfiguration _config;
    private readonly ILisSearchService _lisService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HomeSearchService> _logger;

    // SP names only — no 3-part qualifier.
    // CommandType.StoredProcedure + DefaultConnection targets Objection DB.
    private const string SP_TOWNS = "propertyDetailsTown";
    private const string SP_SCHEMES = "propertyDetailsScheme";
    private const string CACHE_KEY = "home_towns_schemes";

    private static readonly (string Source, string Name)[] Rolls =
    {
        ("Objection_Supp3", "Supplementary Roll 3"),
        ("Objection_Supp2", "Supplementary Roll 2"),
        ("Objection_Supp1", "Supplementary Roll 1"),
        ("Objection",       "General Valuation Roll"),
    };

    public HomeSearchService(
        IConfiguration config,
        ILisSearchService lisService,
        IMemoryCache cache,
        ILogger<HomeSearchService> logger)
    {
        _config = config;
        _lisService = lisService;
        _cache = cache;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════
    //  GET TOWNS + SCHEMES
    //  Two separate SP calls on the Objection (DefaultConnection) DB
    // ════════════════════════════════════════════════════════════════
    public async Task<(List<string> Towns, List<string> Schemes)>
        GetTownsAndSchemesAsync()
    {
        // Return cached if available
        if (_cache.TryGetValue(CACHE_KEY,
                out (List<string> Towns, List<string> Schemes) hit)
            && hit.Towns.Count > 0)
        {
            _logger.LogDebug("[HomeSearch] Cache hit — {T} towns {S} schemes",
                hit.Towns.Count, hit.Schemes.Count);
            return hit;
        }

        var connStr = _config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            _logger.LogError("[HomeSearch] DefaultConnection missing from config");
            return (new(), new());
        }

        var towns = new List<string>();
        var schemes = new List<string>();

        // Re-use one open connection for both calls
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        // ── Towns via propertyDetailsTown ─────────────────────────────
        try
        {
            var rows = await conn.QueryAsync(
                SP_TOWNS,
                commandType: System.Data.CommandType.StoredProcedure,
                commandTimeout: 30);

            foreach (var row in rows)
            {
                var d = (IDictionary<string, object>)row;
                if (d.TryGetValue("Town_Name_Description", out var v)
                    && v is not DBNull
                    && !string.IsNullOrWhiteSpace(v.ToString()))
                {
                    towns.Add(v.ToString()!.Trim());
                }
            }

            towns = towns
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList();

            _logger.LogInformation("[HomeSearch] {SP} returned {N} towns",
                SP_TOWNS, towns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HomeSearch] {SP} failed", SP_TOWNS);
        }

        // ── Schemes via propertyDetailsScheme ─────────────────────────
        try
        {
            var rows = await conn.QueryAsync(
                SP_SCHEMES,
                commandType: System.Data.CommandType.StoredProcedure,
                commandTimeout: 30);

            foreach (var row in rows)
            {
                var d = (IDictionary<string, object>)row;
                if (d.TryGetValue("Scheme_Name", out var v)
                    && v is not DBNull
                    && !string.IsNullOrWhiteSpace(v.ToString()))
                {
                    schemes.Add(v.ToString()!.Trim());
                }
            }

            schemes = schemes
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            _logger.LogInformation("[HomeSearch] {SP} returned {N} schemes",
                SP_SCHEMES, schemes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HomeSearch] {SP} failed", SP_SCHEMES);
        }

        var result = (towns, schemes);

        // Only cache when we actually got data
        if (towns.Count > 0 || schemes.Count > 0)
        {
            _cache.Set(CACHE_KEY, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
                SlidingExpiration = TimeSpan.FromMinutes(30)
            });
        }

        return result;
    }

    // ════════════════════════════════════════════════════════════════
    //  SEARCH ALL ROLLS — sequential, early exit at 200 rows
    // ════════════════════════════════════════════════════════════════
    public async Task<List<HomeSearchResult>> SearchAllRollsAsync(HomeSearchParams p)
    {
        var lisP = new LisSearchParams
        {
            SearchTownName = p.SearchTownName?.Trim(),
            SearchStand = p.SearchStand?.Trim(),
            SearchAddress = p.SearchAddress?.Trim(),
            SearchScheme = p.SearchScheme?.Trim(),
            SearchUnit = p.SearchUnit?.Trim(),
        };

        var combined = new List<HomeSearchResult>();

        foreach (var roll in Rolls)
        {
            try
            {
                var results = await _lisService.SearchAsync(roll.Source, lisP);

                combined.AddRange(results.Select(r => new HomeSearchResult
                {
                    RollSource = roll.Source,
                    RollName = roll.Name,
                    TownNameDesc = r.TownNameDescription,
                    LisStreetAddress = r.LisStreetAddress,
                    Erf = r.Erf,
                    Ptn = r.Ptn,
                    Re = r.Re,
                    CatDesc = r.CATDescription,
                    RateableArea = r.RateableArea,
                    MarketValue = r.MarketValue,
                    SchemeName = r.SchemeName,
                    SchemeNumber = r.SchemeNumber,
                    SchemeYear = r.SchemeYear,
                    Lease = r.Lease,
                    UnitNo = int.TryParse(r.UnitNo, out var u) ? u : 0,
                    Reason = r.Reason,
                    UnitKey = r.UnitKey,
                    ValuationKey = r.ValuationKey,
                }));

                if (combined.Count >= 200) break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[HomeSearch] Roll {Roll} failed — skipping", roll.Source);
            }
        }

        return combined;
    }
}