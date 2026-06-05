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
            var s1 = S("Section1");

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
                            V(s1,
                                "Erf_Unit_No",
                                "ErfNo",
                                "ERF",
                                "Unit_Key",
                                "UnitNo"),
                            1);

                        LineField(r, "SUBURB/SCHEME NAME",
                            V(s1,
                                "Suburb_Scheme_Name",
                                "SuburbSchemeName",
                                "Suburb",
                                "Town",
                                "SchemeName"),
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
            BuildDeclaration(col, "Section10");
            BuildAdminReceipt(col);
        }

        private void BuildSection3Reasons(ColumnDescriptor col)
        {
            var s = S("Section2Query");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 3: REASONS/MOTIVATION WHY ABOVE SUPPLEMENTARY IS TO BE DONE")
                        .Bold();

                    x.Item()
                      .PaddingTop(6)
                      .Border(1)
                      .MinHeight(120)
                      .Padding(6)
                      .Text((string?)Convert.ToString(V(s,
                          "Reasons_Motivation",
                          "Motivation",
                          "Reason",
                          "Reasons",
                          "Query_Reason",
                          "QueryReason",
                          "Description")) ?? string.Empty)
                      .FontSize(8);
                });
            });
        }

        private void BuildSection4PropertyDetails(ColumnDescriptor col)
        {
            var s = S("Section5");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 4: PROPERTY DETAILS (FOR SECTIONAL TITLES SEE SECTION 6)")
                        .Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "PHYSICAL ADDRESS",
                            V(s,
                                "PhysicalAddress",
                                "Physical_Address",
                                "PropertyPhysicalAddress",
                                "Address"),
                            3);

                        LineField(r, "CODE",
                            V(s,
                                "Code",
                                "PostalCode",
                                "PropertyCode"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "EXTENT OF PROPERTY (m²)",
                            V(s,
                                "Extent",
                                "ExtentM2",
                                "ExtentOfProperty",
                                "Property_Extent"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "MUNICIPAL ACCOUNT NO.",
                            V(s,
                                "MunicipalAccountNo",
                                "Municipal_Account_No",
                                "AccountNo"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF BONDHOLDER",
                            V(s,
                                "BondholderName",
                                "Bondholder",
                                "NameOfBondholder"),
                            1);

                        LineField(r, "REGISTERED AMOUNT OF BOND",
                            V(s,
                                "RegisteredBondAmount",
                                "BondAmount"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "SERVITUDES / ROAD PROCLAMATIONS / OTHER ENDORSEMENTS",
                            V(s,
                                "ServitudeDetails",
                                "EndorsementDetails",
                                "Servitudes",
                                "RoadProclamations"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "SERVITUDE NO.",
                            V(s, "ServitudeNo", "Servitude_No"),
                            1);

                        LineField(r, "AFFECTED AREA (m²)",
                            V(s, "AffectedArea", "Affected_Area"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IN FAVOUR OF",
                            V(s, "InFavourOf", "In_Favour_Of"),
                            1);

                        LineField(r, "FOR WHAT PURPOSE",
                            V(s, "Purpose", "PurposeOfServitude", "ForWhatPurpose"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "WAS COMPENSATION PAID?",
                            V(s, "CompensationPaid", "WasCompensationPaid"),
                            1);

                        LineField(r, "DATE OF PAYMENT",
                            V(s, "CompensationDate", "DateOfPayment"),
                            1);

                        LineField(r, "AMOUNT R",
                            V(s, "CompensationAmount", "Amount"),
                            1);
                    });
                });
            });
        }

        private void BuildSection5Buildings(ColumnDescriptor col)
        {
            var s = S("Section6");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 5: DESCRIPTION OF BUILDINGS (FOR SECTIONAL TITLES SEE SECTION 6)")
                        .Bold();

                    x.Item().PaddingTop(6)
                        .Text("5.1 TENANT AND RENT INFORMATION – ANNEXURE A")
                        .SemiBold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF TENANT",
                            V(s, "TenantName", "NameOfTenant"),
                            2);

                        LineField(r, "SIZE",
                            V(s, "TenantSize", "Size"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "RENTAL (EXCL VAT)",
                            V(s, "RentalExclVat", "Rental"),
                            1);

                        LineField(r, "ESCALATION OF RENTAL",
                            V(s, "EscalationOfRental", "Escalation"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "OTHER CONTRIBUTION",
                            V(s, "OtherContribution", "OtherContributions"),
                            1);

                        LineField(r, "TERM OF LEASE",
                            V(s, "LeaseTerm", "TermOfLease"),
                            1);

                        LineField(r, "START DATE",
                            V(s, "LeaseStartDate", "StartDate"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("5.2 SCHEDULE OF EXPENSES INCLUDING: MUNICIPAL, ADMINISTRATION, INSURANCES, SECURITY etc. – ANNEXURE B")
                        .FontSize(8);

                    x.Item().PaddingTop(4)
                        .Text("5.3 STATEMENT OF INCOME & EXPENDITURE FOR PREVIOUS FINANCIAL YEAR – ANNEXURE C")
                        .FontSize(8);

                    x.Item().PaddingTop(4)
                        .Text("5.4 BUILDING SIZES – ANNEXURE D")
                        .SemiBold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "BUILDING NO.",
                            V(s, "BuildingNo", "Building_No"),
                            1);

                        LineField(r, "SIZE (m²)",
                            V(s, "BuildingSize", "SizeM2"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "DESCRIPTION",
                            V(s, "BuildingDescription", "Description"),
                            2);

                        LineField(r, "CONDITION",
                            V(s, "Condition", "BuildingCondition"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("5.5 IF THE PROPERTY HAS NOT BEEN DEVELOPED TO ITS HIGHEST AND BEST USE, INDICATE THE EXTENT OF LAND THAT IS AVAILABLE FOR FURTHER DEVELOPMENT (m²)")
                        .FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "DEVELOPMENT",
                            V(s, "Development", "FurtherDevelopmentExtent"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("OTHER FEATURES OR BUILDINGS: (PROVIDE ANNEXURE IF NECESSARY)")
                        .SemiBold();

                   

                    x.Item()
                        .Border(1)
                        .MinHeight(70)
                        .Padding(4)
                        .Text((string?)Convert.ToString(V(s,
                            "OtherFeatures",
                            "OtherBuildings",
                            "OtherFeaturesOrBuildings")) ?? string.Empty)
                        .FontSize(8);
                });
            });
        }

        private void BuildSection6SectionalTitle(ColumnDescriptor col)
        {
            var s = S("Section7");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 6: SECTIONAL TITLE UNITS")
                        .Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "SCHEME NO.",
                            V(s, "SchemeNo", "Scheme_No"),
                            1);

                        LineField(r, "NAME OF SCHEME",
                            V(s, "SchemeName", "NameOfScheme"),
                            2);

                        LineField(r, "FLAT NO./DOOR NO.",
                            V(s, "FlatNo", "DoorNo", "Flat_Door_No"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "UNIT SIZE (m²)",
                            V(s, "UnitSize", "Unit_Size"),
                            1);

                        LineField(r, "NAME OF MANAGING AGENT",
                            V(s, "ManagingAgent", "NameOfManagingAgent"),
                            2);

                        LineField(r, "TEL NO.",
                            V(s, "ManagingAgentTel", "TelNo"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("INDICATE NUMBER OR STATE YES/NO")
                        .SemiBold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "NO OF BEDROOMS",
                            V(s, "NoBedrooms", "Bedrooms"),
                            1);

                        LineField(r, "NO OF BATHROOMS",
                            V(s, "NoBathrooms", "Bathrooms"),
                            1);

                        LineField(r, "KITCHEN",
                            V(s, "Kitchen"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "LOUNGE",
                            V(s, "Lounge"),
                            1);

                        LineField(r, "DINING ROOM",
                            V(s, "DiningRoom"),
                            1);

                        LineField(r, "STUDY",
                            V(s, "Study"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "LOUNGE WITH DINING ROOM",
                            V(s, "LoungeWithDiningRoom", "LoungeWithDining"),
                            1);

                        LineField(r, "PLAYROOM",
                            V(s, "Playroom"),
                            1);

                        LineField(r, "TELEVISION",
                            V(s, "Television", "TelevisionRoom"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "LAUNDRY",
                            V(s, "Laundry"),
                            1);

                        LineField(r, "SEPARATE TOILET",
                            V(s, "SeparateToilet"),
                            1);

                        LineField(r, "OTHER",
                            V(s, "Other1"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("COMMON PROPERTY CONSISTS OF: DETAILS OF EXCLUSIVE AREAS")
                        .SemiBold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "SWIMMING POOL (m²)",
                            V(s, "SwimmingPool"),
                            1);

                        LineField(r, "GARAGE (m²)",
                            V(s, "Garage"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "TENNIS COURT (m²)",
                            V(s, "TennisCourt"),
                            1);

                        LineField(r, "CARPORT (m²)",
                            V(s, "Carport"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "OPEN PARKING (m²)",
                            V(s, "OpenParking"),
                            1);

                        LineField(r, "STORE ROOM (m²)",
                            V(s, "StoreRoom"),
                            1);

                        LineField(r, "GARDEN (m²)",
                            V(s, "Garden"),
                            1);
                    });
                });
            });
        }

        private void BuildSection7MarketInformation(ColumnDescriptor col)
        {
            var s = S("Section8");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 7: MARKET INFORMATION")
                        .Bold();

                    x.Item().Text("IF YOUR PROPERTY IS CURRENTLY ON THE MARKET WHAT IS THE ASKING PRICE?")
                        .FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "R",
                            V(s, "CurrentAskingPrice", "AskingPrice"),
                            1);

                        LineField(r, "OFFER RECEIVED R",
                            V(s, "CurrentOfferReceived", "OfferReceived"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("IF YOUR PROPERTY HAS BEEN ON THE MARKET IN THE LAST 3 YEARS WHAT WAS THE ASKING PRICE?")
                        .FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "R",
                            V(s, "PreviousAskingPrice"),
                            1);

                        LineField(r, "OFFER RECEIVED R",
                            V(s, "PreviousOfferReceived"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF AGENT",
                            V(s, "AgentName", "NameOfAgent"),
                            1);

                        LineField(r, "TEL NO.",
                            V(s, "AgentTel", "AgentTelNo"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("SALES TRANSACTIONS USED BY OWNER IN DETERMINING THE MARKET VALUE OF THE PROPERTY")
                        .SemiBold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "ERF/UNIT NO.",
                            V(s, "ComparableErfNo", "SalesErfUnitNo"),
                            1);

                        LineField(r, "SUBURB/FARM/SCHEME NAME",
                            V(s, "ComparableSuburb", "SalesSuburb"),
                            2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "DATE OF SALE",
                            V(s, "ComparableSaleDate", "DateOfSale"),
                            1);

                        LineField(r, "SELLING PRICE",
                            V(s, "ComparableSellingPrice", "SellingPrice"),
                            1);
                    });
                });
            });
        }

        private void BuildSection8QueryDetails(ColumnDescriptor col)
        {
            var s = S("Section9");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 8: QUERY DETAILS PARTICULARS TO BE REFLECTED IN VALUATION ROLL CHANGES REQUESTED BY OWNER")
                        .Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "DESCRIPTION OF THE PROPERTY / UNIT NO.",
                            V(s,
                                "DescriptionOfProperty",
                                "PropertyDescription",
                                "QueryPropertyDescription",
                                "Old_Property_Description"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "CATEGORY",
                            V(s,
                                "Category",
                                "QueryCategory",
                                "Old_Category"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "PHYSICAL ADDRESS / DOOR NO. / FLAT NO.",
                            V(s,
                                "PhysicalAddress",
                                "QueryPhysicalAddress",
                                "Old_Address"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "EXTENT",
                            V(s,
                                "Extent",
                                "QueryExtent",
                                "Old_Extent"),
                            1);

                        LineField(r, "MARKET VALUE",
                            V(s,
                                "MarketValue",
                                "QueryMarketValue",
                                "Old_Market_Value"),
                            1);

                        LineField(r, "WITH EFFECT DATE",
                            V(s,
                                "WithEffectDate",
                                "EffectiveDate",
                                "Old_Effective_Date"),
                            1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF OWNER",
                            V(s,
                                "OwnerName",
                                "NameOfOwner",
                                "QueryOwnerName"),
                            1);
                    });

                    x.Item().PaddingTop(6)
                        .Text("ADVERSE FEATURES AND/OR FURTHER REASONS IN SUPPORT OF THIS REVIEW")
                        .SemiBold();
                    
                    x.Item()
                        .Border(1)
                        .MinHeight(70)
                        .Padding(4)
                        .Text((string?)Convert.ToString(V(s,
                            "AdverseFeatures",
                            "FurtherReasons",
                            "QueryReasons",
                            "Reason")) ?? string.Empty)
                        .FontSize(8);
                });
            });
        }
    }
}