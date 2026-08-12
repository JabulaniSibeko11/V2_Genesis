using V2_Genesis.Models.Results.Acknowledgement;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAcknowledgementDownloadService
    {
        Task<GeneratedAcknowledgementResult> GenerateAsync(
        string referenceNumber,
        string? rollSource,
        string userId,
        bool allowAdministrativeAccess = false,
        CancellationToken cancellationToken = default);
    }
}
