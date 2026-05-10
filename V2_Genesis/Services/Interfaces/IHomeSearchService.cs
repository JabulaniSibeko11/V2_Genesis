using V2_Genesis.Models.Results.Home;    // ← must match
using V2_Genesis.Models.ViewModels.Home;

namespace V2_Genesis.Services.Interfaces;

public interface IHomeSearchService
{
    Task<(List<string> Towns, List<string> Schemes)> GetTownsAndSchemesAsync();
    Task<List<HomeSearchResult>> SearchAllRollsAsync(HomeSearchParams p);
}