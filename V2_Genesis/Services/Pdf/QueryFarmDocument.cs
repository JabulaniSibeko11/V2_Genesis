
using QuestPDF.Fluent;
using V2_Genesis.Models;

namespace GV_Forms.Pdf
{
    public sealed class QueryFarmDocument : QueryPdfBase
    {
        public QueryFarmDocument(
            InquiryAggregate data,
            Wording wording,
            IWebHostEnvironment environment)
            : base(data, wording, environment)
        {
        }

        protected override string GetHeadingLeft()
            => "AGRICULTURAL HOLDINGS OR FARMS";

        protected override void BuildPropertyIntro(ColumnDescriptor col)
        {
            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text($"LODGING OF A {InquiryUpper} AGAINST MATTERS PERTAINING TO A GENERAL / SUPPLEMENTARY VALUATION ON THE PROPERTY DESCRIBED BELOW:")
                        .Bold().FontSize(8);

                    x.Item().PaddingTop(4).Text($"DESCRIPTION OF PROPERTY IN RESPECT OF WHICH THE {InquiryUpper} IS MADE")
                        .Bold().FontSize(8);

                    x.Item().Text("(Complete a separate form for each property)").FontSize(8);

                    x.Item().PaddingTop(6).Row(r =>
                    {
                        LineField(r, "ERF/PORTION/UNIT NO.", ErfOrUnitNumber(), 1);
                        LineField(r, "SUBURB/SCHEME NAME", PropertyDescription(), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "FARM NO.", FarmNumber(), 1);
                        LineField(r, "REG. DIV", RegistrationDivision(), 1);
                    });
                });
            });
        }

        protected override void BuildRemainingSections(ColumnDescriptor col)
        {
            BuildSection4PropertyDetails(col);
            BuildSection5DwellingAndLand(col);
            BuildSection5Other(col);
            BuildSection6Market(col);
            BuildSection7ReviewDetails(col);
            BuildDeclaration(col, "Section7", 8);
            BuildAdminReceipt(col);
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
                        LineField(r, "REGISTERED AMOUNT OF BOND", Money(V(s, "Registered_Amount")), 1);
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
                        LineField(r, "AMOUNT", Money(V(s, "Compensation_Amount")), 1);
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
                        LineField(r, "MAIN DWELLING SIZE", V(s, "Agri_Main_Dwelling_Size"), 1);
                    });

                    x.Item().PaddingTop(6).Text("5.2 OTHER BUILDINGS – ATTACH AS ANNEXURE A").SemiBold();
                    x.Item().Row(r =>
                    {
                        LineField(r, "BUILDING NO.", V(s, "Agri_Building_No"), 1);
                        LineField(r, "DESCRIPTION", V(s, "Agri_Building_Description"), 2);
                        LineField(r, "SIZE (m²)", V(s, "Agri_Building_Size"), 1);
                        LineField(r, "CONDITION", V(s, "Agri_Building_Condition"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IS THE BUILDING FUNCTIONAL?", V(s, "Agri_Building_Functional"), 1);
                    });

                    x.Item().PaddingTop(6).Text("5.3 IS ANY PORTION OF THE PROPERTY USED FOR ANY PURPOSE OTHER THAN AGRICULTURAL?").SemiBold();
                    x.Item().Row(r =>
                    {
                        LineField(r, "YES / NO", V(s, "Agri_Another_Purpose_Not_Agriculture"), 1);
                        LineField(r, "DESCRIBE THE USE(S)", V(s, "Agri_Another_Purpose_Not_Agriculture_Desc"), 2);
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
                        LineField(r, "OTHER 1 (ha)", V(s, "Agri_Other_ha_1"), 1);
                        LineField(r, "OTHER 2 (ha)", V(s, "Agri_Other_ha_2"), 1);
                        LineField(r, "OTHER 3 (ha)", V(s, "Agri_Other_ha_3"), 1);
                        LineField(r, "CONDITION OF FENCES", V(s, "Agri_Fence_Condition"), 1);
                        LineField(r, "AREA GAME FENCED (ha)", V(s, "Agri_Game_Area_Fenced"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NO. OF BOREHOLES", V(s, "Agri_Num_of_Boreholes"), 1);
                        LineField(r, "OUTPUT LITRES/HOUR", V(s, "Agri_Output_litres_Hours"), 1);
                        LineField(r, "DAMS", V(s, "Agri_Dams"), 1);
                        LineField(r, "CAPACITY", V(s, "Agri_Capacity"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IS THE PROPERTY EXPOSED TO A RIVER?", V(s, "Agri_Exposed_To_River"), 1);
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
                        LineField(r, "NAME OF TENANT", V(s, "Agri_Tenant_Name"), 2);
                        LineField(r, "SIZE", V(s, "Agri_Rental_Land_Size"), 1);
                        LineField(r, "RENTAL (EXCL VAT)", Money(V(s, "Agri_Rental")), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "ESCALATION", V(s, "Agri_Escalation"), 1);
                        LineField(r, "OTHER CONTRIBUTIONS", V(s, "Agri_Other_contribution"), 1);
                        LineField(r, "TERM OF LEASE", V(s, "Agri_Lease_Term"), 1);
                        LineField(r, "START DATE", V(s, "Agri_Start_Date"), 1);
                        LineField(r, "USE", V(s, "Agri_Use"), 1);
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
                        LineField(r, "CURRENT ASKING PRICE", Money(V(s, "Current_Asking_price")), 1);
                        LineField(r, "OFFER RECEIVED", Money(V(s, "Current_Recieved_Offer")), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "ASKING PRICE IN LAST 3 YEARS", Money(V(s, "Previous_Asking_price")), 1);
                        LineField(r, "OFFER RECEIVED", Money(V(s, "Previous_Recieved_Offer")), 1);
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
                    x.Item().Text($"SECTION 7: {InquiryUpper} DETAILS - PARTICULARS TO BE REFLECTED IN THE VALUATION ROLL AND CHANGES REQUESTED BY OWNER").Bold();

                    x.Item().PaddingTop(5).Row(r =>
                    {
                        r.RelativeItem()
                            .Text("PARTICULARS AS REFLECTED IN VALUATION ROLL")
                            .Bold()
                            .FontSize(8)
                            .AlignCenter();

                        r.RelativeItem()
                            .Text("CHANGES REQUESTED BY OWNER")
                            .Bold()
                            .FontSize(8)
                            .AlignCenter();
                    });

                    ComparisonLine(x, "DESCRIPTION OF THE PROPERTY / UNIT NO.: ",
                        V(s, "Old_Property_Description"),
                        V(s, "New_Property_Description"));

                    ComparisonLine(x, "CATEGORY: ",
                        V(s, "Old_Category"),
                        V(s, "New_Category"));

                    ComparisonLine(x, "PHYSICAL ADDRESS: ",
                        V(s, "Old_Address"),
                        V(s, "New_Address"));

                    ComparisonLine(x, "EXTENT: ",
                        V(s, "Old_Extent"),
                        V(s, "New_Extent"));

                    ComparisonLine(x, "MARKET VALUE: ",
                        Money(V(s, "Old_Market_Value")),
                        Money(V(s, "New_Market_Value")));

                    ComparisonLine(x, "NAME OF OWNER: ",
                        V(s, "Old_Owner"),
                        V(s, "New_Owner"));

                    if (!string.IsNullOrWhiteSpace(FirstValue(
                            V(s, "Old2_Category"), V(s, "New2_Category"),
                            V(s, "Old2_Extent"), V(s, "New2_Extent"),
                            V(s, "Old2_Market_Value"), V(s, "New2_Market_Value"))))
                    {
                        x.Item().PaddingTop(8).Text("PURPOSE / CATEGORY SPLIT 2").SemiBold().AlignCenter();
                        ComparisonLine(x, "CATEGORY: ", V(s, "Old2_Category"), V(s, "New2_Category"));
                        ComparisonLine(x, "EXTENT: ", V(s, "Old2_Extent"), V(s, "New2_Extent"));
                        ComparisonLine(x, "MARKET VALUE: ", Money(V(s, "Old2_Market_Value")), Money(V(s, "New2_Market_Value")));
                    }

                    if (!string.IsNullOrWhiteSpace(FirstValue(
                            V(s, "Old3_Category"), V(s, "New3_Category"),
                            V(s, "Old3_Extent"), V(s, "New3_Extent"),
                            V(s, "Old3_Market_Value"), V(s, "New3_Market_Value"))))
                    {
                        x.Item().PaddingTop(8).Text("PURPOSE / CATEGORY SPLIT 3").SemiBold().AlignCenter();
                        ComparisonLine(x, "CATEGORY: ", V(s, "Old3_Category"), V(s, "New3_Category"));
                        ComparisonLine(x, "EXTENT: ", V(s, "Old3_Extent"), V(s, "New3_Extent"));
                        ComparisonLine(x, "MARKET VALUE: ", Money(V(s, "Old3_Market_Value")), Money(V(s, "New3_Market_Value")));
                    }

                    x.Item().Row(r =>
                    {
                        LineField(r, "ADVERSE FEATURES / FURTHER REASONS", V(s, "Objection_Reasons"), 1);
                    });
                });
            });
        }
    }
}
