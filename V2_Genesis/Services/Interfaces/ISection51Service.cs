using V2_Genesis.Models.Section51;

namespace V2_Genesis.Services.Interfaces
{
    public interface ISection51Service
    {
        Task<Section51ValidateResult> ValidateAsync(
            string rollSource, string objectionNo, string pin);

        Task<(bool Success, string? Error, int FileCount, List<string> FileNames)>
            UploadAsync(
                string rollSource,
                string objectionNo,
                List<IFormFile> files);
    }
}
