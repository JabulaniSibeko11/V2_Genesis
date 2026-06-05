
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
            var s1 = S("Section1");

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
                        LineField(r, "ERF/PORTION/UNIT NO.", V(s1, "Erf_Unit_No", "PortionNo", "UnitNo"), 1);
                        LineField(r, "SUBURB/SCHEME NAME", V(s1, "Suburb_Scheme_Name", "SuburbSchemeName"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "FARM NO.", V(s1, "FarmNo", "Farm_No"), 1);
                        LineField(r, "REG. DIV", V(s1, "RegDiv", "Reg_Div"), 1);
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
            BuildDeclaration(col, "Section8");
            BuildAdminReceipt(col);
        }

        private void BuildSection4PropertyDetails(ColumnDescriptor col)
        {
            var s = S("Section5");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 4 PROPERTY DETAILS (FOR SECTIONAL TITLES SEE SECTION 6)").Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "PHYSICAL ADDRESS", V(s, "PhysicalAddress", "Address"), 3);
                        LineField(r, "CODE", V(s, "Code", "PostalCode"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "EXTENT OF PROPERTY (m²)", V(s, "Extent", "ExtentM2"), 1);
                        LineField(r, "MUNICIPAL ACCOUNT NO.", V(s, "MunicipalAccountNo", "AccountNo"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF BONDHOLDER", V(s, "BondholderName", "Bondholder"), 1);
                        LineField(r, "REGISTERED AMOUNT OF BOND", V(s, "RegisteredBondAmount", "BondAmount"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "SERVITUDES / ROAD PROCLAMATIONS / OTHER ENDORSEMENTS", V(s, "ServitudeDetails", "EndorsementDetails"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "SERVITUDE NO.", V(s, "ServitudeNo"), 1);
                        LineField(r, "AFFECTED AREA (m²)", V(s, "AffectedArea"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IN FAVOUR OF", V(s, "InFavourOf"), 1);
                        LineField(r, "FOR WHAT PURPOSE", V(s, "Purpose"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "WAS COMPENSATION PAID?", V(s, "CompensationPaid"), 1);
                        LineField(r, "DATE OF PAYMENT", V(s, "CompensationDate"), 1);
                        LineField(r, "AMOUNT R", V(s, "CompensationAmount"), 1);
                    });
                });
            });
        }

        private void BuildSection5DwellingAndLand(ColumnDescriptor col)
        {
            var s = S("Section6");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 5: DESCRIPTION OF RESIDENTIAL DWELLING").Bold();

                    x.Item().Text("5.1 MAIN DWELLING").SemiBold();
                    x.Item().Row(r =>
                    {
                        LineField(r, "BEDROOMS", V(s, "Bedrooms"), 1);
                        LineField(r, "BATHROOMS", V(s, "Bathrooms"), 1);
                        LineField(r, "KITCHEN", V(s, "Kitchen"), 1);
                        LineField(r, "LOUNGE", V(s, "Lounge"), 1);
                        LineField(r, "DINING ROOM", V(s, "DiningRoom"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "LOUNGE WITH DINING ROOM", V(s, "LoungeWithDining"), 1);
                        LineField(r, "STUDY", V(s, "Study"), 1);
                        LineField(r, "PLAYROOM", V(s, "Playroom"), 1);
                        LineField(r, "TELEVISION", V(s, "TelevisionRoom", "Television"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "LAUNDRY", V(s, "Laundry"), 1);
                        LineField(r, "SEPARATE TOILET", V(s, "SeparateToilet"), 1);
                        LineField(r, "OTHER", V(s, "Other1"), 1);
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
                        LineField(r, "NON-AGRICULTURAL (ha)", V(s, "NonAgriculturalHa"), 1);
                        LineField(r, "GRAZING (ha)", V(s, "GrazingHa"), 1);
                        LineField(r, "UNDER IRRIGATION (ha)", V(s, "UnderIrrigationHa"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "DRY LAND (ha)", V(s, "DryLandHa"), 1);
                        LineField(r, "PERMANENT CROPS (ha)", V(s, "PermanentCropsHa"), 1);
                        LineField(r, "OTHER (ha)", V(s, "OtherHa1"), 1);
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
            var s = S("Section7");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("5.5 OTHER").Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "AFFECTED BY LAND CLAIM?", V(s, "LandClaimYesNo"), 1);
                        LineField(r, "DATE OF CLAIM", V(s, "LandClaimDate"), 1);
                        LineField(r, "GAZETTE NO.", V(s, "GazetteNo"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "DO YOU HAVE WATER RIGHTS?", V(s, "WaterRightsYesNo"), 1);
                        LineField(r, "DETAILS", V(s, "WaterRightsDetails"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "APPLIED FOR REZONING / CONSENT USE?", V(s, "RezoningYesNo"), 1);
                        LineField(r, "DETAILS", V(s, "RezoningDetails"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "HAS PROPERTY BEEN EXCISED?", V(s, "ExcisedYesNo"), 1);
                        LineField(r, "NEW FARM DESCRIPTION", V(s, "NewFarmDescription"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "HAS TOWNSHIP APPLIED FOR / PROCLAIMED?", V(s, "TownshipYesNo"), 1);
                        LineField(r, "DETAILS", V(s, "TownshipDetails"), 2);
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
            var s = S("Section8");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 6: MARKET INFORMATION").Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "CURRENT ASKING PRICE", V(s, "CurrentAskingPrice", "AskingPrice"), 1);
                        LineField(r, "OFFER RECEIVED", V(s, "CurrentOfferReceived", "OfferReceived"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "ASKING PRICE IN LAST 3 YEARS", V(s, "PreviousAskingPrice"), 1);
                        LineField(r, "OFFER RECEIVED", V(s, "PreviousOfferReceived"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF AGENT", V(s, "AgentName"), 1);
                        LineField(r, "TEL NO.", V(s, "AgentTel"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "SALES TRANSACTIONS", V(s, "ComparableSales", "SalesTransactions"), 1);
                    });
                });
            });
        }

        private void BuildSection7ReviewDetails(ColumnDescriptor col)
        {
            var s = S("Section9");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 7: REVIEW DETAILS PARTICULARS TO BE REFLECTED IN VALUATION ROLL CHANGES REQUESTED BY OWNER").Bold();

                    x.Item().Row(r =>
                    {
                        LineField(r, "DESCRIPTION OF THE PROPERTY / UNIT NO.", V(s, "DescriptionOfProperty", "PropertyDescription"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "PHYSICAL ADDRESS", V(s, "PhysicalAddress"), 2);
                        LineField(r, "CATEGORY", V(s, "Category"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "EXTENT", V(s, "Extent"), 1);
                        LineField(r, "MARKET VALUE", V(s, "MarketValue"), 1);
                        LineField(r, "WITH EFFECT DATE", V(s, "WithEffectDate", "EffectiveDate"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF OWNER", V(s, "OwnerName", "NameOfOwner"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "ADVERSE FEATURES / FURTHER REASONS", V(s, "AdverseFeatures", "FurtherReasons"), 1);
                    });
                });
            });
        }
    }
}