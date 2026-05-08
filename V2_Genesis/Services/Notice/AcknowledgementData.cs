using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace V2_Genesis.Services.Notice
{
    public class AcknowledgementData
    {
        // ── Reference ─────────────────────────────────────────────────────
        public string ObjectionNo { get; set; } = string.Empty;
        public string ObjectionRef { get; set; } = string.Empty;
        public string RollSource { get; set; } = string.Empty;
        public string SubmissionTime { get; set; } = string.Empty;
        public bool IsMulti { get; set; }
        public int FileCount { get; set; }
        public string? ObjectionReason { get; set; }

        // ── Section 1 — Original roll values ──────────────────────────────
        public string? Old_PropertyDescription { get; set; }
        public string? Old_Category { get; set; }
        public string? Old_Address { get; set; }
        public string? Old_Extent { get; set; }
        public string? Old_MarketValue { get; set; }
        public string? Old_Owner { get; set; }

        // ── Section 1 — Objector's claimed values ─────────────────────────
        public string? New_PropertyDescription { get; set; }
        public string? New_Category { get; set; }
        public string? New_Address { get; set; }
        public string? New_Extent { get; set; }
        public string? New_MarketValue { get; set; }
        public string? New_Owner { get; set; }

        // ── Multi: Section 2 ──────────────────────────────────────────────
        public string? Old2_Category { get; set; }
        public string? Old2_Extent { get; set; }
        public string? Old2_MarketValue { get; set; }
        public string? New2_Category { get; set; }
        public string? New2_Extent { get; set; }
        public string? New2_MarketValue { get; set; }

        // ── Multi: Section 3 ──────────────────────────────────────────────
        public string? Old3_Category { get; set; }
        public string? Old3_Extent { get; set; }
        public string? Old3_MarketValue { get; set; }
        public string? New3_Category { get; set; }
        public string? New3_Extent { get; set; }
        public string? New3_MarketValue { get; set; }

        // ── Factory: build from TempData ─────────────────────────────────
        public static AcknowledgementData FromTempData(ITempDataDictionary td, string rollSource)
        {
            string? Get(string key) => td[key]?.ToString();
            int GetInt(string key) => int.TryParse(Get(key), out var v) ? v : 0;

            var pin = Get("pin") ?? Get("objection_ref") ?? string.Empty;

            return new AcknowledgementData
            {
                ObjectionNo = pin,
                ObjectionRef = Get("objection_ref") ?? pin,
                RollSource = rollSource,
                SubmissionTime = Get("time") ?? DateTime.Now.ToString("dd MMMM yyyy HH:mm"),
                IsMulti = (Get("IsMulti") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase),

                FileCount = GetInt("Count"),
                ObjectionReason = Get("objection_reason"),

                Old_PropertyDescription = Get("Old_Property_Description"),
                Old_Category = Get("Old_Category"),
                Old_Address = Get("Old_Address"),
                Old_Extent = Get("Old_Extent"),
                Old_MarketValue = Get("Old_Market_Value"),
                Old_Owner = Get("Old_Owner"),

                New_PropertyDescription = Get("new_Property_Description"),
                New_Category = Get("new_Category"),
                New_Address = Get("new_Address"),
                New_Extent = Get("new_Extent"),
                New_MarketValue = Get("new_Market_Value"),
                New_Owner = Get("new_Owner"),

                Old2_Category = Get("Old2_Category"),
                Old2_Extent = Get("Old2_Extent"),
                Old2_MarketValue = Get("Old2_Market_Value"),
                New2_Category = Get("new2_Category"),
                New2_Extent = Get("new2_Extent"),
                New2_MarketValue = Get("new2_Market_Value"),

                Old3_Category = Get("Old3_Category"),
                Old3_Extent = Get("Old3_Extent"),
                Old3_MarketValue = Get("Old3_Market_Value"),
                New3_Category = Get("new3_Category"),
                New3_Extent = Get("new3_Extent"),
                New3_MarketValue = Get("new3_Market_Value"),
            };
        }
    }
}
