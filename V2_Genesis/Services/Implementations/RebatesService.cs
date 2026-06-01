using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Results.Rebates;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class RebatesService : IRebatesService
    {
        private readonly RebateDBContext _db;
        private readonly IConfiguration _config;
        private readonly string _connStr;
        private readonly EmailService _email;

        public RebatesService(RebateDBContext db, IConfiguration config, EmailService email) { 
        _db= db;
            _config= config;
            _email= email;
            _connStr = config.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("RebatesConnection missing.");
        }
        public async Task<RebatesSubmitResult> SubmitAsync(string rebateType, string userId, string userEmail,
            Rebate_Info info, Rebate_Section1_PersonalDetails s1, Rebate_Section2_Addresses s2,
            Rebate_Section3_ContactDetails s3,
            Rebate_Section4_Ownership s4,
            Rebate_Section5_Declaration s5,
            Rebate_Section6_FI s6,
            Rebate_Section7_MinorOccupants s7,
            Rebate_Section8_ACS s8,
            Rebate_Section9_HeritageDetails s9,
            Rebate_Section10_Organisation s10,
            Rebate_Section11_SummaryIES s11,
            Rebates_Files rebates_Files,
            List<IFormFile> evidenceFiles,
            List<IFormFile> attachedFiles)
        {

            //1. Status determination (mirrors V1 logic exactly)
            info.Status = DetermineStatus(rebateType, s1, s10);
            info.UserID = userId;
            info.Rebate_Type = rebateType;
            _db.Rebate_Infos.Add(info);
            await _db.SaveChangesAsync();


            long id = info.Rebate_ID;

            s1.Ref = id;
            _db.Rebate_Section1_PersonalDetails.Add(s1);
            await _db.SaveChangesAsync();

            s2.Ref = id;
            _db.Rebate_Section2_Addresses.Add(s2);
            await _db.SaveChangesAsync();

            s3.Ref = id;
            _db.Rebate_Section3_ContactDetails.Add(s3);
            await _db.SaveChangesAsync();

            s4.Ref= id;
            _db.Rebate_Section4_Ownerships.Add(s4);
            await _db.SaveChangesAsync();

       

            s6.Ref = id;
            _db.Rebate_Section6_FI.Add(s6);
            await _db.SaveChangesAsync();

            s7.Ref = id;
            _db.Rebate_Section7_MinorOccupants.Add(s7);
            await _db.SaveChangesAsync();

            s8.Ref = id;
            _db.Rebate_Section8_ACSs.Add(s8);
            await _db.SaveChangesAsync();

            s9.Ref = id;
            _db.Rebate_Section9_HeritageDetails.Add(s9);
            await _db.SaveChangesAsync();

            s10.Ref = id;
            _db.Rebate_Section10_Organisations.Add(s10);
            await _db.SaveChangesAsync();

            s11.Ref= id;
            _db.Rebate_Section11_Summaries.Add(s11);
                        await _db.SaveChangesAsync();

            s5.DateOfSubmission = DateTime.UtcNow;
            s5.Ref = id;
            _db.Rebate_Section5_Declarations.Add(s5);
            await _db.SaveChangesAsync();

            string RebateNo = info.Rebate_No ?? id.ToString();

            string uploadRoot = _config["Rebates:RebateRooTPath"] ?? throw new InvalidOperationException("RebateRooTPath missing.");
            string folder = Path.Combine(uploadRoot, RebateNo);
            if (!Directory.Exists(folder)) { 
            Directory.CreateDirectory(folder);

                foreach (var file in attachedFiles) { 
                 var sub= Path.Combine(folder, "Attached File");
                }
            
            }

        }



    }

}

