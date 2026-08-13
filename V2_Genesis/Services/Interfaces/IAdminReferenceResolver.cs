using V2_Genesis.Models.Results.Admin;

namespace V2_Genesis.Services.Interfaces;

public interface IAdminReferenceResolver
{
    Task<AdminEnquiryFoundation?> ResolveAsync(
        string referenceNumber,
        string? rollSource,
        CancellationToken cancellationToken = default);
}
