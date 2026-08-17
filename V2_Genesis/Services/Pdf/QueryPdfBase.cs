
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

                page.Header().Column(header =>
                {
                    BuildHeader(header);
                    header.Item().PaddingTop(5).LineHorizontal(1);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(8);

                    BuildBrandBanner(col);
                    BuildPropertyIntro(col);
                    BuildOwnerInfo(col);
                    BuildRepresentative(col);

                    // The official Section 78 Query forms contain the supplementary
                    // reason checklist and motivation page. The official Review forms
                    // do not - they move straight from owner/representative details
                    // to Property Details.
                    if (InquiryUpper == "QUERY")
                    {
                        BuildReasonChecklist(col);
                        BuildMotivation(col);
                    }

                    BuildRemainingSections(col);
                });

                page.Footer()
                    .AlignRight()
                    .Text(text =>
                    {
                        text.Span($"SECTION 78 {InquiryUpper} - Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        }

        protected abstract void BuildRemainingSections(ColumnDescriptor col);

        protected virtual string QueryNumber()
        {
            dynamic? main = Data.Main;
            var reviewReference = V(S("Section2Query"), "Review_No");
            var reference = FirstValue(
                InquiryUpper == "REVIEW" ? reviewReference : null,
                InquiryUpper == "REVIEW" ? V(main, "Review_No", "ReviewNo") : null,
                V(main, "Query_No", "QueryNo", "ReferenceNo", "Objection_No", "Appeal_No"));

            if (InquiryUpper == "REVIEW" &&
                !string.IsNullOrWhiteSpace(reference) &&
                !reference.EndsWith("-R", StringComparison.OrdinalIgnoreCase))
            {
                reference += "-R";
            }

            return reference;
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
               .Border(2)
               .Padding(10)

               .Element(content);
        }

        protected static void LineField(RowDescriptor row, string label, string value, float relative = 1)
        {
            row.RelativeItem(relative).Column(col =>
            {
                col.Item().Text(label).SemiBold().FontSize(8);
                col.Item()
                    .BorderBottom(1)
                    .PaddingBottom(2)
                    .Text(value ?? "")
                    .FontSize(8)
                    .AlignCenter();
            });
        }

        protected static void ComparisonLine(
            ColumnDescriptor column,
            string label,
            string? rollValue,
            string? requestedValue)
        {
            column.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Row(left =>
                {
                    left.AutoItem()
                        .Text(label)
                        .FontSize(8);

                    left.RelativeItem()
                        .BorderBottom(1)
                        .PaddingBottom(2)
                        .Text(rollValue ?? "")
                        .FontSize(8)
                        .AlignCenter();
                });

                row.RelativeItem()
                    .PaddingLeft(10)
                    .BorderBottom(1)
                    .PaddingBottom(2)
                    .Text(requestedValue ?? "")
                    .FontSize(8)
                    .AlignCenter();
            });
        }

        protected string InquiryUpper =>
            string.Equals(
                Wording.InquiryLabel,
                "REVIEW",
                StringComparison.OrdinalIgnoreCase)
                ? "REVIEW"
                : "QUERY";

        protected string NumberLabel =>
            InquiryUpper == "REVIEW"
                ? "REVIEW NO."
                : "QUERY NO.";

        protected string FormTitle =>
            $"SECTION 78 {InquiryUpper} FORM";

        protected bool IsReview => InquiryUpper == "REVIEW";

        // The Query templates start property content at section 4 because sections
        // 2/3 are the supplementary reason and motivation. Review templates omit
        // those pages and therefore start property content at section 2.
        protected int FormSection(int querySection, int reviewSection) =>
            IsReview ? reviewSection : querySection;

        protected string PropertyFormType
        {
            get
            {
                dynamic? main = Data.Main;
                var value = V(main, "Property_Type", "PropertyType").Trim();

                if (value.Contains("Multiple", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("Multi", StringComparison.OrdinalIgnoreCase))
                    return "Multi";

                if (value.Contains("Agric", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("Farm", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("Vacant", StringComparison.OrdinalIgnoreCase))
                    return "Agric";

                if (value.Contains("Business", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("Commercial", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("Industrial", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("Office", StringComparison.OrdinalIgnoreCase))
                    return "Bus";

                return "Res";
            }
        }

        protected string PropertyDescription()
        {
            dynamic? main = Data.Main;

            return V(
                main,
                "Property_Desc",
                "PropertyDescription",
                "A_Property_Desc");
        }

        protected string ErfOrUnitNumber()
        {
            var description = PropertyDescription();

            if (string.IsNullOrWhiteSpace(description))
                return "";

            var match = System.Text.RegularExpressions.Regex.Match(
                description,
                @"(?:ERF|UNIT|PORTION)\s+(\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return match.Success
                ? match.Groups[1].Value
                : "";
        }

        protected string FarmNumber()
        {
            var description = PropertyDescription();
            var match = System.Text.RegularExpressions.Regex.Match(
                description,
                @"(?:FARM\s+)?(\d+)\s*-?\s*([A-Z]{1,3})\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value : ErfOrUnitNumber();
        }

        protected string RegistrationDivision()
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                PropertyDescription(),
                @"(?:FARM\s+)?\d+\s*-?\s*([A-Z]{1,3})\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : "";
        }

        protected string JoinValues(
            dynamic? source,
            params string[] propertyNames)
        {
            return string.Join(
                " ",
                propertyNames
                    .Select(name => V(source, name).Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        protected static string Money(string? value)
        {
            var text = value?.Trim();

            if (string.IsNullOrWhiteSpace(text))
                return "";

            var numericText = new string(
                text.Where(character =>
                    char.IsDigit(character) ||
                    character == '.' ||
                    character == ',' ||
                    character == '-')
                .ToArray())
                .Replace(",", "");

            return decimal.TryParse(
                numericText,
                NumberStyles.Number | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var amount)
                    ? "R " + amount.ToString(
                        "N0",
                        CultureInfo.GetCultureInfo("en-ZA"))
                    : text.StartsWith(
                        "R",
                        StringComparison.OrdinalIgnoreCase)
                        ? text
                        : "R " + text;
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
                    right.Item().Text($"{NumberLabel} {qno}").Bold().AlignRight();
                    right.Item().PaddingTop(8).Text("VALUATION SERVICES").Bold().AlignRight();
                    right.Item().Text("valuationenquiries@joburg.org.za").FontSize(8).FontColor(Colors.Blue.Darken2).AlignRight();
                    right.Item().PaddingTop(6).Text(FormTitle).Bold().FontSize(16).AlignRight();
                });
            });
        }

        protected abstract string GetHeadingLeft();

        protected virtual void BuildPropertyIntro(ColumnDescriptor col)
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
                        LineField(r, "ERF/UNIT NO.", ErfOrUnitNumber(), 1);
                        LineField(r, "SUBURB/SCHEME NAME", PropertyDescription(), 2);
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
                        LineField(r, "REGISTERED OWNER OF PROPERTY", V(s1, "Owner_Name", "OwnerName", "RegisteredOwner"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "IDENTITY NO.", V(s1, "Owner_Identity", "IdentityNo", "IdNo"), 1);
                        LineField(r, "COMPANY OR C.C REGISTRATION NO.", V(s1, "Owner_Company", "CompanyRegNo", "RegistrationNo"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(
                            r,
                            "PHYSICAL ADDRESS OF OWNER",
                            JoinValues(s1, "Owner_Address_1", "Owner_Address_2", "Owner_Address_3", "Owner_Address_4"),
                            3);
                        LineField(r, "CODE", V(s1, "Owner_Address_5", "OwnerPhysicalCode"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(
                            r,
                            "POSTAL ADDRESS OF OWNER",
                            JoinValues(s1, "Owner_Postal_1", "Owner_Postal_2", "Owner_Postal_3", "Owner_Postal_4"),
                            3);
                        LineField(r, "CODE", V(s1, "Owner_Postal_5", "OwnerPostalCode"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "HOME", V(s1, "Owner_Home_Phone", "OwnerTelHome"), 1);
                        LineField(r, "WORK", V(s1, "Owner_Work_Phone", "OwnerTelWork"), 1);
                        LineField(r, "CELL", V(s1, "Owner_Cell_Phone", "OwnerCell"), 1);
                        LineField(r, "FAX", V(s1, "Owner_Fax_Phone", "OwnerFax"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "E-MAIL ADDRESS (compulsory)", V(s1, "Owner_Email", "OwnerEmail"), 1);
                    });
                });
            });
        }

        protected virtual void BuildRepresentative(ColumnDescriptor col)
        {
            var s2 = S("Section1");

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
                        LineField(r, "NAME OF REPRESENTATIVE", V(s2, "Representative_name", "RepresentativeName"), 2);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(
                            r,
                            "POSTAL ADDRESS",
                            JoinValues(s2, "Rep_Postal_1", "Rep_Postal_2", "Rep_Postal_3", "Rep_Postal_4"),
                            3);
                        LineField(r, "CODE", V(s2, "Rep_Postal_5", "RepresentativePostalCode"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "HOME", V(s2, "Rep_Home_Phone", "RepresentativeTelHome"), 1);
                        LineField(r, "WORK", V(s2, "Rep_Work_Phone", "RepresentativeTelWork"), 1);
                        LineField(r, "CELL", V(s2, "Rep_Cell_Phone", "RepresentativeCell"), 1);
                        LineField(r, "FAX", V(s2, "Rep_Fax_Phone", "RepresentativeFax"), 1);
                    });

                    x.Item().Row(r =>
                    {
                        LineField(r, "E-MAIL ADDRESS (compulsory)", V(s2, "Rep_Email", "RepresentativeEmail"), 1);
                    });
                });
            });
        }

        protected virtual void BuildReasonChecklist(ColumnDescriptor col)
        {
            var s3 = S("Section2Query");

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
                    x.Item().Text($"2. SELECT THE REASON FOR THE SUPPLEMENTARY {InquiryUpper} WITH AN X IN THE LAST COLUMN:")
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
                            var optionName = $"Option_{(char)('A' + i)}";
                            var tick = V(s3, optionName, $"Reason{idx}", $"Option{idx}")?.Trim();

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
            var s4 = S("Section2Query");

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text($"3.1 Reasons/Motivation for this supplementary {InquiryUpper.ToLowerInvariant()}").Bold();

                    string motivation = V(
                        s4,
                        "Motivation_for_Supp_Request",
                        "Motivation",
                        "Reason");
                    if (string.IsNullOrWhiteSpace(motivation))
                        motivation = new string('\n', 10);

                    x.Item().Border(1).MinHeight(520).Padding(8).Text(motivation);
                });
            });
        }

        protected virtual void BuildDeclaration(
            ColumnDescriptor col,
            string sectionKey,
            int displaySectionNumber)
        {
            var sDecl = S(sectionKey);
            var declarationDate = V(sDecl, "Declaration_Date");



            DateTime? parsedDeclarationDate =
                DateTime.TryParse(
                    declarationDate,
                    out DateTime parsedDate)
                    ? parsedDate
                    : null;

            RoundedBlock(col, c =>
            {
                c.Column(x =>
                {
                    x.Item().Text($"SECTION {displaySectionNumber}: DECLARATION").Bold();

                    x.Item().Text(
                        "ATTENTION IS HEREBY DRAWN TO SECTION 42(2) OF THE ACT WHICH STATES THAT WHERE ANY DOCUMENT, INFORMATION OR PARTICULARS WERE NOT PROVIDED WHEN REQUIRED IN TERMS OF SUBSECTION 42(1) OF THE ACT AND THE OWNER CONCERNED RELIES ON SUCH DOCUMENT, INFORMATION OR PARTICULARS IN AN APPEAL TO AN APPEAL BOARD, THE APPEAL BOARD MAY MAKE AN ORDER AS TO COSTS IN TERMS OF SECTION 70 OF THE ACT IF THE APPEAL BOARD IS OF THE VIEW THAT THE FAILURE TO SO HAVE PROVIDED ANY SUCH DOCUMENT, INFORMATION OR PARTICULARS HAS PLACED AN UNNECESSARY BURDEN ON THE FUNCTIONS OF THE MUNICIPAL VALUER OR THE APPEAL BOARD.")
                        .FontSize(8);

                    x.Item().Row(r =>
                    {
                        LineField(r, "I / WE", V(sDecl, "Signature_Name", "Declarer", "Name"), 2);
                        LineField(r, "YEAR", parsedDeclarationDate?.Year.ToString() ?? "", 1);
                        LineField(r, "MONTH", parsedDeclarationDate?.Month.ToString("00") ?? "", 1);
                        LineField(r, "DAY", parsedDeclarationDate?.Day.ToString("00") ?? "", 1);
                    });

                    x.Item().PaddingTop(6).Row(r =>
                    {
                        r.AutoItem()
                            .Text("SIGNATURE")
                            .SemiBold()
                            .FontSize(8);

                        r.RelativeItem()
                            .BorderBottom(1)
                            .Height(24)
                            .AlignCenter()
                            .AlignMiddle()
                            .Element(signatureContainer =>
                            {
                                var signature =
                                    V(sDecl, "Signature_Picture");

                                if (string.IsNullOrWhiteSpace(signature))
                                    return;


                                if (string.IsNullOrWhiteSpace(signature))
                                    return;

                                try
                                {
                                    string base64 = signature.Split(',').Last();
                                    byte[] bytes = Convert.FromBase64String(base64);

                                    signatureContainer.Image(
                                        bytes,
                                        ImageScaling.FitArea);
                                }
                                catch
                                {
                                    // Leave the signature area empty when the data is invalid.
                                }
                            });
                    });
                });
            });
        }

        private void BuildBrandBanner(ColumnDescriptor col)
        {
            var banner1Path = Path.Combine(
                Environment.WebRootPath,
                "Images",
                "banner1.webp");

            var banner2Path = Path.Combine(
                Environment.WebRootPath,
                "Images",
                "Banner2.png");

            if (!File.Exists(banner1Path) || !File.Exists(banner2Path))
                return;

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
                        contact.Item()
                            .Text("City of Johannesburg")
                            .Bold()
                            .FontSize(12);

                        contact.Item()
                            .Text("Group Finance: Valuation Services")
                            .FontSize(9);

                        contact.Item().PaddingTop(4).LineHorizontal(1);

                        contact.Item()
                            .PaddingTop(6)
                            .Text("Phone 011 407-6622 or 011 407-6597")
                            .FontSize(8);

                        contact.Item()
                            .Text("valuationenquiries@joburg.org.za")
                            .FontSize(8)
                            .FontColor(Colors.Blue.Darken2);
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
        .Text((string)FirstValue(
            (string)V(sAdmin, "PropertyDescription"),
            PropertyDescription()))
        .AlignCenter();

            col.Item().PaddingTop(10).Text($"ADMINISTRATION OFFICER RECEIVED SECTION 78 {InquiryUpper}: (Compulsory)").Bold();

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

        protected static string FirstValue(
            params string?[] values)
        {
            return values.FirstOrDefault(
                       value => !string.IsNullOrWhiteSpace(value))
                   ?.Trim()
                   ?? "";
        }
    }
}
