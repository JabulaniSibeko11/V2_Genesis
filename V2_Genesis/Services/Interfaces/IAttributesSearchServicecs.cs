using V2_Genesis.Models;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Services;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAttributesSearchService
    {
        Task<List<PropertySearchResult>> SearchAsync(PropertySearchParams p);

        Task<List<string>> GetTownshipsAsync();
        Task<List<string>> GetSchemesAsync();

        Task<LinkResult> LinkPropertyAsync(string idProperty, string userId, string propertyFrom);
        Task<LisPropertyDetail?> GetPropertyDetailAsync(string unitKey);


      
    }
}
