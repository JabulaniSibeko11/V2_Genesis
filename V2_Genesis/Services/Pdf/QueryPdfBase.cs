
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections;
using System.Globalization;
using V2_Genesis.Models;

namespace GV_Forms.Pdf
{
    public abstract class QueryPdfBase : IDocument
    {
        protected readonly InquiryAggregate Data;
        protected readonly Wording Wording;

        protected QueryPdfBase(InquiryAggregate data, Wording wording)
        {
            Data = data;
            Wording = wording;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    BuildHeader(col);
                    BuildPropertyIntro(col);
                    BuildOwnerInfo(col);
                    BuildRepresentative(col);
                    BuildReasonChecklist(col);
                    BuildMotivation(col);

                    BuildRemainingSections(col);
                });
            });
        }

        protected abstract void BuildRemainingSections(ColumnDescriptor col);

        protected virtual string QueryNumber()
        {
            try
            {
                dynamic m = Data.Main;
                return Convert.ToString(m?.Query_No ?? m?.QueryNo ?? m?.ReferenceNo ?? m?.Objection_No ?? m?.Appeal_No) ?? "";
            }
            catch
            {
                return "";
            }
        }

        protected dynamic? S(string key) => Data.GetSection<dynamic>(key);

        protected string V(dynamic? obj, params string[] names)
        {
            if (obj == null) return "";

            foreach (var name in names)
            {
                try
                {
                    var dict = obj as IDictionary<string, object>;
                    if (dict != null && dict.TryGetValue(name, out var val) && val != null)
                        return Convert.ToString(val) ?? "";

                    var type = obj.GetType();
                    var prop = type.GetProperty(name);
                    if (prop != null)
                    {
                        var value = prop.GetValue(obj);
                        if (value != null) return Convert.ToString(value) ?? "";
                    }
                }
                catch
                {
                }
            }

            return "";
        }

        protected static void RoundedBlock(ColumnDescriptor col, Action<IContainer> content)
        {
            col.Item()
               .Border(1)
               .Padding(8)
               
               .Element(content);
        }

        protected static void LineField(RowDescriptor row, string label, string value, float relative = 1)
        {
            row.RelativeItem(relative).Column(col =>
            {
                col.Item().Text(label).SemiBold().FontSize(8);
                col.Item().BorderBottom(1).PaddingBottom(2).Text(value ?? "");
            });
        }

        protected void BuildHeader(ColumnDescriptor col)
        {
            var qno = QueryNumber();

            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(GetHeadingLeft()).Bold().FontSize(14);
                    left.Item().Text("City of Johannesburg").FontSize(10);
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().Text($"QUERY NO. {qno}").Bold().AlignRight();
                    right.Item().PaddingTop(8).Text("VALUATION SERVICES").Bold().AlignRight();
                    right.Item().Text("valuationenquiries@joburg.org.za").FontSize(8).FontColor(Colors.Blue.Darken2).AlignRight();
                    right.Item().PaddingTop(6).Text("SECTION 78 QUERY FORM").Bold().FontSize(16).AlignRight();
                });
            });
        }

        protected abstract string GetHeadingLeft();

        protected virtual void BuildPropertyIntro(ColumnDescriptor col)
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
                        LineField(r, "ERF/UNIT NO.", V(s1, "Erf_Unit_No", "ErfUnitNo", "UnitNo", "ErfNo"), 1);
                        LineField(r, "SUBURB/SCHEME NAME", V(s1, "Suburb_Scheme_Name", "SuburbSchemeName", "Suburb", "SchemeName", "Property_Desc"), 2);
                    });
                });
            });
        }

        protected virtual void BuildOwnerInfo(ColumnDescriptor col)
        {
            var s1 = S("Section1");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("SECTION 1: OWNER INFORMATION").Bold();
                    x.Item().Text("1.1 OWNER").Bold().FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "REGISTERED OWNER OF PROPERTY", V(s1, "OwnerName", "RegisteredOwner", "Owner", "Name"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IDENTITY NO.", V(s1, "IdentityNo", "IdNo"), 1);
                        LineField(r, "COMPANY OR C.C REGISTRATION NO.", V(s1, "CompanyRegNo", "RegistrationNo"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "PHYSICAL ADDRESS OF OWNER", V(s1, "OwnerPhysicalAddress", "PhysicalAddress"), 3);
                        LineField(r, "CODE", V(s1, "OwnerPhysicalCode", "PhysicalCode"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "POSTAL ADDRESS OF OWNER", V(s1, "OwnerPostalAddress", "PostalAddress"), 3);
                        LineField(r, "CODE", V(s1, "OwnerPostalCode", "PostalCode"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "HOME", V(s1, "OwnerTelHome", "HomeTel", "TelephoneHome"), 1);
                        LineField(r, "WORK", V(s1, "OwnerTelWork", "WorkTel", "TelephoneWork"), 1);
                        LineField(r, "CELL", V(s1, "OwnerCell", "Cellphone"), 1);
                        LineField(r, "FAX", V(s1, "OwnerFax", "Fax"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "E-MAIL ADDRESS (compulsory)", V(s1, "OwnerEmail", "Email"), 1);
                    });
                });
            });
        }

        protected virtual void BuildRepresentative(ColumnDescriptor col)
        {
            var s2 = S("Section2");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Row(r =>
                    {
                        r.RelativeItem().Text("1.2 AUTHORISED REPRESENTATIVE OF THE OWNER*").Bold().FontSize(8);
                        r.RelativeItem().Text("OWNER DETAILS MUST BE COMPLETED").Bold().FontSize(8);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "NAME OF REPRESENTATIVE", V(s2, "RepresentativeName", "RepName", "Name"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "POSTAL ADDRESS", V(s2, "RepresentativePostalAddress", "PostalAddress"), 3);
                        LineField(r, "CODE", V(s2, "RepresentativePostalCode", "PostalCode"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "HOME", V(s2, "RepresentativeTelHome", "HomeTel"), 1);
                        LineField(r, "WORK", V(s2, "RepresentativeTelWork", "WorkTel"), 1);
                        LineField(r, "CELL", V(s2, "RepresentativeCell", "Cellphone"), 1);
                        LineField(r, "FAX", V(s2, "RepresentativeFax", "Fax"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "E-MAIL ADDRESS (compulsory)", V(s2, "RepresentativeEmail", "Email"), 1);
                    });
                });
            });
        }

        protected virtual void BuildReasonChecklist(ColumnDescriptor col)
        {
            var s3 = S("Section3");

            string[] labels =
            {
                "incorrectly omitted from the valuation roll;",
                "included in a municipality after the last general valuation;",
                "subdivided or consolidated after the last general valuation;",
                "of which the market value has substantially increased or decreased for any reason after the last general valuation;",
                "substantially incorrectly valued during the last general valuation;",
                "that must be revalued for any other exceptional reason;",
                "of which the category has changed; or",
                "the value of which was incorrectly recorded in the valuation roll as a result of a clerical or typing error."
            };

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("2. SELECT THE REASON FOR THE SUPPLEMENTARY REQUEST WITH AN “X” IN THE LAST COLUMN:")
                        .Bold().FontSize(8);

                    x.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(22);
                            cols.RelativeColumn();
                            cols.ConstantColumn(22);
                        });

                        for (int i = 0; i < labels.Length; i++)
                        {
                            var idx = i + 1;
                            var tick = V(s3, $"Reason{idx}", $"R{idx}", $"Option{idx}", $"Item{idx}")?.Trim();

                            t.Cell().Border(1).Padding(4).Text(((char)('a' + i)).ToString());
                            t.Cell().Border(1).Padding(4).Text(labels[i]);
                            t.Cell().Border(1).Padding(4).AlignCenter().Text(
                                string.Equals(tick, "true", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(tick, "yes", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(tick, "x", StringComparison.OrdinalIgnoreCase)
                                    ? "X"
                                    : "");
                        }
                    });
                });
            });
        }

        protected virtual void BuildMotivation(ColumnDescriptor col)
        {
            var s4 = S("Section4");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text("3.1 Reasons/Motivation why above supplementary is to be done").Bold();

                    string motivation = V(s4, "Motivation", "Reason", "Reasons", "Narrative", "Comments");
                    if (string.IsNullOrWhiteSpace(motivation))
                        motivation = new string('\n', 10);

                    x.Item().Border(1).MinHeight(180).Padding(8).Text(motivation);
                });
            });
        }

        protected virtual void BuildDeclaration(ColumnDescriptor col, string sectionNumber)
        {
            var sDecl = S(sectionNumber);

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text($"{sectionNumber.Replace("Section", "SECTION ")}: DECLARATION").Bold();

                    x.Item().Text(
                        "ATTENTION IS HEREBY DRAWN TO SECTION 42(2) OF THE ACT WHICH STATES THAT WHERE ANY DOCUMENT, INFORMATION OR PARTICULARS WERE NOT PROVIDED WHEN REQUIRED IN TERMS OF SUBSECTION 42(1) OF THE ACT AND THE OWNER CONCERNED RELIES ON SUCH DOCUMENT, INFORMATION OR PARTICULARS IN AN APPEAL TO AN APPEAL BOARD, THE APPEAL BOARD MAY MAKE AN ORDER AS TO COSTS IN TERMS OF SECTION 70 OF THE ACT IF THE APPEAL BOARD IS OF THE VIEW THAT THE FAILURE TO SO HAVE PROVIDED ANY SUCH DOCUMENT, INFORMATION OR PARTICULARS HAS PLACED AN UNNECESSARY BURDEN ON THE FUNCTIONS OF THE MUNICIPAL VALUER OR THE APPEAL BOARD.")
                        .FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "I / WE", V(sDecl, "Declarer", "OwnerName", "Name"), 1);
                        LineField(r, "YEAR", V(sDecl, "Year"), 1);
                        LineField(r, "MONTH", V(sDecl, "Month"), 1);
                        LineField(r, "DAY", V(sDecl, "Day"), 1);
                        LineField(r, "SIGNATURE", V(sDecl, "Signature"), 2);
                    });
                });
            });
        }

        protected virtual void BuildAdminReceipt(ColumnDescriptor col)
        {
            var sAdmin = S("Section99"); // optional extra result set if you add one

            col.Item().PaddingTop(6).Text("Property Description:").Bold();
            col.Item()
       .BorderBottom(1)
       .PaddingBottom(4)
       .Text((string)V(sAdmin, "PropertyDescription"));

            col.Item().PaddingTop(10).Text("ADMINISTRATION OFFICER RECEIVED SECTION 78 QUERY: (Compulsory)").Bold();

            col.Item().PaddingTop(10).Row(r =>
            {
                LineField(r, "NAME AND SURNAME", V(sAdmin, "AdminOfficerName"), 1);
                LineField(r, "SIGNATURE", V(sAdmin, "AdminOfficerSignature"), 1);
            });

            col.Item().PaddingTop(6).Row(r =>
            {
                LineField(r, "DATE", V(sAdmin, "AdminOfficerDate"), 1);
            });

            col.Item().PaddingTop(8).Text("valuationenquiries@joburg.org.za")
               .FontColor(Colors.Blue.Darken2)
               .Underline();
        }
    }
}