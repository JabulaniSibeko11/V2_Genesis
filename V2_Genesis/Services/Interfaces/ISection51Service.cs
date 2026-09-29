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

        /// Section 51 notice to the property owner when a Third-Party
        /// objection is submitted (PDF, email + .eml, Section51Table,
        /// Obj_Property_Info.Section51_Emailed). Never throws.
        Task<Section51NoticeResult> SendThirdPartyNoticeAsync(Section51NoticeRequest request);
    }
}
