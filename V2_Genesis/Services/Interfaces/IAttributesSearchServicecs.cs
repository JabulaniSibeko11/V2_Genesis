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

        Task<LinkResult> LinkPropertyAsync(
     string idProperty,
     string userId,
     string propertyFrom,
     string? verifiedAccountNumber = null);

        Task<LisPropertyDetail?> GetPropertyDetailAsync(string unitKey);

        Task<bool> VerifyAccountStatementPinAsync(
    string unitKey,
    string accountNumber,
    string statementPin,
    CancellationToken cancellationToken = default);

      



    }
}
