namespace V2_Genesis.Services.Interfaces;

public interface ISection53NoticeService
{
    Task<(byte[] Pdf, string FileName)> GenerateAsync(
        string rollSource,
        string objectionNo,
        string userId,
        CancellationToken cancellationToken = default);
}
