using V2_Genesis.Models;
using V2_Genesis.Models.Results.Rebates;

namespace V2_Genesis.Services.Interfaces
{
    public interface IRebatesService
    {
        Task<RebatesSubmitResult>SubmitAsync( string rebateType,
            string userId,
            string userEmail,
            Rebate_Info info,
            Rebate_Section1_PersonalDetails s1,
            Rebate_Section2_Addresses s2,
            Rebate_Section3_ContactDetails s3,
            Rebate_Section4_Ownership s4,
            Rebate_Section5_Declaration s5,
            Rebate_Section6_FI s6,
            Rebate_Section7_MinorOccupants s7,
            Rebate_Section8_ACS s8,
            Rebate_Section9_HeritageDetails s9,
            Rebate_Section10_Organisation s10,
            Rebate_Section11_SummaryIES s11,
            Rebates_Files files,
            List<IFormFile> evidenceFiles,
            List<IFormFile> attachedFiles);

        Task<List<Rebate_View_Model>> GetDashboardAsync(string userId);
        Task<List<Rebate_View_Model>> GetRebateDataAsync(string rebateNo);
        void WriteAcknoeledgement(RebatesSubmitResult result);
    }
}
