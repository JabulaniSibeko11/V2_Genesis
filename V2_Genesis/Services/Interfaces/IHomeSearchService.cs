using V2_Genesis.Models.Results.Home;
using V2_Genesis.Models.ViewModels.Home;

namespace V2_Genesis.Services.Interfaces
{
    public interface IHomeSearchService
    {
       
        Task<List<HomeSearchResult>> SearchAllRollsAsync(HomeSearchParams p);
        Task<(List<string> Towns, List<string> Schemes)> GetTownsAndSchemesAsync();
    }
}
