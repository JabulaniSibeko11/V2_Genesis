
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
        protected readonly IWebHostEnvironment Environment;

        protected QueryPdfBase(
            InquiryAggregate data,
            Wording wording,
            IWebHostEnvironment environment)
        {
            Data = data;
            Wording = wording;
            Environment = environment;
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

        protected string JoinV(dynamic? obj, params string[] names)
        {
            return string.Join(
                ", ",
                names.Select(name => V(obj, name))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()));
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

            var banner1Path = Path.Combine(
                Environment.WebRootPath,
                "Images",
                "banner1.webp");
            var banner2Path = Path.Combine(
                Environment.WebRootPath,
                "Images",
                "Banner2.png");

            if (File.Exists(banner1Path) && File.Exists(banner2Path))
            {
                var banner1Bytes = File.ReadAllBytes(banner1Path);
                var banner2Bytes = File.ReadAllBytes(banner2Path);

                col.Item().PaddingBottom(6).Row(row =>
                {
                    row.RelativeItem(4).Row(banners =>
                    {
                        banners.ConstantItem(95)
                            .Height(70)
                            .AlignMiddle()
                            .Image(banner1Bytes, ImageScaling.FitArea);
                        banners.ConstantItem(12);
                        banners.RelativeItem()
                            .Height(70)
                            .AlignMiddle()
                            .Image(banner2Bytes, ImageScaling.FitArea);
                    });

                    row.RelativeItem(6)
                        .PaddingLeft(18)
                        .PaddingTop(4)
                        .Column(contact =>
                        {
                            contact.Item().Text("City of Johannesburg")
                                .Bold().FontSize(12);
                            contact.Item().Text("Group Finance: Valuation Services")
                                .FontSize(9);
                            contact.Item().PaddingTop(4).LineHorizontal(1);
                            contact.Item().PaddingTop(6)
                                .Text("Phone 011 407-6622 or 011 407-6597")
                                .FontSize(8);
                            contact.Item().Text("valuationenquiries@joburg.org.za")
                                .FontSize(8)
                                .FontColor(Colors.Blue.Darken2);
                        });
                });
            }

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
                        LineField(r, "ERF/UNIT NO.",
                            V(main, "ERF", "Erf", "Unit_key", "Unit_Key", "Property_id", "Property_ID"), 1);
                        LineField(r, "SUBURB/SCHEME NAME",
                            V(main, "Town", "Town_Name", "Property_Desc"), 2);
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
                        LineField(r, "REGISTERED OWNER OF PROPERTY",
                            V(s1, "Owner_Name", "OwnerName", "RegisteredOwner", "Owner", "Name"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IDENTITY NO.",
                            V(s1, "Owner_Identity", "IdentityNo", "IdNo"), 1);
                        LineField(r, "COMPANY OR C.C REGISTRATION NO.",
                            V(s1, "Owner_Company", "CompanyRegNo", "RegistrationNo"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "PHYSICAL ADDRESS OF OWNER",
                            JoinV(s1, "Owner_Address_1", "Owner_Address_2", "Owner_Address_3", "Owner_Address_4"), 3);
                        LineField(r, "CODE", V(s1, "Owner_Address_5"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "POSTAL ADDRESS OF OWNER",
                            JoinV(s1, "Owner_Postal_1", "Owner_Postal_2", "Owner_Postal_3", "Owner_Postal_4"), 3);
                        LineField(r, "CODE", V(s1, "Owner_Postal_5"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "HOME", V(s1, "Owner_Home_Phone", "OwnerTelHome", "HomeTel"), 1);
                        LineField(r, "WORK", V(s1, "Owner_Work_Phone", "OwnerTelWork", "WorkTel"), 1);
                        LineField(r, "CELL", V(s1, "Owner_Cell_Phone", "OwnerCell", "Cellphone"), 1);
                        LineField(r, "FAX", V(s1, "Owner_Fax_Phone", "OwnerFax", "Fax"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "E-MAIL ADDRESS (compulsory)",
                            V(s1, "Owner_Email", "OwnerEmail", "Email"), 1);
                    });
                });
            });
        }

        protected virtual void BuildRepresentative(ColumnDescriptor col)
        {
            var s1 = S("Section1");

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
                        LineField(r, "NAME OF REPRESENTATIVE",
                            V(s1, "Representative_name", "RepresentativeName", "RepName", "Name"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "POSTAL ADDRESS",
                            JoinV(s1, "Rep_Postal_1", "Rep_Postal_2", "Rep_Postal_3", "Rep_Postal_4"), 3);
                        LineField(r, "CODE", V(s1, "Rep_Postal_5"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "HOME", V(s1, "Rep_Home_Phone", "RepresentativeTelHome", "HomeTel"), 1);
                        LineField(r, "WORK", V(s1, "Rep_Work_Phone", "RepresentativeTelWork", "WorkTel"), 1);
                        LineField(r, "CELL", V(s1, "Rep_Cell_Phone", "RepresentativeCell", "Cellphone"), 1);
                        LineField(r, "FAX", V(s1, "Rep_Fax_Phone", "RepresentativeFax", "Fax"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "E-MAIL ADDRESS (compulsory)",
                            V(s1, "Rep_Email", "RepresentativeEmail", "Email"), 1);
                    });
                });
            });
        }

        protected virtual void BuildReasonChecklist(ColumnDescriptor col)
        {
            var reasons = S("Section2Query");

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
                            var optionName = $"Option_{(char)('A' + i)}";
                            string tick = Convert.ToString(
                                V(reasons, optionName))?.Trim() ?? string.Empty;

                            t.Cell().Border(1).Padding(4).Text(((char)('a' + i)).ToString());
                            t.Cell().Border(1).Padding(4).Text(labels[i]);
                            t.Cell().Border(1).Padding(4).AlignCenter().Text(
                                string.Equals(tick, "true", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(tick, "yes", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(tick, "on", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(tick, "1", StringComparison.OrdinalIgnoreCase) ||
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

            string declarationDateText = Convert.ToString(
                V(sDecl, "Declaration_Date", "DeclarationDate")) ?? string.Empty;
            var year = string.Empty;
            var month = string.Empty;
            var day = string.Empty;

            if (DateTime.TryParse(
                declarationDateText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out DateTime declarationDate)
                || DateTime.TryParse(
                    declarationDateText,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out declarationDate))
            {
                year = declarationDate.Year.ToString(CultureInfo.InvariantCulture);
                month = declarationDate.Month.ToString("00", CultureInfo.InvariantCulture);
                day = declarationDate.Day.ToString("00", CultureInfo.InvariantCulture);
            }

            string signatureValue = Convert.ToString(
                V(
                    sDecl,
                    "Signature_Picture",
                    "SignaturePicture",
                    "Signature")) ?? string.Empty;

            string declarerName = Convert.ToString(
                V(
                    sDecl,
                    "Signature_Name",
                    "SignatureName",
                    "Declarer",
                    "OwnerName",
                    "Name")) ?? string.Empty;

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
                        LineField(
                            r,
                            "I / WE",
                            declarerName,
                            2);
                        LineField(r, "YEAR", year, 1);
                        LineField(r, "MONTH", month, 1);
                        LineField(r, "DAY", day, 1);
                    });

                    x.Item().PaddingTop(5).Row(r =>
                    {
                        r.ConstantItem(70)
                            .Text("SIGNATURE")
                            .SemiBold()
                            .FontSize(8);

                        if (TryGetImageBytes(
                            signatureValue,
                            out byte[] signatureBytes))
                        {
                            r.RelativeItem()
                                .Height(45)
                                .BorderBottom(1)
                                .AlignLeft()
                                .Image(signatureBytes, ImageScaling.FitArea);
                        }
                        else
                        {
                            r.RelativeItem()
                                .BorderBottom(1)
                                .PaddingBottom(2)
                                .Text((string)signatureValue);
                        }
                    });
                });
            });
        }

        private static bool TryGetImageBytes(string? value, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var raw = value.Trim();
            var commaIndex = raw.IndexOf(',');

            if (raw.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) &&
                commaIndex >= 0)
            {
                raw = raw[(commaIndex + 1)..];
            }

            try
            {
                bytes = Convert.FromBase64String(raw);
                return bytes.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
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
