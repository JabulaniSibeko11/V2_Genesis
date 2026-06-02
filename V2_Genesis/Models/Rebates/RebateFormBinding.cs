namespace V2_Genesis.Models.Rebates
{
    public class RebateFormBinding
    {
        public Rebate_Info Info { get; set; } = new();
        public Rebate_Section1_PersonalDetails S1 { get; set; } = new();
        public Rebate_Section2_Addresses S2 { get; set; } = new();
        public Rebate_Section3_ContactDetails S3 { get; set; } = new();
        public Rebate_Section4_Ownership S4 { get; set; } = new();
        public Rebate_Section5_Declaration S5 { get; set; } = new();
        public Rebate_Section6_FI S6 { get; set; } = new();
        public Rebate_Section7_MinorOccupants S7 { get; set; } = new();
        public Rebate_Section8_ACS S8 { get; set; } = new();
        public Rebate_Section9_HeritageDetails S9 { get; set; } = new();
        public Rebate_Section10_Organisation S10 { get; set; } = new();
        public Rebate_Section11_SummaryIES S11 { get; set; } = new();
        public Rebates_Files Files { get; set; } = new();
    }
}
