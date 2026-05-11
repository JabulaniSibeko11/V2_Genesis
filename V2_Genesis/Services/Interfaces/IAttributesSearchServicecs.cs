using V2_Genesis.Models;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAttributesSearchService
    {
        Task<List<PropertySearchResult>> SearchAsync(PropertySearchParams p);
        Task<LinkResult> LinkPropertyAsync(string idProperty, string userId, string propertyFrom);
    }
}
