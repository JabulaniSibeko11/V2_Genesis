using V2_Genesis.Models.Section78;

namespace V2_Genesis.Models.Results
{

    public class ObjectedPropertyResult
    {
        public string? Objection_No { get; set; }

        public string? Property_Type { get; set; }

        public string? Town_Name { get; set; }

        public string? Old_Market_Value { get; set; }

        public string? Old_Category { get; set; }

        public string? New_Market_Value_MVD { get; set; }

        public string? New_Category_MVD { get; set; }

        public string? Property_Desc { get; set; }

        public string? Unit_key { get; set; }

        public string? Valuation_Key { get; set; }

        public string? objection_Status { get; set; }

        public string? PropertyFrom { get; set; }

        public int Sub_typ { get; set; }

        public string? Appeal_No { get; set; }

        public string? Query_No { get; set; }

        // Section 78 Review period and eligibility.
        public DateTime? Review_Close_Date { get; set; }

        public string? Review_Status { get; set; }

        public bool CanLodgeReview { get; set; }

        public string? ReviewActionText { get; set; }

        public bool IsReviewOpen =>
            Section78ReviewStatus.IsOpen(Review_Status);

        public bool IsReviewClosed =>
            Section78ReviewStatus.IsClosed(Review_Status);

        public bool IsQuery =>
            Sub_typ == 0;

        public bool IsReview =>
            Sub_typ == 1;

        public string DisplayReference =>
            !string.IsNullOrWhiteSpace(Query_No)
                ? Query_No
                : Objection_No ?? string.Empty;
    }
}

