
using V2_Genesis.Models;
using V2_Genesis.Models.Results;

namespace V2_Genesis.Services.Interfaces
{
    public interface IPropertySearchService
    {

        Task<List<string>> GetTownshipsAsync(string? rollSource = null);


        Task<List<string>> GetSchemesAsync();


        Task<List<PropertySearchResult>> SearchAsync(
            string rollSource,
            PropertySearchParams searchParams);


        Task<List<PropertyDetailResult>> GetPropertyDetailsAsync(
            string rollSource,
            string unitKey,
            string valuationKey);


        Task<LinkResult> LinkPropertyAsync(
            string rollSource,
            string idProperty,
            string userId,
            string propertyFrom);
    }
}
