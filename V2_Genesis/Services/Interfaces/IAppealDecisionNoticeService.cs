namespace V2_Genesis.Services.Interfaces;

public interface IAppealDecisionNoticeService
{
    Task<(byte[] Pdf, string FileName)> GenerateAsync(
        string rollSource,
        string referenceNumber,
        string userId,
        CancellationToken cancellationToken = default);
}
