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
        public DateTime? Appeal_Start_DateTime { get; set; }

        // Calculated by SQL Server using the same database clock that
        // stores Appeal_Start_DateTime.
        public DateTime? Evidence_Expires_At { get; set; }
        public bool Evidence_Window_Open { get; set; }

        public DateTime? EvidenceExpiresAt =>
            Evidence_Expires_At
            ?? Appeal_Start_DateTime?.AddHours(48);

        public bool IsEvidenceWindowOpen =>
            Evidence_Window_Open;

        // View-friendly aliases
        public string? Objection_No => Appeal_No;
        public string? Property_Desc => A_Property_Desc;
        public string? Unit_key => A_Unit_key;
        public string? Valuation_Key => A_Valuation_Key;
        public string? Property_Type => A_Property_Type;
        public string? objection_Status => Appeal_Status;
    }
}
