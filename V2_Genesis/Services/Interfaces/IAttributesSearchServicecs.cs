using V2_Genesis.Models;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAttributesSearchService
    {
        /// Search uses AttributesConnection, same SP names as GV PropertyIndex
        Task<List<PropertySearchResult>> SearchAsync(PropertySearchParams p);

        /// Link inserts into Attributes DB via Attr_InsertLinkedProperty
        Task<LinkResult> LinkPropertyAsync(
            string idProperty,
            string userId,
            string propertyFrom);
    }
}
