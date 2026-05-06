
using V2_Genesis.Models;

namespace V2_Genesis.Services.Interfaces
{
    public interface IPropertySearchService
    {
        /// <summary>Load townships (shared across all rolls — loaded once).</summary>
        Task<List<string>> GetTownshipsAsync();

        /// <summary>Load scheme names (shared across all rolls — loaded once).</summary>
        Task<List<string>> GetSchemesAsync();

        /// <summary>
        /// Execute property search for the given roll.
        /// Returns empty list if rollSource not found in registry.
        /// </summary>
        Task<List<PropertySearchResult>> SearchAsync(
            string rollSource,
            PropertySearchParams @params);
    }
}