using V2_Genesis.Models;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.ViewModels.Attributes;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAttributesSearchService
    {
        Task<List<PropertySearchResult>> SearchAsync(PropertySearchParams p);
        Task<LinkResult> LinkPropertyAsync(string idProperty, string userId, string propertyFrom);
        Task<LisPropertyDetail?> GetPropertyDetailAsync(string unitKey);


      
    }
}
