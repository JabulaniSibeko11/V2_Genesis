
using QuestPDF.Fluent;
using V2_Genesis.Models;

namespace GV_Forms.Pdf
{
    public sealed class QueryFarmDocument : QueryPdfBase
    {
        public QueryFarmDocument(InquiryAggregate data, Wording wording) : base(data, wording) { }

        protected override string GetHeadingLeft()
            => "AGRICULTURAL HOLDINGS OR FARMS";

        protected override void BuildPropertyIntro(ColumnDescriptor col)
        {
            dynamic main = Data.Main;

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("LODGING OF A QUERY AGAINSTS MATTERS PERTAINING TO A GENERAL / SUPPLEMENTARY VALUATION ON THE PROPERTY DESCRIBED BELOW:")
                        .Bold().FontSize(8);

                    x.Item().PaddingTop(4).Text("DESCRIPTION OF PROPERTY IN RESPECT OF WHICH THE QUERY IS MADE")
                        .Bold().FontSize(8);

                    x.Item().Text("(Complete a separate form for each property)").FontSize(8);

                    x.Item().PaddingTop(6).Row(r =>
                    {
                        LineField(r, "ERF/PORTION/UNIT NO.",
                            V(main, "ERF", "Erf", "PTN", "Ptn", "Unit_key", "Unit_Key", "Property_id"), 1);
                        LineField(r, "SUBURB/SCHEME NAME",
                            V(main, "Town", "Town_Name", "Property_Desc"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "FARM NO.", V(main, "FarmNo", "Farm_No", "ERF", "Erf"), 1);
                        LineField(r, "REG. DIV", V(main, "RegDiv", "Reg_Div"), 1);
                    });
                });
            });
        }

        protected override void BuildRemainingSections(ColumnDescriptor col)
        {
            BuildSection3Reasons(col);
            BuildSection4PropertyDetails(col);
            BuildSection5DwellingAndLand(col);
            BuildSection5Other(col);
            BuildSection6Market(col);
            BuildSection7ReviewDetails(col);
            BuildDeclaration(col, "Section7");
            BuildAdminReceipt(col);
        }
        private void BuildSection3Reasons(ColumnDescriptor col)
        {
            var s = S("Section2Query");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item()
                        .Text("3.1 Reasons/Motivation why above supplementary is to be done")
                        .Bold()
                        .FontSize(11);

                    x.Item()
                        .PaddingTop(6)
                        .Border(1)
                        .MinHeight(520)
                        .Padding(6)
                        .Text((string?)Convert.ToString(V(s, "Motivation_for_Supp_Request")) ?? string.Empty)
                        .FontSize(8);
                });
            });
        }
        private void BuildSection4PropertyDetails(ColumnDescriptor col)
        {
            var s = S("Section2");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 4 PROPERTY DETAILS (FOR SECTIONAL TITLES SEE SECTION 6)").Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "PHYSICAL ADDRESS", V(s, "physical_address"), 3);

                        LineField(r, "CODE", V(s, "Code"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "EXTENT OF PROPERTY (m²)", V(s, "Extent"), 1);
                        LineField(r, "MUNICIPAL ACCOUNT NO.", V(s, "Municipal_Account_No"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF BONDHOLDER", V(s, "BondHolder_Name"), 1);
                        LineField(r, "REGISTERED AMOUNT OF BOND", V(s, "Registered_Amount"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "SERVITUDES / ROAD PROCLAMATIONS / OTHER ENDORSEMENTS", V(s, "Full_Details"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "SERVITUDE NO.", V(s, "Servitude_No"), 1);
                        LineField(r, "AFFECTED AREA (m²)", V(s, "Affected_Area"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IN FAVOUR OF", V(s, "Property_Favour_Of"), 1);
                        LineField(r, "FOR WHAT PURPOSE", V(s, "Property_Purpose"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "WAS COMPENSATION PAID?", V(s, "Compensation_Paid"), 1);
                        LineField(r, "DATE OF PAYMENT", V(s, "Payment_Date"), 1);
                        LineField(r, "AMOUNT R", V(s, "Compensation_Amount"), 1);
                    });
                });
            });
        }

        private void BuildSection5DwellingAndLand(ColumnDescriptor col)
        {
            var s = S("Section3Agri");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 5: DESCRIPTION OF RESIDENTIAL DWELLING").Bold();

                    x.Item().Text("5.1 MAIN DWELLING").SemiBold();
                    x.Item().Row(r =>
                    {
                        LineField(r, "BEDROOMS", V(s, "Agri_No_of_Bedroom"), 1);
                        LineField(r, "BATHROOMS", V(s, "Agri_No_of_BathRoom"), 1);
                        LineField(r, "KITCHEN", V(s, "Agri_Kitchen"), 1);
                        LineField(r, "LOUNGE", V(s, "Agri_Lounge"), 1);
                        LineField(r, "DINING ROOM", V(s, "Agri_Dinning_Room"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "LOUNGE WITH DINING ROOM", V(s, "Agri_Lounge_Dining_Room"), 1);
                        LineField(r, "STUDY", V(s, "Agri_Study"), 1);
                        LineField(r, "PLAYROOM", V(s, "Agri_Play_Room"), 1);
                        LineField(r, "TELEVISION", V(s, "Agri_Television"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "LAUNDRY", V(s, "Agri_Laundry"), 1);
                        LineField(r, "SEPARATE TOILET", V(s, "Agri_Seperate_Toilet"), 1);
                        LineField(r, "OTHER", V(s, "Agri_Dwell_Other1"), 1);
                        LineField(r, "OTHER", V(s, "Other2"), 1);
                    });

                    x.Item().PaddingTop(6).Text("5.2 OTHER BUILDINGS – ATTACH AS ANNEXURE A").SemiBold();
                    x.Item().Row(r =>
                    {
                        LineField(r, "BUILDING NO.", V(s, "BuildingNo"), 1);
                        LineField(r, "DESCRIPTION", V(s, "BuildingDescription"), 2);
                        LineField(r, "SIZE (m²)", V(s, "BuildingSize"), 1);
                        LineField(r, "CONDITION", V(s, "Condition"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IS THE BUILDING FUNCTIONAL?", V(s, "IsFunctional"), 1);
                    });

                    x.Item().PaddingTop(6).Text("5.3 IS ANY PORTION OF THE PROPERTY USED FOR ANY PURPOSE OTHER THAN AGRICULTURAL?").SemiBold();
                    x.Item().Row(r =>
                    {
                        LineField(r, "YES / NO", V(s, "OtherUseYesNo", "IsUsedForOtherPurpose"), 1);
                        LineField(r, "DESCRIBE THE USE(S)", V(s, "OtherUseDescription"), 2);
                    });

                    x.Item().PaddingTop(6).Text("5.4 LAND ANALYSIS").SemiBold();
                    x.Item().Row(r =>
                    {
                        LineField(r, "NON-AGRICULTURAL (ha)", V(s, "Agri_Non_Agricultural"), 1);
                        LineField(r, "GRAZING (ha)", V(s, "Agri_Grazing"), 1);
                        LineField(r, "UNDER IRRIGATION (ha)", V(s, "Agri_Under_Irrigation"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "DRY LAND (ha)", V(s, "Agri_Dry_Land"), 1);
                        LineField(r, "PERMANENT CROPS (ha)", V(s, "Agri_Permanent_Crop"), 1);
                        LineField(r, "TOTAL (ha)", V(s, "Agri_Total_ha"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "TOTAL (ha)", V(s, "TotalHa"), 1);
                        LineField(r, "CONDITION OF FENCES", V(s, "FencesCondition"), 1);
                        LineField(r, "AREA GAME FENCED (ha)", V(s, "GameFencedHa"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NO. OF BOREHOLES", V(s, "NoOfBoreholes"), 1);
                        LineField(r, "OUTPUT LITRES/HOUR", V(s, "OutputLitresPerHour"), 1);
                        LineField(r, "DAMS CAPACITY", V(s, "DamsCapacity"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IS THE PROPERTY EXPOSED TO A RIVER?", V(s, "ExposedToRiver"), 1);
                    });
                });
            });
        }

        private void BuildSection5Other(ColumnDescriptor col)
        {
            var s = S("Section3Agri");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("5.5 OTHER").Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "AFFECTED BY LAND CLAIM?", V(s, "Agri_Land_Claim"), 1);
                        LineField(r, "DATE OF CLAIM", V(s, "Agri_Claim_Date"), 1);
                        LineField(r, "GAZETTE NO.", V(s, "Agri_Gazette_No"), 1);

                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "DO YOU HAVE WATER RIGHTS?", V(s, "Agri_Water_Rights"), 1);
                        LineField(r, "DETAILS", V(s, "Agri_Water_Rights_Details"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "APPLIED FOR REZONING / CONSENT USE?", V(s, "Agri_Rezoning_Consent_Use"), 1);
                        LineField(r, "DETAILS", V(s, "Agri_Consent_Use_Details"), 2);

                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "HAS PROPERTY BEEN EXCISED?", V(s, "Agri_Land_Excised"), 1);
                        LineField(r, "NEW FARM DESCRIPTION", V(s, "Agri_New_Farm_Desc"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "HAS TOWNSHIP APPLIED FOR / PROCLAIMED?", V(s, "Agri_Township_Applied"), 1);
                        LineField(r, "DETAILS", V(s, "Agri_Township_Applied_Detail"), 2);
                    });

                    x.Item().Text("TENANT AND RENT INFORMATION - ANNEXURE C").SemiBold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF TENANT", V(s, "TenantName"), 2);
                        LineField(r, "SIZE", V(s, "TenantSize"), 1);
                        LineField(r, "RENTAL (EXCL VAT)", V(s, "RentalExclVat"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "ESCALATION", V(s, "Escalation"), 1);
                        LineField(r, "OTHER CONTRIBUTIONS", V(s, "OtherContribution"), 1);
                        LineField(r, "TERM OF LEASE", V(s, "LeaseTerm"), 1);
                        LineField(r, "START DATE", V(s, "LeaseStartDate"), 1);
                    });
                });
            });
        }

        private void BuildSection6Market(ColumnDescriptor col)
        {
            var s = S("Section5");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 6: MARKET INFORMATION").Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "CURRENT ASKING PRICE", V(s, "Current_Asking_price"), 1);
                        LineField(r, "OFFER RECEIVED", V(s, "Current_Recieved_Offer"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "ASKING PRICE IN LAST 3 YEARS", V(s, "Previous_Asking_price"), 1);
                        LineField(r, "OFFER RECEIVED", V(s, "Previous_Recieved_Offer"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF AGENT", V(s, "Agent_Name"), 1);
                        LineField(r, "TEL NO.", V(s, "Agent_Tel_No"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "SALES TRANSACTIONS", V(s, "Other_Nearby_Sales"), 1);
                    });
                });
            });
        }

        private void BuildSection7ReviewDetails(ColumnDescriptor col)
        {
            var s = S("Section6");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 7: REVIEW DETAILS PARTICULARS TO BE REFLECTED IN VALUATION ROLL CHANGES REQUESTED BY OWNER").Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "DESCRIPTION OF THE PROPERTY / UNIT NO.", V(s, "New_Property_Description", "Old_Property_Description"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "PHYSICAL ADDRESS", V(s, "New_Address", "Old_Address"), 2);
                        LineField(r, "CATEGORY", V(s, "New_Category", "Old_Category"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "EXTENT", V(s, "New_Extent", "Old_Extent"), 1);
                        LineField(r, "MARKET VALUE", V(s, "New_Market_Value", "Old_Market_Value"), 1);
                        LineField(r, "WITH EFFECT DATE", V(s, "WithEffectDate", "EffectiveDate"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF OWNER", V(s, "New_Owner", "Old_Owner"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "ADVERSE FEATURES / FURTHER REASONS", V(s, "Objection_Reasons"), 1);
                    });
                });
            });
        }
        private static string Rand(object? value)
        {
            var text = value?.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(text))
                return "";

            text = text
                .Replace("R", "", StringComparison.OrdinalIgnoreCase)
                .Replace(",", "")
                .Replace(" ", "")
                .Trim();

            if (!decimal.TryParse(text, out var amount))
                return "R " + value;

            return "R " + amount.ToString("N0", new System.Globalization.CultureInfo("en-ZA"));
        }
    }
}
