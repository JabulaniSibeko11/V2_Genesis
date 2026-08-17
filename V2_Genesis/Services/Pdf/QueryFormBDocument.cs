using QuestPDF.Fluent;
using System.Globalization;
using V2_Genesis.Models;

namespace GV_Forms.Pdf
{
    public sealed class QueryFormBDocument : QueryPdfBase
    {
        public QueryFormBDocument(
            V2_Genesis.Models.InquiryAggregate data,
            Wording wording,
            IWebHostEnvironment environment)
            : base(data, wording, environment)
        {
        }

        protected override string GetHeadingLeft()
            => PropertyFormType switch
            {
                "Res" => "RESIDENTIAL (FULL TITLE AND SECTIONAL TITLE USED FOR RESIDENTIAL PURPOSES)",
                "Multi" => "MULTIPLE PURPOSE (PROPERTY USED FOR MORE THAN ONE PURPOSE)",
                _ => "PROPERTIES OTHER THAN RESIDENTIAL OR AGRICULTURAL (EG. BUSINESSES, FACTORIES, OFFICES, SCHOOLS)"
            };

        protected override void BuildPropertyIntro(ColumnDescriptor col)
        {
            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text($"LODGING OF A {InquiryUpper} AGAINST MATTERS PERTAINING TO A GENERAL / SUPPLEMENTARY VALUATION ON THE PROPERTY DESCRIBED BELOW:")
                        .Bold()
                        .FontSize(8);

                    x.Item().PaddingTop(4)
                        .Text($"DESCRIPTION OF PROPERTY IN RESPECT OF WHICH THE {InquiryUpper} IS MADE")
                        .Bold()
                        .FontSize(8);

                    x.Item().Text("(Complete a separate form for each property)")
                        .FontSize(8);

                    x.Item().PaddingTop(6).Row(r =>
                    {
                        LineField(r, "ERF/UNIT NO.",
                            ErfOrUnitNumber(),
                            1);

                        LineField(r, "SUBURB/SCHEME NAME",
                            PropertyDescription(),
                            2);
                    });
                });
            });
        }

        protected override void BuildRemainingSections(ColumnDescriptor col)
        {
            BuildSection4PropertyDetails(col);

            if (PropertyFormType is "Res" or "Multi")
            {
                BuildSection5Residential(col);
                BuildSection6ResidentialSectionalTitle(col);
            }

            if (PropertyFormType is "Bus" or "Multi")
            {
                BuildSection5Buildings(col);
                BuildSection6SectionalTitle(col);
            }

            if (PropertyFormType == "Multi")
                BuildSection5Agricultural(col);

            BuildSection7MarketInformation(col);
            BuildSection8QueryDetails(col);

            // Match the supplied municipal templates. Business Review uses
            // SECTION 7: DECLARATION, while the supplied Residential Review
            // template retains SECTION 9: DECLARATION.
            var declarationSection = IsReview
                ? (PropertyFormType == "Res" ? 9 : 7)
                : 9;

            BuildDeclaration(col, "Section7", declarationSection);
            BuildAdminReceipt(col);
        }

        private void BuildSection5Residential(ColumnDescriptor col)
        {
            var s = S("Section3Res");

            RoundedBlock(col, c => c.Column(x =>
            {
                x.Item().Text($"SECTION {FormSection(5, 3)}: DESCRIPTION OF RESIDENTIAL DWELLING").Bold();
                x.Item().Text("5.1 MAIN DWELLING").SemiBold();

                x.Item().Row(r =>
                {
                    LineField(r, "BEDROOMS", V(s, "Res_No_of_Bedroom"));
                    LineField(r, "BATHROOMS", V(s, "Res_No_of_BathRoom"));
                    LineField(r, "KITCHEN", V(s, "Res_Kitchen"));
                    LineField(r, "LOUNGE", V(s, "Res_Lounge"));
                    LineField(r, "DINING ROOM", V(s, "Res_Dinning_Room"));
                });

                x.Item().Row(r =>
                {
                    LineField(r, "LOUNGE / DINING", V(s, "Res_Lounge_Dining_Room"));
                    LineField(r, "STUDY", V(s, "Res_Study"));
                    LineField(r, "PLAYROOM", V(s, "Res_Play_Room"));
                    LineField(r, "TV ROOM", V(s, "Res_Television"));
                    LineField(r, "LAUNDRY", V(s, "Res_Laundry"));
                });

                x.Item().Row(r =>
                {
                    LineField(r, "SEPARATE TOILET", V(s, "Res_Seperate_Toilet"));
                    LineField(r, "OTHER", V(s, "Res_Dwell_Other1"));
                    LineField(r, "OTHER", V(s, "Res_Dwell_Other2"));
                    LineField(r, "OTHER", V(s, "Res_Dwell_Other3"));
                    LineField(r, "OTHER", V(s, "Res_Dwell_Other4"));
                });

                x.Item().Text("OUTBUILDINGS").SemiBold();
                x.Item().Row(r =>
                {
                    LineField(r, "GARAGES", V(s, "Res_No_of_Garages"));
                    LineField(r, "GRANNY FLAT / ROOMS", V(s, "Res_Granny_Room"));
                    LineField(r, "OTHER", V(s, "Res_Outbuild_Other"));
                });

                x.Item().Row(r =>
                {
                    LineField(r, "MAIN SIZE (m²)", V(s, "Res_Main_Dwelling_Size"));
                    LineField(r, "OUTBUILDING SIZE (m²)", V(s, "Res_Outside_Building_Size"));
                    LineField(r, "OTHER SIZE (m²)", V(s, "Res_Other_Building_Size"));
                    LineField(r, "TOTAL SIZE (m²)", V(s, "Res_Total_Building_Size"));
                });

                x.Item().Row(r =>
                {
                    LineField(r, "POOL", V(s, "Res_Swimming_Pool"));
                    LineField(r, "BOREHOLE", V(s, "Res_Bore_Hole"));
                    LineField(r, "TENNIS COURT", V(s, "Res_Tennis_Court"));
                    LineField(r, "GARDEN", V(s, "Res_Garden"));
                });

                x.Item().Row(r =>
                {
                    LineField(r, "DRIVEWAY", V(s, "Res_Drive_Way"));
                    LineField(r, "BOOMED / SECURITY", V(s, "Res_Security_Boomed_Area"));
                    LineField(r, "GENERAL CONDITION", V(s, "Res_General_Condition"));
                });
            }));
        }

        private void BuildSection6ResidentialSectionalTitle(ColumnDescriptor col)
        {
            var s = S("Section4Res");

            RoundedBlock(col, c => c.Column(x =>
            {
                x.Item().Text($"SECTION {FormSection(6, 4)}: SECTIONAL TITLE UNITS").Bold();
                x.Item().Row(r =>
                {
                    LineField(r, "SCHEME NO.", V(s, "Res4_Scheme_No"));
                    LineField(r, "SCHEME NAME", V(s, "Res4_Scheme_Name"), 2);
                    LineField(r, "FLAT / DOOR NO.", V(s, "Res4_Flat_No"));
                    LineField(r, "UNIT SIZE (m²)", V(s, "Res4_Unit_Size"));
                });

                x.Item().Row(r =>
                {
                    LineField(r, "MANAGING AGENT", V(s, "Res4_Managing_Agent_Name"), 2);
                    LineField(r, "TEL NO.", V(s, "Res4_Managing_Agent_Tel_No"));
                    LineField(r, "MONTHLY LEVY", Money(V(s, "Res4_Monthly_Levy_Res")));
                });

                x.Item().Row(r =>
                {
                    LineField(r, "BEDROOMS", V(s, "Res4_No_of_Bedroom"));
                    LineField(r, "BATHROOMS", V(s, "Res4_No_of_BathRoom"));
                    LineField(r, "KITCHEN", V(s, "Res4_Kitchen"));
                    LineField(r, "LOUNGE", V(s, "Res4_Lounge"));
                    LineField(r, "DINING ROOM", V(s, "Res4_Dinning_Room"));
                });

                x.Item().Row(r =>
                {
                    LineField(r, "POOL (m²)", V(s, "Res4_Pool_Size"));
                    LineField(r, "TENNIS COURT (m²)", V(s, "Res4_Tennis_Court_Size"));
                    LineField(r, "GARAGE (m²)", V(s, "Res4_Garage_Size"));
                    LineField(r, "CARPORT (m²)", V(s, "Res4_Carport_Size"));
                });
            }));
        }

        private void BuildSection5Agricultural(ColumnDescriptor col)
        {
            var s = S("Section3Agri");

            RoundedBlock(col, c => c.Column(x =>
            {
                x.Item().Text($"SECTION {FormSection(5, 3)}: AGRICULTURAL USE DETAILS").Bold();
                x.Item().Row(r =>
                {
                    LineField(r, "NON-AGRICULTURAL (ha)", V(s, "Agri_Non_Agricultural"));
                    LineField(r, "GRAZING (ha)", V(s, "Agri_Grazing"));
                    LineField(r, "IRRIGATION (ha)", V(s, "Agri_Under_Irrigation"));
                    LineField(r, "DRY LAND (ha)", V(s, "Agri_Dry_Land"));
                    LineField(r, "PERMANENT CROPS (ha)", V(s, "Agri_Permanent_Crop"));
                });

                x.Item().Row(r =>
                {
                    LineField(r, "TOTAL (ha)", V(s, "Agri_Total_ha"));
                    LineField(r, "FENCE CONDITION", V(s, "Agri_Fence_Condition"));
                    LineField(r, "BOREHOLES", V(s, "Agri_Num_of_Boreholes"));
                    LineField(r, "DAMS", V(s, "Agri_Dams"));
                });

                x.Item().Row(r =>
                {
                    LineField(r, "TENANT", V(s, "Agri_Tenant_Name"), 2);
                    LineField(r, "RENTAL", Money(V(s, "Agri_Rental")));
                    LineField(r, "USE", V(s, "Agri_Use"));
                });
            }));
        }

        private void BuildSection4PropertyDetails(ColumnDescriptor col)
        {
            var s = S("Section2");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item()
                        .Text($"SECTION {FormSection(4, 2)}: PROPERTY DETAILS (FOR SECTIONAL TITLES SEE SECTION {FormSection(6, 4)})")
                        .Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "PHYSICAL ADDRESS",
                            V(s, "physical_address"),
                            3);

                        LineField(r, "CODE",
                            V(s, "Code"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "EXTENT OF PROPERTY (m²)",
                            V(s, "Extent"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "MUNICIPAL ACCOUNT NO.",
                            V(s, "Municipal_Account_No"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF BONDHOLDER",
                            V(s, "BondHolder_Name"),
                            1);

                        LineField(r, "REGISTERED AMOUNT OF BOND",
                            Money(V(s, "Registered_Amount")),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r,
                            "PROVIDE FULL DETAILS OF ALL SERVITUDES, ROAD PROCLAMATIONS OR OTHER ENDORSEMENTS AGAINST THE PROPERTY",
                            V(s, "Full_Details"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "SERVITUDE NO.",
                            V(s, "Servitude_No"),
                            1);

                        LineField(r, "AFFECTED AREA (m²)",
                            V(s, "Affected_Area"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IN FAVOUR OF",
                            V(s, "Property_Favour_Of"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "FOR WHAT PURPOSE",
                            V(s, "Property_Purpose"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "WAS COMPENSATION PAID?",
                            V(s, "Compensation_Paid"),
                            1);

                        LineField(r, "IF YES DATE OF PAYMENT",
                            V(s, "Payment_Date"),
                            1);

                        LineField(r, "AMOUNT",
                            Money(V(s, "Compensation_Amount")),
                            1);
                    });
                });
            });
        }


        private void BuildSection5Buildings(ColumnDescriptor col)
        {
            var s = S("Section3Bus");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item()
                        .Text($"SECTION {FormSection(5, 3)}: DESCRIPTION OF BUILDINGS (FOR SECTIONAL TITLES SEE SECTION {FormSection(6, 4)})")
                        .Bold();

                    x.Item().PaddingTop(6)
                        .Text("5.1 TENANT AND RENT INFORMATION – ANNEXURE A")
                        .SemiBold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF TENANT",
                            V(s, "Bus_Tenant_Name"),
                            2);

                        LineField(r, "SIZE",
                            V(s, "Bus_Rental_Land_Size"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "RENTAL (EXCL VAT)",
                            Money(V(s, "Bus_Rental")),
                            1);

                        LineField(r, "ESCALATION OF RENTAL",
                            V(s, "Bus_Escalation"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "OTHER CONTRIBUTION",
                            V(s, "Bus_Other_contribution"),
                            1);

                        LineField(r, "TERM OF LEASE",
                            V(s, "Bus_Lease_Term"),
                            1);

                        LineField(r, "START DATE",
                            V(s, "Bus_Start_Date"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("5.4 BUILDING SIZES – ANNEXURE D")
                        .SemiBold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "BUILDING NO.",
                            V(s, "Bus_Building_No"),
                            1);

                        LineField(r, "SIZE (m²)",
                            V(s, "Bus_Building_Size"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "DESCRIPTION e.g. Used as a shop, office etc.",
                            V(s, "Bus_Shops"),
                            2);

                        LineField(r, "CONDITION",
                            V(s, "Bus_Building_Condition"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("5.5 IF THE PROPERTY HAS NOT BEEN DEVELOPED TO ITS HIGHEST AND BEST USE, INDICATE THE EXTENT OF LAND THAT IS AVAILABLE FOR FURTHER DEVELOPMENT (m²)")
                        .FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "DEVELOPMENT",
                            V(s, "Bus_Extent_Land_further_Dev"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("OTHER FEATURES OR BUILDINGS: (PROVIDE ANNEXURE IF NECESSARY)")
                        .SemiBold();

                    x.Item()
                        .Border(1)
                        .MinHeight(70)
                        .Padding(4)
                         .Text((string?)Convert.ToString(V(s, "Bus_Other_features_Condition")) ?? string.Empty)

                        .FontSize(8);
                });
            });
        }

        private void BuildSection6SectionalTitle(ColumnDescriptor col)
        {
            var s = S("Section4Bus");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item()
                        .Text($"SECTION {FormSection(6, 4)}: SECTIONAL TITLE UNITS")
                        .Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "SCHEME NO.",
                            V(s, "Bus4_Scheme_No"),
                            1);

                        LineField(r, "NAME OF SCHEME",
                            V(s, "Bus4_Scheme_Name"),
                            2);

                        LineField(r, "FLAT NO./DOOR NO.",
                            V(s, "Bus4_Flat_No"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "UNIT SIZE (m²)",
                            V(s, "Bus4_Unit_Size"),
                            1);

                        LineField(r, "NAME OF MANAGING AGENT",
                            V(s, "Bus4_Managing_Agent_Name"),
                            2);

                        LineField(r, "TEL NO.",
                            V(s, "Bus4_Managing_Agent_Tel_No"),
                            1);
                    });

                    if (IsReview)
                    {
                        x.Item().Row(r =>
                        {
                            LineField(r,
                                "DESCRIPTION OF USE OF UNIT INCLUSIVE OF SIZES (m²)",
                                JoinValues(
                                    s,
                                    "Bus4_Shops",
                                    "Bus4_Offices",
                                    "Bus4_Factories",
                                    "Bus4_Bus_Sect_Title_Other1_name",
                                    "Bus4_Bus_Sect_Title_Other1",
                                    "Bus4_Bus_Sect_Title_Other2_name",
                                    "Bus4_Bus_Sect_Title_Other2",
                                    "Bus4_Bus_Sect_Title_Other3_name",
                                    "Bus4_Bus_Sect_Title_Other3"),
                                1);
                        });
                    }
                    else
                    {
                        x.Item().Row(r =>
                        {
                            LineField(r, "SHOPS", V(s, "Bus4_Shops"), 1);
                            LineField(r, "OFFICES", V(s, "Bus4_Offices"), 1);
                            LineField(r, "FACTORIES", V(s, "Bus4_Factories"), 1);
                        });
                    }

                    x.Item().PaddingTop(6)
                        .Text("COMMON PROPERTY CONSISTS OF: DETAILS OF EXCLUSIVE AREAS")
                        .SemiBold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "SWIMMING POOL (m²)",
                            V(s, "Bus4_Pool_Size"),
                            1);

                        LineField(r, "GARAGE (m²)",
                            V(s, "Bus4_Garage_Size"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "TENNIS COURT (m²)",
                            V(s, "Bus4_Tennis_Court_Size"),
                            1);

                        LineField(r, "CARPORT (m²)",
                            V(s, "Bus4_Carport_Size"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "OPEN PARKING (m²)",
                            V(s, "Bus4_Open_Parking_Size"),
                            1);

                        LineField(r, "STORE ROOM (m²)",
                            V(s, "Bus4_Store_Room_Size"),
                            1);

                        LineField(r, "GARDEN (m²)",
                            V(s, "Bus4_Garden_Size"),
                            1);
                    });
                });
            });
        }

        private void BuildSection7MarketInformation(ColumnDescriptor col)
        {
            var s = S("Section5");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item()
                        .Text($"SECTION {FormSection(7, 5)}: MARKET INFORMATION")
                        .Bold();

                    x.Item()
                        .Text("IF YOUR PROPERTY IS CURRENTLY ON THE MARKET WHAT IS THE ASKING PRICE?")
                        .FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "ASKING PRICE",
                            Money(V(s, "Current_Asking_price")),
                            1);

                        LineField(r, "OFFER RECEIVED",
                            Money(V(s, "Current_Recieved_Offer")),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("IF YOUR PROPERTY HAS BEEN ON THE MARKET IN THE LAST 3 YEARS WHAT WAS THE ASKING PRICE?")
                        .FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "ASKING PRICE",
                            Money(V(s, "Previous_Asking_price")),
                            1);

                        LineField(r, "OFFER RECEIVED",
                            Money(V(s, "Previous_Recieved_Offer")),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF AGENT",
                            V(s, "Agent_Name"),
                            1);

                        LineField(r, "TEL NO.",
                            V(s, "Agent_Tel_No"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("SALES TRANSACTIONS USED BY OWNER IN DETERMINING THE MARKET VALUE OF THE PROPERTY")
                        .SemiBold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "ERF/UNIT NO.",
                            V(s, "Unit_No"),
                            1);

                        LineField(r, "SUBURB/FARM/SCHEME NAME",
                            V(s, "Suburb_Name"),
                            2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "DATE OF SALE",
                            V(s, "Sale_Date"),
                            1);

                        LineField(r, "SELLING PRICE",
                            Money(V(s, "Selling_Price")),
                            1);
                    });
                });
            });
        }
        private void BuildSection8QueryDetails(ColumnDescriptor col)
        {
            var s = S("Section6");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item()
                        .Text($"SECTION {FormSection(8, 6)}: {InquiryUpper} DETAILS - PARTICULARS TO BE REFLECTED IN THE VALUATION ROLL AND CHANGES REQUESTED BY OWNER")
                        .Bold();

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

                    ComparisonLine(x, "PHYSICAL ADDRESS / DOOR NO. / FLAT NO.: ",
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

                    x.Item().PaddingTop(6)
                        .Text($"ADVERSE FEATURES AND/OR FURTHER REASONS IN SUPPORT OF THIS {InquiryUpper}")
                        .SemiBold();

                    string objectionReasons =
     Convert.ToString(
         (object?)V(s, "Objection_Reasons"),
         CultureInfo.InvariantCulture)
     ?? string.Empty;

                    x.Item()
                        .Border(1)
                        .MinHeight(70)
                        .Padding(4)
                        .Text(objectionReasons)
                        .FontSize(8);
                });
            });
        }
    }
}
