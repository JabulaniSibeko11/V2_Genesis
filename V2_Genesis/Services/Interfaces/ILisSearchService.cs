using V2_Genesis.Models.Lis;
using V2_Genesis.Models.LIS;

namespace V2_Genesis.Services.Interfaces
{
    public interface ILisSearchService
    {
        Task<List<LisProperty>> SearchAsync(
        string rollSource,
        LisSearchParams p,
        CancellationToken cancellationToken = default);

        Task<List<LisProperty>> GetTownSchemesAsync(string rollSource);

        Task<LisProperty?> GetPropertyByKeysAsync(
            string rollSource,
            string unitKey,
            string valuationKey,
            CancellationToken cancellationToken = default);
    }
}
