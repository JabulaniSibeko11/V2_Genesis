namespace V2_Genesis.Models.Results
{
    public class AppealResult
    {
        // SP returns these column names — match exactly
        public string? Appeal_No { get; set; }
        public string? Town_Name { get; set; }
        public string? Old_Market_Value { get; set; }
        public string? Old_Category { get; set; }
        public string? A_Property_Desc { get; set; }
        public string? A_Unit_key { get; set; }
        public string? A_Valuation_Key { get; set; }
        public string? A_Property_Type { get; set; }
        public string? Appeal_Status { get; set; }

        // View-friendly aliases
        public string? Objection_No => Appeal_No;
        public string? Property_Desc => A_Property_Desc;
        public string? Unit_key => A_Unit_key;
        public string? Valuation_Key => A_Valuation_Key;
        public string? Property_Type => A_Property_Type;
        public string? objection_Status => Appeal_Status;
    }
}
