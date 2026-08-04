using QuestPDF.Fluent;
using V2_Genesis.Models;

namespace GV_Forms.Pdf
{
    public sealed class QueryFormBDocument : QueryPdfBase
    {
        public QueryFormBDocument(V2_Genesis.Models.InquiryAggregate data, Wording wording)
            : base(data, wording)
        {
        }

        protected override string GetHeadingLeft()
            => "PROPERTIES OTHER THAN RESIDENTIAL OR AGRICULTURAL (EG. BUSINESSES, FACTORIES, OFFICES, SCHOOLS)";

        protected override void BuildPropertyIntro(ColumnDescriptor col)
        {
            dynamic main = Data.Main;

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("LODGING OF A QUERY AGAINSTS MATTERS PERTAINING TO A GENERAL / SUPPLEMENTARY VALUATION ON THE PROPERTY DESCRIBED BELOW:")
                        .Bold()
                        .FontSize(8);

                    x.Item().PaddingTop(4)
                        .Text("DESCRIPTION OF PROPERTY IN RESPECT OF WHICH THE QUERY IS MADE")
                        .Bold()
                        .FontSize(8);

                    x.Item().Text("(Complete a separate form for each property)")
                        .FontSize(8);

                    x.Item().PaddingTop(6).Row(r =>
                    {
                        LineField(r, "ERF/UNIT NO.",
                            V(main,
                                "ERF",
                                "Erf",
                                "Unit_Key",
                                "Unit_key",
                                "Property_id"),
                            1);

                        LineField(r, "SUBURB/SCHEME NAME",
                            V(main,
                                "Town",
                                "Town_Name",
                                "Property_Desc"),
                            2);
                    });
                });
            });
        }

        protected override void BuildRemainingSections(ColumnDescriptor col)
        {
            BuildSection3Reasons(col);
            BuildSection4PropertyDetails(col);
            BuildSection5Buildings(col);
            BuildSection6SectionalTitle(col);
            BuildSection7MarketInformation(col);
            BuildSection8QueryDetails(col);
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
                    x.Item()
                        .Text("SECTION 4: PROPERTY DETAILS (FOR SECTIONAL TITLES SEE SECTION 6)")
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
                            V(s, "Registered_Amount"),
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

                        LineField(r, "AMOUNT R",
                            V(s, "Compensation_Amount"),
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
                        .Text("SECTION 5: DESCRIPTION OF BUILDINGS (FOR SECTIONAL TITLES SEE SECTION 6)")
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
                            V(s, "Bus_Rental"),
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
                        .Text("SECTION 6: SECTIONAL TITLE UNITS")
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

                    x.Item().Row(r =>
                    {
                        LineField(r, "SHOPS",
                            V(s, "Bus4_Shops"),
                            1);

                        LineField(r, "OFFICES",
                            V(s, "Bus4_Offices"),
                            1);

                        LineField(r, "FACTORIES",
                            V(s, "Bus4_Factories"),
                            1);
                    });

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
                        .Text("SECTION 7: MARKET INFORMATION")
                        .Bold();

                    x.Item()
                        .Text("IF YOUR PROPERTY IS CURRENTLY ON THE MARKET WHAT IS THE ASKING PRICE?")
                        .FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "R",
                            V(s, "Current_Asking_price"),
                            1);

                        LineField(r, "OFFER RECEIVED R",
                            V(s, "Current_Recieved_Offer"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("IF YOUR PROPERTY HAS BEEN ON THE MARKET IN THE LAST 3 YEARS WHAT WAS THE ASKING PRICE?")
                        .FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "R",
                            V(s, "Previous_Asking_price"),
                            1);

                        LineField(r, "OFFER RECEIVED R",
                            V(s, "Previous_Recieved_Offer"),
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
                            V(s, "Selling_Price"),
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
                        .Text("SECTION 8: QUERY DETAILS PARTICULARS TO BE REFLECTED IN VALUATION ROLL CHANGES REQUESTED BY OWNER")
                        .Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "DESCRIPTION OF THE PROPERTY / UNIT NO.",
                            V(s, "New_Property_Description", "Old_Property_Description"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "CATEGORY",
                            V(s, "New_Category", "Old_Category"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "PHYSICAL ADDRESS / DOOR NO. / FLAT NO.",
                            V(s, "New_Address", "Old_Address"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "EXTENT",
                            V(s, "New_Extent", "Old_Extent"),
                            1);

                        LineField(r, "MARKET VALUE",
                            V(s, "New_Market_Value", "Old_Market_Value"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF OWNER",
                            V(s, "New_Owner", "Old_Owner"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("ADVERSE FEATURES AND/OR FURTHER REASONS IN SUPPORT OF THIS REVIEW")
                        .SemiBold();

                    x.Item()
                        .Border(1)
                        .MinHeight(70)
                        .Padding(4)
                        .Text((string)Convert.ToString(V(s, "Objection_Reasons")) ?? string.Empty)
                        .FontSize(8);
                });
            });
        }
    }
}
