using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Results.Home;
using V2_Genesis.Models.ViewModels.Home;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

// ── Standalone result model — no inheritance, no naming assumptions ──


public class HomeSearchService : IHomeSearchService
{
    private readonly IPropertySearchService _searchService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HomeSearchService> _logger;
    private readonly string _defaultConn;

    private const string SP_TOWNSHIPS = "Objection.dbo.propertyDetailsTown";
    private const string SP_SCHEMES = "Objection.dbo.propertyDetailsScheme";
    private const string CACHE_KEY = "home_towns_schemes";

    private static readonly (string Source, string Name)[] Rolls =
    {
         ("Objection_Supp5", "Supplementary Roll 5"),
         ("Objection_Supp4", "Supplementary Roll 4"),
        ("Objection_Supp3", "Supplementary Roll 3"),
        ("Objection_Supp2", "Supplementary Roll 2"),
        ("Objection_Supp1", "Supplementary Roll 1"),
        ("Objection",       "General Valuation Roll"),
    };

    public HomeSearchService(
        IPropertySearchService searchService,
        IMemoryCache cache,
        ILogger<HomeSearchService> logger,
        IConfiguration config)
    {
        _searchService = searchService
            ?? throw new ArgumentNullException(nameof(searchService));
        _cache = cache;
        _logger = logger;
        _defaultConn = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection missing");
    }

    // ════════════════════════════════════════════════════════════════
    //  TOWNS + SCHEMES — same as PropertySearchService pattern
    // ════════════════════════════════════════════════════════════════
    public async Task<(List<string> Towns, List<string> Schemes)>
        GetTownsAndSchemesAsync()
    {
        if (_cache.TryGetValue(CACHE_KEY,
                out (List<string> T, List<string> S) hit)
            && hit.T.Count > 0)
            return (hit.T, hit.S);

        var towns = new List<string>();
        var schemes = new List<string>();

        try
        {
            await using var conn = new SqlConnection(_defaultConn);
            var rows = await conn.QueryAsync<string>(
                SP_TOWNSHIPS,
                commandType: CommandType.StoredProcedure);
            towns = rows
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .OrderBy(r => r)
                .ToList();
            _logger.LogInformation("[HomeSearch] {SP} → {N} towns",
                SP_TOWNSHIPS, towns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HomeSearch] {SP} failed", SP_TOWNSHIPS);
        }

        try
        {
            await using var conn = new SqlConnection(_defaultConn);
            var rows = await conn.QueryAsync<string>(
                SP_SCHEMES,
                commandType: CommandType.StoredProcedure);
            schemes = rows
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .OrderBy(r => r)
                .ToList();
            _logger.LogInformation("[HomeSearch] {SP} → {N} schemes",
                SP_SCHEMES, schemes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HomeSearch] {SP} failed", SP_SCHEMES);
        }

        if (towns.Count > 0 || schemes.Count > 0)
            _cache.Set(CACHE_KEY, (towns, schemes),
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });

        return (towns, schemes);
    }

    // ════════════════════════════════════════════════════════════════
    //  SEARCH ALL ROLLS
    //  Uses PropertySearchService.SearchAsync — same SPs as PropertyIndex
    // ════════════════════════════════════════════════════════════════
    public async Task<List<HomeSearchResult>> SearchAllRollsAsync(
        HomeSearchParams p)
    {
        if (p is null)
        {
            _logger.LogWarning("[HomeSearch] SearchAllRollsAsync called with null params");
            return new();
        }

        var searchParams = new PropertySearchParams
        {
            TownName = p.SearchTownName?.Trim() ?? string.Empty,
            Stand = string.IsNullOrWhiteSpace(p.SearchStand) ? null : p.SearchStand.Trim(),
            Address = string.IsNullOrWhiteSpace(p.SearchAddress) ? null : p.SearchAddress.Trim(),
            Scheme = string.IsNullOrWhiteSpace(p.SearchScheme) ? null : p.SearchScheme.Trim(),
            Unit = string.IsNullOrWhiteSpace(p.SearchUnit) ? null : p.SearchUnit.Trim(),
        };

        var combined = new List<HomeSearchResult>();

        foreach (var roll in Rolls)
        {
            try
            {
                var results = await _searchService.SearchAsync(
                    roll.Source, searchParams);

                if (results is null || !results.Any())
                {
                    _logger.LogDebug("[HomeSearch] {Roll} → 0 results", roll.Source);
                    continue;
                }

                foreach (var r in results)
                {
                    if (r is null) continue;

                    combined.Add(new HomeSearchResult
                    {
                        RollSource = roll.Source,
                        RollName = roll.Name,
                        // Use safe property access — matches PropertySearchResult fields
                        TownNameDesc = r.TownNameDesc,
                        LisStreetAddress = r.LisStreetAddress,
                        Erf = r.Erf,
                        Ptn = r.Ptn,
                        Re = r.Re,
                        CatDesc = r.CatDesc,
                        RateableArea = r.RateableArea,
                        MarketValue = r.MarketValue,
                        SchemeName = r.SchemeName,
                        SchemeNumber = r.SchemeNumber,
                        SchemeYear = r.SchemeYear,
                        Lease = r.Lease,
                        UnitNo = r.UnitNo,
                        Reason = r.Reason,
                        UnitKey = r.UnitKey,
                        ValuationKey = r.ValuationKey,
                    });
                }

                _logger.LogDebug("[HomeSearch] {Roll} → {N} results",
                    roll.Source, results.Count);

                if (combined.Count >= 200) break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[HomeSearch] {Roll} failed — skipping", roll.Source);
            }
        }

        _logger.LogInformation("[HomeSearch] Total: {N} results", combined.Count);
        return combined;
    }
}