using V2_Genesis.Models.Results.Admin;

namespace V2_Genesis.Services.Interfaces;

public interface IAdminPropertyLookupService
{
    Task<AdminSearchResult> SearchAsync(
        string? town,
        string? stand,
        string? address,
        string? scheme,
        string? unit,
        string? rollSource,
        CancellationToken cancellationToken = default);
}
