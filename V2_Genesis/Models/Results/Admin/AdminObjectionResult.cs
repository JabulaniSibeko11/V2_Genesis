namespace V2_Genesis.Models.Results.Admin
{
    public class AdminObjectionResult
    {
        public string? Objection_No { get; set; }
        public string? Property_Type { get; set; }
        public string? Property_Desc { get; set; }
        public string? Town_Name { get; set; }
        public string? Old_Market_Value { get; set; }
        public string? Old_Category { get; set; }
        public string? Unit_key { get; set; }
        public string? Valuation_Key { get; set; }
        public string? objection_Status { get; set; }
        public string? PropertyFrom { get; set; }
    }

    public class AdminAppealResult
    {
        public string? Appeal_No { get; set; }
        public string? A_Property_Desc { get; set; }
        public string? A_Property_Type { get; set; }
        public string? Town_Name { get; set; }
        public string? Old_Market_Value { get; set; }
        public string? Old_Category { get; set; }
        public string? A_Unit_key { get; set; }
        public string? A_Valuation_Key { get; set; }
        public string? Appeal_Status { get; set; }
    }
}
