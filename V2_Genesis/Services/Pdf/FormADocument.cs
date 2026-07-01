
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Buffers.Text;
using V2_Genesis.Models;

namespace GV_Forms.Pdf
{
    public class FormADocument : IDocument
    {
        private readonly InquiryAggregate _data;
        private readonly Wording _wording;
        private readonly IWebHostEnvironment _env;
        public FormADocument(InquiryAggregate data, Wording wording,IWebHostEnvironment env)
        {
            _data = data;
            _wording = wording;
            _env=env;
        }
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        private string GetFormHeaderColor()
        {
            return string.Equals(_wording.InquiryLabel, "APPEAL", StringComparison.OrdinalIgnoreCase)
                ? "#FF0000"
                : "#FFFF00";
        }

        private void BuildFullBannerHeader(
    IContainer container,
    byte[] banner1Bytes,
    byte[] banner2Bytes)
        {
            container.Row(row =>
            {
                // Left side: Joburg logo + GV banner
                row.RelativeItem(4).Row(bannerRow =>
                {
                    bannerRow.ConstantItem(95)
                        .Height(75)
                        .AlignMiddle()
                        .Image(banner1Bytes, ImageScaling.FitArea);

                    bannerRow.ConstantItem(15);

                    bannerRow.RelativeItem()
                        .Height(75)
                        .AlignMiddle()
                        .Image(banner2Bytes, ImageScaling.FitArea);
                });

                // Right side: use the remaining page width properly
                row.RelativeItem(6)
                    .PaddingLeft(20)
                    .PaddingTop(5)
                    .Column(contactCol =>
                    {
                        contactCol.Item()
                            .Text("City of Johannesburg")
                            .Bold()
                            .FontSize(12);

                        contactCol.Item()
                            .Text("Group Finance: Valuation Services")
                            .FontSize(10);

                        contactCol.Item()
                            .PaddingTop(5)
                            .LineHorizontal(1);

                        contactCol.Item()
                            .PaddingTop(8)
                            .Row(contactRow =>
                            {
                                contactRow.RelativeItem(4).Column(phoneCol =>
                                {
                                    phoneCol.Item()
                                        .Text("Phone 011 407-6622 or")
                                        .FontSize(8);

                                    phoneCol.Item()
                                        .PaddingTop(2)
                                        .Text("011 407-6597")
                                        .FontSize(8);
                                });

                                contactRow.RelativeItem(5).Column(webCol =>
                                {
                                    webCol.Item()
                                        .Hyperlink("https://www.joburg.org.za")
                                        .Text("www.joburg.org.za")
                                        .FontSize(8)
                                        .FontColor("#0000FF");

                                    webCol.Item()
                                        .PaddingTop(2)
                                        .Hyperlink("mailto:valuationenquiries@joburg.org.za")
                                        .Text("valuationenquiries@joburg.org.za")
                                        .FontSize(8)
                                        .FontColor("#0000FF");
                                });
                            });
                    });
            });
        }

        public void Compose(IDocumentContainer container)
        {
            //byte[] banner1Bytes = File.ReadAllBytes(Path.Combine("wwwroot", "img", "banner1.webp"));
            //byte[] banner2Bytes = File.ReadAllBytes(Path.Combine("wwwroot", "img", "banner2.png"));

            var banner1Path = Path.Combine(_env.WebRootPath, "Images", "banner1.webp");
            var banner2Path = Path.Combine(_env.WebRootPath, "Images", "banner2.png");

            byte[] banner1Bytes = System.IO.File.ReadAllBytes(banner1Path);
            byte[] banner2Bytes = System.IO.File.ReadAllBytes(banner2Path);

            // Fetch data sections
            var s1 = _data.GetSection<dynamic>("Section1");
            var s2 = _data.GetSection<dynamic>("Section2");
            var s3 = _data.GetSection<dynamic>("Section3");
            var s4 = _data.GetSection<dynamic>("Section4");
            var s5 = _data.GetSection<dynamic>("Section5");
            var s6 = _data.GetSection<dynamic>("Section6");
            var s7 = _data.GetSection<dynamic>("Section7");
            var s8 = _data.GetSection<dynamic>("Section8");
            var s9 = _data.GetSection<dynamic>("Section9");

            // Extract objection number, ERF/Unit No, and Area/Scheme Name
            string objectionNo = string.Empty;
            string erfUnitNo = string.Empty;
            string areaScheme = string.Empty;

            try
            {
                dynamic m = _data.Main;
                objectionNo = m?.Objection_No ?? m?.Appeal_No ?? string.Empty;
                areaScheme = _wording.InquiryLabel.Equals("APPEAL", StringComparison.OrdinalIgnoreCase)
        ? (m?.A_Property_Desc ?? string.Empty)
        : (m?.Property_Desc ?? string.Empty);

                string premiseId = m?.Premise_Id ?? string.Empty;

                // Check if it's a sectional title (ends with ST followed by unit number)
                if (!string.IsNullOrEmpty(premiseId) && premiseId.Contains("ST") && premiseId.Length >= 5)
                {
                    // Extract Unit No from last 5 digits for sectional title
                    string lastFive = premiseId.Substring(premiseId.Length - 5);
                    erfUnitNo = int.TryParse(lastFive, out int unitNumber) ? unitNumber.ToString() : lastFive;
                }
                else
                {
                    // For full title, extract ERF number from Property_Desc
                    if (!string.IsNullOrEmpty(areaScheme))
                    {
                        // Look for "ERF" followed by a number
                        var erfMatch = System.Text.RegularExpressions.Regex.Match(areaScheme, @"ERF\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (erfMatch.Success && erfMatch.Groups.Count > 1)
                        {
                            erfUnitNo = erfMatch.Groups[1].Value;
                        }
                    }
                }

                // If still empty, try to extract unit number from Property_Desc (e.g., "UNIT 6" -> "6")
                if (string.IsNullOrEmpty(erfUnitNo) && !string.IsNullOrEmpty(areaScheme))
                {
                    var unitMatch = System.Text.RegularExpressions.Regex.Match(areaScheme, @"UNIT\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (unitMatch.Success && unitMatch.Groups.Count > 1)
                    {
                        erfUnitNo = unitMatch.Groups[1].Value;
                    }
                }
            }
            catch { }

            // ===================== PAGE 1 (FULL HEADER WITH BANNERS + SECTION 1) =====================
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Colors.White);

                page.Header().Column(header =>
                {
                    // Top Header Row - Complete Erf/Unit No and Form A Objection
                    header.Item().Row(topRow =>
                    {
                        topRow.RelativeItem().Row(leftRow =>
                        {
                            leftRow.AutoItem().Text("Complete: Erf/Unit No:  ").FontSize(8);
                            leftRow.AutoItem().BorderBottom(1).PaddingBottom(2).Width(80).Text(erfUnitNo).FontSize(8).AlignCenter();
                            leftRow.AutoItem().PaddingLeft(10).Text("Area/Scheme Name:  ").FontSize(8);
                            leftRow.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(areaScheme).FontSize(8).AlignCenter();
                        });
                        // topRow.AutoItem().Text("Form A Objection").BackgroundColor("#FF0000").Bold().FontSize(9);

                        topRow.AutoItem().Text($"Form A {_wording.InquiryLabel}").BackgroundColor(GetFormHeaderColor()).Bold().FontSize(9);
                    });

                    // Page Number
                    header.Item().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(8).Italic();
                        text.CurrentPageNumber().FontSize(8).Italic();
                        text.Span(" of ").FontSize(8).Italic();
                        text.TotalPages().FontSize(8).Italic();
                    });

                    // Banners and Contact Information Row
                    header.Item().PaddingTop(5).Element(c =>
                        {
                            BuildFullBannerHeader(c, banner1Bytes, banner2Bytes);
                        });

                    header.Item().PaddingTop(10).Text("FORM A: RESIDENTIAL (FULL TITLE AND SECTIONAL TITLE USED FOR RESIDENTIAL PURPOSES)")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(5).Text("THE MUNICIPAL MANAGER")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem(1).Text("City of Johannesburg").FontSize(9).Bold();
                        row.RelativeItem(2).Row(objRow =>
                        {
                            // objRow.AutoItem().Text("OBJECTION NO.: ").FontSize(9).Bold();
                            objRow.AutoItem().Text($"{_wording.NumberLabel}: ").FontSize(9).Bold();
                            objRow.RelativeItem().BorderBottom(1).Text(objectionNo).FontSize(9).AlignCenter();
                        });
                    });

                    header.Item().PaddingTop(10).Border(2).Padding(10).Column(boxCol =>
                    {
                        boxCol.Item().Text(text =>
                        {
                            text.Span($"LODGING OF AN {_wording.InquiryLabel} AGAINST THE DECISION OF THE MUNICIPAL VALUER REGARDING MATTERS PERTAINING TO PROPERTY AS REFELECTED IN/OR OMITTED FROM THE VALUATION ROLL SUPPLEMENTARY").Bold().FontSize(8);
                            text.Span("* / ").Bold().FontSize(8);
                            text.Span("VALUATION ROLL").Bold().FontSize(8);
                            text.Span("* ( ").Bold().FontSize(8);
                            text.Span("*Delete whichever is not applicable").Bold().FontSize(8);
                            text.Span(" ) FOR THE PERIOD:").Bold().FontSize(8);
                        });

                        boxCol.Item().PaddingTop(10).Row(dateRow =>
                        {
                            dateRow.AutoItem().Text("1 JULY ").Bold().FontSize(8);
                            dateRow.AutoItem().BorderBottom(1).Width(100).Text("2023").FontSize(8).AlignCenter().Bold();
                            dateRow.AutoItem().PaddingLeft(10).Text("TO 30 JUNE ").Bold().FontSize(8);
                            dateRow.AutoItem().BorderBottom(1).Width(100).Text("2027").FontSize(8).AlignCenter().Bold();
                        });

                        boxCol.Item().PaddingTop(8).Text("DESCRIPTION OF PROPERTY IN RESPECT OF WHICH THE OBJECTION IS MADE")
                            .Bold().FontSize(8);

                        boxCol.Item().Text("(Complete a separate form for each entry objected to)")
                            .FontSize(8);

                        boxCol.Item().PaddingTop(5).Row(erfRow =>
                        {
                            erfRow.AutoItem().Text("ERF/UNIT NO.: ").FontSize(8);
                            erfRow.AutoItem().BorderBottom(1).Width(150).Text(erfUnitNo).FontSize(8).AlignCenter();
                            erfRow.AutoItem().PaddingLeft(15).Text("SUBURB/SCHEME NAME: ").FontSize(8);
                            erfRow.RelativeItem().BorderBottom(1).Text(areaScheme).FontSize(8).AlignCenter();
                        });
                    });
                });

                // SECTION 1 CONTENT ON PAGE 1
                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Element(c => { BuildSection1(c, s1); return c; });
                });
            });

            // ===================== PAGES 2-3 (SIMPLE HEADER + SECTIONS 2-6) =====================
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Colors.White);

                page.Header().Column(header =>
                {
                    // Top Header Row - Complete Erf/Unit No and Form A Objection
                    header.Item().Row(topRow =>
                    {
                        topRow.RelativeItem().Row(leftRow =>
                        {
                            leftRow.AutoItem().Text("Complete: Erf/Unit No:  ").FontSize(8);
                            leftRow.AutoItem().BorderBottom(1).PaddingBottom(2).Width(80).Text(erfUnitNo).FontSize(8).AlignCenter();
                            leftRow.AutoItem().PaddingLeft(10).Text("Area/Scheme Name:  ").FontSize(8);
                            leftRow.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(areaScheme).FontSize(8).AlignCenter();
                        });
                        // topRow.AutoItem().Text("Form A Objection").BackgroundColor("#FF0000").Bold().FontSize(9);
                        topRow.AutoItem().Text($"Form A {_wording.InquiryLabel}").BackgroundColor(GetFormHeaderColor()).Bold().FontSize(9);

                    });

                    // Page Number
                    header.Item().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(8).Italic();
                        text.CurrentPageNumber().FontSize(8).Italic();
                        text.Span(" of ").FontSize(8).Italic();
                        text.TotalPages().FontSize(8).Italic();
                    });

                    header.Item().PaddingTop(5).Text("FORM A: RESIDENTIAL (FULL TITLE AND SECTIONAL TITLE USED FOR RESIDENTIAL PURPOSES)")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(5).Text("THE MUNICIPAL MANAGER")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(1).Text("City of Johannesburg").FontSize(9).Bold();
                        row.RelativeItem(2).Row(objRow =>
                        {
                            //  objRow.AutoItem().Text("OBJECTION NO.: ").FontSize(9).Bold();
                            objRow.AutoItem().Text($"{_wording.NumberLabel}: ").FontSize(9).Bold();

                            objRow.RelativeItem().BorderBottom(1).Text(objectionNo).FontSize(9).AlignCenter();
                        });
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Element(c => { BuildSection2(c, s2); return c; });
                    col.Item().PaddingTop(10).Element(c => { BuildSection3(c, s3); return c; });
                    col.Item().PaddingTop(10).Element(c => { BuildSection4(c, s4); return c; });
                    col.Item().PaddingTop(10).Element(c => { BuildSection5(c, s5, erfUnitNo, areaScheme); return c; });
                    col.Item().PaddingTop(10).Element(c => { BuildSection6(c, s6); return c; });
                });
            });

            // ===================== PAGE 4 (FULL HEADER WITH BANNERS + SECTIONS 7-9) =====================
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Colors.White);

                page.Header().Column(header =>
                {
                    // Top Header Row - Complete Erf/Unit No and Form A Objection
                    header.Item().Row(topRow =>
                    {
                        topRow.RelativeItem().Row(leftRow =>
                        {
                            leftRow.AutoItem().Text("Complete: Erf/Unit No:  ").FontSize(8);
                            leftRow.AutoItem().BorderBottom(1).PaddingBottom(2).Width(80).Text(erfUnitNo).FontSize(8).AlignCenter();
                            leftRow.AutoItem().PaddingLeft(10).Text("Area/Scheme Name:  ").FontSize(8);
                            leftRow.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(areaScheme).FontSize(8).AlignCenter();
                        });

                        topRow.AutoItem().Text($"Form A {_wording.InquiryLabel}").BackgroundColor(GetFormHeaderColor()).Bold().FontSize(9);

                        //topRow.AutoItem().Text("Form A Objection").BackgroundColor("#FF0000").Bold().FontSize(9);
                    });

                    // Page Number
                    header.Item().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(8).Italic();
                        text.CurrentPageNumber().FontSize(8).Italic();
                        text.Span(" of ").FontSize(8).Italic();
                        text.TotalPages().FontSize(8).Italic();
                    });

                    // Banners and Contact Information Row
                    header.Item().PaddingTop(5).Row(row =>
                    {
                        // Left side - Banners (closer together)
                        row.AutoItem().Row(bannerRow =>
                        {
                            bannerRow.AutoItem().Height(70).AlignMiddle().AlignCenter().Image(banner1Bytes, ImageScaling.FitArea);
                            bannerRow.AutoItem().PaddingLeft(10).Height(70).AlignMiddle().AlignCenter().Image(banner2Bytes, ImageScaling.FitArea);
                        });

                        // Right side - Contact Information (closer to banners)
                        row.AutoItem().PaddingLeft(20).Column(contactCol =>
                        {
                            contactCol.Item().Text("City of Johannesburg").Bold().FontSize(10);
                            contactCol.Item().Text("Group Finance: Valuation Services").FontSize(9);
                            contactCol.Item().PaddingTop(5).LineHorizontal(1);
                            contactCol.Item().PaddingTop(8).Row(phoneRow =>
                            {
                                phoneRow.AutoItem().Column(phoneCol =>
                                {
                                    phoneCol.Item().Text("Phone 011 407-6622 or").FontSize(8);
                                    phoneCol.Item().PaddingTop(2).Text("       011 407-6597").FontSize(8);
                                });
                                phoneRow.AutoItem().PaddingLeft(10).Column(webCol =>
                                {
                                    webCol.Item().Hyperlink("https://www.joburg.org.za").Text("www.joburg.org.za").FontSize(8).FontColor("#0000FF");
                                    webCol.Item().PaddingTop(2).Hyperlink("mailto:valuationenquiries@joburg.org.za").Text("valuationenquiries@joburg.org.za").FontSize(8).FontColor("#0000FF");
                                });
                            });
                        });
                    });

                    header.Item().PaddingTop(10).Text("FORM A: RESIDENTIAL (FULL TITLE AND SECTIONAL TITLE USED FOR RESIDENTIAL PURPOSES)")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(5).Text("THE MUNICIPAL MANAGER")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem(1).Text("City of Johannesburg").FontSize(9).Bold();
                        row.RelativeItem(2).Row(objRow =>
                        {
                            objRow.AutoItem().Text($"{_wording.NumberLabel}: ").FontSize(9).Bold();

                            //objRow.AutoItem().Text("OBJECTION NO.: ").FontSize(9).Bold();
                            objRow.RelativeItem().BorderBottom(1).Text(objectionNo).FontSize(9).AlignCenter();
                        });
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Element(c => { BuildSection7(c, s7); return c; });

                    // OFFICIAL USE - Red Text with Line
                    col.Item().PaddingTop(10).LineHorizontal(1);
                    col.Item().PaddingTop(3).Text("OFFICIAL USE")
                        .FontColor("#FF0000")
                        .Bold()
                        .FontSize(9);
                    col.Item().PaddingTop(3).LineHorizontal(1);

                    col.Item().PaddingTop(10).Element(c => { BuildSection8(c, s8); return c; });
                    col.Item().PaddingTop(10).Element(c => { BuildSection9(c, s9); return c; });
                });

                // page.Footer().Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}",TextStyle.Default.Size(7).Italic());
            });
        }

        // Section 1: personal, objector, representative
        private void BuildSection1(IContainer container, dynamic s)
        {
            container.Column(col =>
            {
                // 1.1 OBJECTOR IS THE OWNER section
                col.Item().Border(2).Padding(10).Column(ownerBox =>
                {
                    ownerBox.Item().Text($"SECTION 1: {_wording.PartyLabel} INFORMATION").Bold().FontSize(9);
                    ownerBox.Item().PaddingTop(5).PaddingBottom(3).Text($"1.1  {_wording.PartyLabel} IS THE OWNER").Bold().FontSize(8);
                    ownerBox.Item().Row(row =>
                    {
                        row.AutoItem().Text("REGISTERED OWNER OF PROPERTY: ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Owner_Name)).FontSize(8).AlignCenter();
                    });

                    ownerBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeColumn(1).Row(r =>
                        {
                            r.AutoItem().Text("IDENTITY NO.: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Owner_Identity)).FontSize(8).AlignCenter();
                        });
                        row.RelativeColumn(1).PaddingLeft(20).Row(r =>
                        {
                            r.AutoItem().Text("COMPANY OR C.C REGISTRATION NO.: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Owner_Company)).FontSize(8).AlignCenter();
                        });
                    });

                    ownerBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.AutoItem().Text("PHYSICAL ADDRESS OF OWNER: ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(string.Join(" ", new[] {

                    s?.Owner_Address_2,
                    s?.Owner_Address_3,
                    s?.Owner_Address_4,

                }.Where(x => !string.IsNullOrWhiteSpace(x?.ToString()))) ?? "").FontSize(8);
                        row.AutoItem().PaddingLeft(5).Text("CODE:").FontSize(8);
                        row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Owner_Address_5)).FontSize(8).AlignCenter();
                    });

                    ownerBox.Item().PaddingTop(3).Row(row =>
                    {
                        row.AutoItem().Text("POSTAL ADDRESS OF OWNER: ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(string.Join(" ", new[] {

                    s?.Owner_Postal_2,
                    s?.Owner_Postal_3,
                    s?.Owner_Postal_4,

                }.Where(x => !string.IsNullOrWhiteSpace(x?.ToString()))) ?? "").FontSize(8).AlignCenter();
                        row.AutoItem().PaddingLeft(5).Text("CODE:").FontSize(8);
                        row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Owner_Postal_5)).FontSize(8).AlignCenter();
                    });

                    ownerBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeColumn(1).Row(r =>
                        {
                            r.AutoItem().Text("TELEPHONE NO.    HOME: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Owner_Home_Phone)).FontSize(8).AlignCenter();
                        });
                        row.RelativeColumn(1).PaddingLeft(10).Row(r =>
                        {
                            r.AutoItem().Text("WORK: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Owner_Work_Phone)).FontSize(8).AlignCenter();
                        });
                    });

                    ownerBox.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeColumn(1).Row(r =>
                        {
                            r.AutoItem().Text("                           CELL ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Owner_Cell_Phone)).FontSize(8).AlignCenter();
                        });
                        row.RelativeColumn(1).PaddingLeft(10).Row(r =>
                        {
                            r.AutoItem().Text("FAX ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Owner_Fax_Phone)).FontSize(8).AlignCenter();
                        });
                    });

                    ownerBox.Item().PaddingTop(3).Row(row =>
                    {
                        row.AutoItem().Text("E-MAIL ADDRESS  ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Owner_Email)).FontSize(8).AlignCenter();
                    });
                });

                // 1.2 OBJECTOR IS NOT THE OWNER section
                col.Item().PaddingTop(10).Border(2).Padding(10).Column(objectorBox =>
                {
                    objectorBox.Item().Text($"1.2 {_wording.PartyLabel} IS NOT THE OWNER OR MUNICIPALITY IS THE OBJECTOR").Bold().FontSize(8);

                    objectorBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.AutoItem().Text($"NAME OF {_wording.PartyLabel}:  ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Name)).FontSize(8).AlignCenter();
                    });

                    objectorBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeColumn(1).Row(r =>
                        {
                            r.AutoItem().Text("IDENTITY NO.: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Identity)).FontSize(8).AlignCenter();
                        });
                        row.RelativeColumn(1).PaddingLeft(20).Row(r =>
                        {
                            r.AutoItem().Text("COMPANY OR C.C REGISTRATION NO.: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Company)).FontSize(8).AlignCenter();
                        });
                    });

                    objectorBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.AutoItem().Text($"PHYSICAL ADDRESS OF {_wording.PartyLabel}:  ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(string.Join(" ", new[] {

                      s?.Objector_Postal_2,
                    s?.Objector_Postal_3,
                    s?.Objector_Postal_4,

                }.Where(x => !string.IsNullOrWhiteSpace(x?.ToString()))) ?? "").FontSize(8);
                        row.AutoItem().PaddingLeft(5).Text("CODE:").FontSize(8);
                        row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Postal_5)).FontSize(8).AlignCenter();
                    });

                    objectorBox.Item().PaddingTop(3).Row(row =>
                    {
                        row.AutoItem().Text($"POSTAL ADDRESS OF {_wording.PartyLabel}: ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(string.Join(" ", new[] {

                    s?.Objector_Postal_2,
                    s?.Objector_Postal_3,
                    s?.Objector_Postal_4,

                }.Where(x => !string.IsNullOrWhiteSpace(x?.ToString()))) ?? "").FontSize(8).AlignCenter();
                        row.AutoItem().PaddingLeft(5).Text("CODE:").FontSize(8);
                        row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Postal_5)).FontSize(8).AlignCenter();
                    });

                    objectorBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeColumn(1).Row(r =>
                        {
                            r.AutoItem().Text("TELEPHONE NO.    HOME: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Home)).FontSize(8).AlignCenter();
                        });
                        row.RelativeColumn(1).PaddingLeft(10).Row(r =>
                        {
                            r.AutoItem().Text("WORK: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Work)).FontSize(8).AlignCenter();
                        });
                    });

                    objectorBox.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeColumn(1).Row(r =>
                        {
                            r.AutoItem().Text("                           CELL: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Cell)).FontSize(8).AlignCenter();
                        });
                        row.RelativeColumn(1).PaddingLeft(10).Row(r =>
                        {
                            r.AutoItem().Text("FAX ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Fax)).FontSize(8);
                        });
                    });

                    objectorBox.Item().PaddingTop(3).Row(row =>
                    {
                        row.AutoItem().Text("E-MAIL ADDRESS:  ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Email)).FontSize(8).AlignCenter();
                    });

                    objectorBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.AutoItem().Text($"STATUS OF {_wording.PartyLabel} (eg. Tenant, Pending Purchaser, Municipality, etc) ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Status)).FontSize(8).AlignCenter();
                    });
                });

                // 1.3 AUTHORISED REPRESENTATIVE section
                col.Item().PaddingTop(10).Border(2).Padding(10).Column(repBox =>
                {
                    repBox.Item().Text($"1.3 AUTHORISED REPRESENTATIVE OF THE {_wording.PartyLabel}*").Bold().FontSize(8);

                    repBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.AutoItem().Text("NAME OF REPRESENTATIVE:  ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Representative_name)).FontSize(8).AlignCenter();
                    });

                    repBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.AutoItem().Text("POSTAL ADDRESS: ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(string.Join(" ", new[] {

                    s?.Rep_Postal_2,
                    s?.Rep_Postal_3,
                    s?.Rep_Postal_4,

                }.Where(x => !string.IsNullOrWhiteSpace(x?.ToString()))) ?? "").FontSize(8).AlignCenter();
                        row.AutoItem().PaddingLeft(5).Text("CODE:").FontSize(8);
                        row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Rep_Postal_5)).FontSize(8).AlignCenter();
                    });

                    repBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeColumn(1).Row(r =>
                        {
                            r.AutoItem().Text("TELEPHONE NO.    HOME: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Rep_Home_Phone)).FontSize(8).AlignCenter();
                        });
                        row.RelativeColumn(1).PaddingLeft(10).Row(r =>
                        {
                            r.AutoItem().Text("WORK: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Rep_Work_Phone)).FontSize(8);
                        });
                    });

                    repBox.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeColumn(1).Row(r =>
                        {
                            r.AutoItem().Text("                           CELL: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Rep_Cell_Phone)).FontSize(8).AlignCenter();
                        });
                        row.RelativeColumn(1).PaddingLeft(10).Row(r =>
                        {
                            r.AutoItem().Text("FAX: ").FontSize(8);
                            r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Rep_Fax_Phone)).FontSize(8).AlignCenter();
                        });
                    });

                    repBox.Item().PaddingTop(3).Row(row =>
                    {
                        row.AutoItem().Text("E-MAIL ADDRESS:  ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Rep_Email)).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(5).Border(2).Padding(5)
      .Text("* IF A RESPRESENTATIVE IS APPOINTED, PROOF OF AUTHORISATION MUST BE ATTACHED, TO THIS FORM")
      .Bold()
      .FontSize(8)
      .FontColor(Colors.Red.Darken2)
      .AlignCenter();


            });
        }     // Section 2: property details
        private void BuildSection2(IContainer container, dynamic s)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("SECTION 2: PROPERTY DETAILS (FOR SECTIONAL TITLES SEE SECTION 4)").Bold().FontSize(8);

                // Physical Address and Code
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("PHYSICAL ADDRESS: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.physical_address)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(5).Text("CODE:").FontSize(8);
                    row.ConstantColumn(80).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Code)).FontSize(8).AlignCenter();
                });

                // Extent of Property
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("EXTENT OF PROPERTY (m²): ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Extent)).FontSize(8).AlignCenter();
                });

                // Municipal Account No
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("MUNCIPAL ACCOUNT NO: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Municipal_Account_No)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("(if available)").FontSize(7).Italic();
                });

                // Bondholder details
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("NAME OF BONDHOLDER: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.BondHolder_Name)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("REGISTERED AMOUNT OF BOND: ").FontSize(8);
                    row.ConstantColumn(120).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Registered_Amount)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(5).Text("(if applicable)").FontSize(7).Italic();
                });

                // Servitude details header
                col.Item().PaddingTop(8).Text("PROVIDE FULL DETAILS OF ALL SERVITUDES, ROAD PROCLAMATIONS OR OTHER ENDORSEMENTS AGAINST THE PROPERTY (if applicable)").FontSize(8);
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.full_Details)).FontSize(8).AlignCenter();
                });

                // Servitude No and Affected Area
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("SERVITUDE NO.: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Servitude_No)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("AFFECTED AREA (m²): ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Affected_Area)).FontSize(8).AlignCenter();
                });

                // In Favour Of
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("IN FAVOUR OF: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Property_Favour_Of)).FontSize(8).AlignCenter();
                });

                // For What Purpose
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("FOR WHAT PURPOSE: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Property_Purpose)).FontSize(8).AlignCenter();
                });

                // Compensation details
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("WAS COMPENSATION PAID? YES: ").FontSize(8);

                    // YES box
                    row.ConstantColumn(40).Border(1).Padding(2).AlignCenter()
                        .Text(string.Equals(Str(s?.Compensation_Paid), "YES", StringComparison.OrdinalIgnoreCase) ? "X" : "")
                        .FontSize(8)
                        .AlignCenter();

                    // NO label
                    row.AutoItem().PaddingLeft(5).Text("NO: ").FontSize(8);

                    // NO box
                    row.ConstantColumn(40).Border(1).Padding(2).AlignCenter()
                        .Text(string.Equals(Str(s?.Compensation_Paid), "NO", StringComparison.OrdinalIgnoreCase) ? "X" : "")
                        .FontSize(8)
                        .AlignCenter();


                    row.AutoItem().PaddingLeft(10).Text("IF YES DATE OF PAYMENT: ").FontSize(8);
                    row.ConstantColumn(100).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Payment_Date)).FontSize(8);
                    row.AutoItem().PaddingLeft(5).Text("AMOUNT R: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Compensation_Amount)).FontSize(8);
                });
            });
        }

        private void BuildSection3(IContainer container, dynamic s)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("SECTION 3: DESCRIPTION OF RESIDENTIAL DWELLING ( FOR SECTIONAL TITLES SEE SECTION 4 ) ( INDICATE NUMBER OR STATE YES / NO )").Bold().FontSize(8);

                // 3.1 MAIN DWELLING header
                col.Item().PaddingTop(5).Text("3.1 MAIN DWELLING").Bold().FontSize(8);

                // First row of main dwelling details - 5 columns
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("NO OF BEDROOMS:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_No_of_Bedroom)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("NO OF BATHROOMS:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_No_of_BathRoom)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("KITCHEN:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Kitchen)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("LOUNGE:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Lounge)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("DINING ROOM:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Dinning_Room)).FontSize(7).AlignCenter();
                    });
                });

                // Second row - split into 2 rows for better fitting
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("LOUNGE/DINING:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Lounge_Dining_Room)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("STUDY:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Study)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("PLAYROOM:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Play_Room)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("TV ROOM:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Television)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("LAUNDRY:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Laundry)).FontSize(7).AlignCenter();
                    });
                });

                // Third row
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("SEPARATE TOILET:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Seperate_Toilet)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("OTHER: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Dwell_Other1)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("OTHER: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Dwell_Other2)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("OTHER: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Dwell_Other3)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("OTHER: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Dwell_Other4)).FontSize(7).AlignCenter();
                    });
                });

                // OUTBUILDINGS header
                col.Item().PaddingTop(8).Text("OUTBUILDINGS").Bold().FontSize(8);

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("NO. OF GARAGES:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_No_of_Garages)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(5).Row(r =>
                    {
                        r.AutoItem().Text("GRANNY FLAT/ROOMS:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Granny_Room)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn(2).PaddingLeft(5).Row(r =>
                    {
                        r.AutoItem().Text("OTHER:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Outbuild_Other)).FontSize(7).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("MAIN SIZE (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Main_Dwelling_Size)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(5).Row(r =>
                    {
                        r.AutoItem().Text("OUTBUILD SIZE (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Outside_Building_Size)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(5).Row(r =>
                    {
                        r.AutoItem().Text("OTHER SIZE (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Other_Building_Size)).FontSize(7).AlignCenter();
                    });
                });

                // OTHER (ATTACH ANNEXURE) header
                col.Item().PaddingTop(8).Text("OTHER (ATTACH ANNEXURE)").Bold().FontSize(8);

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("POOL:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Swimming_Pool)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("BORE HOLE:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Bore_Hole)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("TENNIS:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Tennis_Court)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("GARDEN:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Garden)).FontSize(7).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("OTHER:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_other_dwell1)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("OTHER:").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_other_dwell2)).FontSize(7).AlignCenter();
                    });
                });

                // FENCING
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("FENCING: FRONT:").FontSize(7);
                    row.ConstantColumn(60).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Fence_Front)).FontSize(7).AlignCenter();
                    row.AutoItem().PaddingLeft(3).Text("BACK: ").FontSize(7);
                    row.ConstantColumn(60).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Fence_Back)).FontSize(7).AlignCenter();
                    row.AutoItem().PaddingLeft(3).Text("SIDE1: ").FontSize(7);
                    row.ConstantColumn(60).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Fence_Side_1)).FontSize(7).AlignCenter();
                    row.AutoItem().PaddingLeft(3).Text("SIDE2:").FontSize(7);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Fence_Side_2)).FontSize(7).AlignCenter();
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text("TYPE: ").FontSize(7);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Fence)).FontSize(7);
                    row.AutoItem().PaddingLeft(5).Text("HEIGHT:").FontSize(7);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(string.Join(" / ", new[] {
                s?.Res_Fence_Height_Front?.ToString(),
                s?.Res_Fence_Height_Back?.ToString(),
                s?.Res_Fence_Height_Side1?.ToString(),
                s?.Res_Fence_Height_Side2?.ToString()
            }.Where(x => !string.IsNullOrWhiteSpace(x)) ?? new string[0])).FontSize(7);
                });

                // DRIVEWAY
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("DRIVEWAY: ").FontSize(7);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Drive_Way)).FontSize(7).AlignCenter();
                    row.AutoItem().PaddingLeft(5).Text("BOOMED/SECURITY YES").FontSize(7);

                    // YES box
                    row.ConstantColumn(30).Border(1).Padding(2).AlignCenter()
                        .Text(string.Equals(Str(s?.Res_Security_Boomed_Area), "YES", StringComparison.OrdinalIgnoreCase) ? "X" : "")
                        .FontSize(7)
                        .AlignCenter();

                    // NO label
                    row.AutoItem().PaddingLeft(3).Text("NO").FontSize(7);

                    // NO box
                    row.ConstantColumn(30).Border(1).Padding(2).AlignCenter()
                        .Text(string.Equals(Str(s?.Res_Security_Boomed_Area), "NO", StringComparison.OrdinalIgnoreCase) ? "X" : "")
                        .FontSize(7)
                        .AlignCenter();

                });

                // OTHER FEATURES
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("OTHER FEATURES: ").FontSize(7);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res_Other_Features)).FontSize(7);
                });

                // GENERAL CONDITION
                col.Item().PaddingTop(5).Row(row =>
                {
                    // GOOD label
                    row.AutoItem().Text("CONDITION: GOOD: ").FontSize(7);

                    // GOOD box
                    row.ConstantColumn(50).Border(1).Padding(2).AlignCenter()
                        .Text(string.Equals(Str(s?.Res_General_Condition), "GOOD", StringComparison.OrdinalIgnoreCase) ? "X" : "")
                        .FontSize(7)
                        .AlignCenter();

                    // AVERAGE label
                    row.AutoItem().PaddingLeft(3).Text("AVERAGE: ").FontSize(7);

                    // AVERAGE box
                    row.ConstantColumn(50).Border(1).Padding(2).AlignCenter()
                        .Text(string.Equals(Str(s?.Res_General_Condition), "AVERAGE", StringComparison.OrdinalIgnoreCase) ? "X" : "")
                        .FontSize(7)
                        .AlignCenter();

                    // POOR label
                    row.AutoItem().PaddingLeft(3).Text("POOR: ").FontSize(7);

                    // POOR box
                    row.ConstantColumn(50).Border(1).Padding(2).AlignCenter()
                        .Text(string.Equals(Str(s?.Res_General_Condition), "POOR", StringComparison.OrdinalIgnoreCase) ? "X" : "")
                        .FontSize(7)
                        .AlignCenter();
                });

            });
        }


        // Section 4: sectional title units
        private void BuildSection4(IContainer container, dynamic s)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("SECTION 4: SECTIONAL TITLE UNITS").Bold().FontSize(8);

                // Scheme details
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("SCHEME NO.: ").FontSize(8).AlignCenter();
                    row.ConstantColumn(100).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Scheme_No)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("NAME OF SCHEME: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Scheme_Name)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("FLAT NO./DOOR NO.: ").FontSize(8);
                    row.ConstantColumn(80).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Flat_No)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("UNIT SIZE (m²): ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Unit_Size)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("NAME OF MANAGING AGENT: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Managing_Agent_Name)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("TEL NO.: ").FontSize(8);
                    row.ConstantColumn(150).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Managing_Agent_Tel_No)).FontSize(8).AlignCenter();
                });

                // INDICATE NUMBER OR STATE YES/NO
                col.Item().PaddingTop(8).Text("INDICATE NUMBER OR STATE YES/NO").Bold().FontSize(8);

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("BEDROOMS: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_No_of_Bedroom)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("BATHROOMS: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_No_of_BathRoom)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("KITCHEN: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Kitchen)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("LOUNGE: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Lounge)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("DINING: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Dinning_Room)).FontSize(7).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("LOUNGE/DINING: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Lounge_Dining_Room)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("STUDY: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Study)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("PLAYROOM: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Play_Room)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("TV: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Television)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("LAUNDRY: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Laundry)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("SEPERATED TOILET: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Seperate_Toilet)).FontSize(7).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("OTHER: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Dwell_Other1)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("OTHER: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Dwell_Other2)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("OTHER: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Dwell_Other3)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(3).Row(r =>
                    {
                        r.AutoItem().Text("OTHER: ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Dwell_Other4)).FontSize(7).AlignCenter();
                    });
                });

                // COMMON PROPERTY CONSISTS OF: and DETAILS OF EXCLUSIVE AREAS (side by side)
                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeColumn().Text("COMMON PROPERTY CONSISTS OF:").Bold().FontSize(8);
                    row.RelativeColumn().Text("DETAILS OF EXCLUSIVE AREAS").Bold().FontSize(8);
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("POOL (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Pool_Size)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(10).Row(r =>
                    {
                        r.AutoItem().Text("GARAGE (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Garage_Size)).FontSize(7).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("TENNIS (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Tennis_Court_Size)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(10).Row(r =>
                    {
                        r.AutoItem().Text("CARPORT (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Carport_Size)).FontSize(7).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("OTHER (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Common_Property_Other_1)).FontSize(7).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(10).Row(r =>
                    {
                        r.AutoItem().Text("PARKING (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Open_Parking_Size)).FontSize(7).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("OTHER (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Common_Property_Other_2)).FontSize(7);
                    });
                    row.RelativeColumn().PaddingLeft(10).Row(r =>
                    {
                        r.AutoItem().Text("STORE (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Store_Room_Size)).FontSize(7).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("OTHER (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Common_Property_Other_3)).FontSize(7);
                    });
                    row.RelativeColumn().PaddingLeft(10).Row(r =>
                    {
                        r.AutoItem().Text("GARDEN (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Res4_Garden_Size)).FontSize(7).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("OTHER (m²): ").FontSize(7);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text("").FontSize(7).AlignCenter();
                    });
                });
            });
        }

        // Section 5: market information
        private void BuildSection5(IContainer container, dynamic s, string erfUnitNo, string areaScheme)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("SECTION 5: MARKET INFORMATION").Bold().FontSize(8);

                col.Item().PaddingTop(5).Text("IF YOUR PROPERTY IS CURRENTLY ON THE MARKET WHAT IS THE ASKING PRICE?").FontSize(8);
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text(":R ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)StrD(s?.Current_Asking_price)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(20).Text("OFFER RECEIVED :R ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)(StrD(s?.Current_Recieved_Offer))).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(8).Text("IF YOUR PROPERTY HAS BEEN ON THE MARKET IN THE LAST 3 YEARS WHAT WAS THE ASKING PRICE?").FontSize(8);
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text(":R ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)(StrD(s?.Previous_Asking_price))).FontSize(8).AlignCenter();

                    row.AutoItem().PaddingLeft(20).Text("OFFER RECEIVED :R ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)(StrD(s?.Previous_Recieved_Offer))).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("NAME OF AGENT: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Agent_Name)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("TEL NO.: ").FontSize(8);
                    row.ConstantColumn(150).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Agent_Tel_No)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(8).Text("SALES TRANSACTIONS (OF OTHER PROPERTIES IN THE VICINITY) USED BY OBJECTOR IN DETERMINING THE MARKET VALUE OF THE PROPERTY OBJECTED TO:").FontSize(8);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("ERF/UNIT NO.: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)((erfUnitNo ?? Str(s?.Unit_No)))).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("SUBURB/FARM/SCHEME NAME: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)((areaScheme ?? Str(s?.Suburb_Name)))).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("DATE OF SALE: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Sale_Date)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("SELLING PRICE: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)(StrD(s?.Selling_Price))).FontSize(8).AlignCenter();
                });
            });
        }

        // Section 6: valuation / objection details
        private void BuildSection6(IContainer container, dynamic s)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text($"SECTION 6: {_wording.InquiryLabel} DETAILS").Bold().FontSize(8);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeColumn().Text($"PARTICULARS AS REFLECTED IN {_wording.secHeader}").Bold().FontSize(8).AlignCenter();
                    row.RelativeColumn().Text($"CHANGES REQUESTED BY {_wording.PartyLabel}").Bold().FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("DESCRIPTION OF THE PROPERTY/UNIT NO.: ").FontSize(8);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Old_Property_Description)).FontSize(8).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(10).Row(r =>
                    {
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.New_Property_Description)).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("CATEGORY: ").FontSize(8);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Old_Category)).FontSize(8).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(10).Row(r =>
                    {
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.New_Category)).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("PHYSICAL ADDRESS/DOOR NO./FLAT NO.: ").FontSize(8);
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Old_Address)).FontSize(8).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(10).Row(r =>
                    {
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.New_Address)).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("EXTENT: ").FontSize(8);
                        //r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(StrD(s?.Old_Extent)).FontSize(8).AlignCenter();
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Old_Extent)).FontSize(8).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(10).Row(r =>
                    {
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)(StrD(s?.New_Extent))).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().Row(r =>
                    {
                        r.AutoItem().Text("MARKET VALUE: ").FontSize(8);
                        //r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(StrD(s?.Old_Market_Value)).FontSize(8).AlignCenter();
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Rand(s?.Old_Market_Value)).FontSize(8).AlignCenter();
                    });
                    row.RelativeColumn().PaddingLeft(10).Row(r =>
                    {
                        //r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(StrD(s?.New_Market_Value)).FontSize(8).AlignCenter();
                        r.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Rand(s?.New_Market_Value)).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("With Effect Date: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text("").FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text("NAME OF OWNER: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Old_Owner)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(8).Text($"ADVERSE FEATURES AND/OR FURTHER REASONS IN SUPPORT OF THIS {_wording.InquiryLabel} (ANNEXURE CAN BE PROVIDED):").FontSize(8);
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objection_Reasons)).FontSize(8);
                });
            });
        }
        // Example for Section 7 with rounded corners
        private void BuildSection7(IContainer container, dynamic s)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                // Section Title
                col.Item().Text("SECTION 7: DECLARATION").Bold().FontSize(8);

                // Section Text
                col.Item().PaddingTop(5)
                    .Text("ATTENTION IS HEREBY DRAWN TO SECTION 42(2) OF THE ACT WHICH STATES THAT WHERE ANY DOCUMENT, " +
                    "INFORMATION OR PARTICULARS WERE NOT PROVIDED WHEN REQUIRED IN TERMS OF SUBSECTION 42(1) OF THE ACT AND THE OWNER CONCERNED RELIES ON SUCH DOCUMENT, " +
                    "INFORMATION OR PARTICULARS IN AN APPEAL TO AN APPEAL BOARD, " +
                    "THE APPEAL BOARD MAY MAKE AN ORDER AS TO COSTS IN TERMS OF SECTION 70 OF THE ACT IF THE APPEAL BOARD IS OF THE VIEW THAT THE FAILURE TO SO HAVE PROVIDED ANY SUCH DOCUMENT," +
                    " INFORMATION OR PARTICULARS HAS PLACED AN UNNECESSARY BURDEN ON THE FUNCTIONS OF THE MUNICIPAL VALUER OR THE APPEAL BOARD.")
                    .FontSize(7)
                    .Bold();

                // Declarer Name Row
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("I / WE ").FontSize(8);
                    row.RelativeColumn()
                        .BorderBottom(1)
                        .PaddingBottom(2)
                        .Text((string)Str(s?.Signature_Name))
                        .FontSize(8)
                        .AlignCenter();
                    row.AutoItem().PaddingLeft(5)
                        .Text(" HEREBY DECLARE THAT THE INFORMATION AND PARTICULARS SUPPLIED ARE TRUE AND CORRECT")
                        .FontSize(7)
                        .Bold();
                });

                // ===================== DECLARATION DATE =====================
                string year = "";
                string month = "";
                string day = "";
                if (s?.Declaration_Date != null)
                {
                    if (DateTime.TryParse((string)s.Declaration_Date, out DateTime parsedDate))
                    {
                        year = parsedDate.Year.ToString();
                        month = parsedDate.Month.ToString("00");
                        day = parsedDate.Day.ToString("00");
                    }
                }

                // Date + Signature Row - All on one line
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text("DATE YEAR").FontSize(8);
                    row.AutoItem().BorderBottom(1).PaddingBottom(2).Width(60).Text(year).FontSize(8).AlignCenter();

                    row.AutoItem().PaddingLeft(5).Text("MONTH").FontSize(8);
                    row.AutoItem().BorderBottom(1).PaddingBottom(2).Width(60).Text(month).FontSize(8).AlignCenter();

                    row.AutoItem().PaddingLeft(5).Text("DAY").FontSize(8);
                    row.AutoItem().BorderBottom(1).PaddingBottom(2).Width(60).Text(day).FontSize(8).AlignCenter();

                    row.AutoItem().PaddingLeft(10).Text("SIGNATURE").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Element(signatureContainer =>
                    {
                        if (!string.IsNullOrEmpty((string?)s?.Signature_Picture))
                        {
                            try
                            {
                                var base64 = ((string)s.Signature_Picture).Split(',').Last();
                                byte[] bytes = Convert.FromBase64String(base64);
                                signatureContainer.Height(20).Image(bytes, ImageScaling.FitArea);
                            }
                            catch
                            {
                                signatureContainer.Text("");
                            }
                        }
                    });
                });
            });
        }
        // Section 8: Decision of Municipal Valuer
        private void BuildSection8(IContainer container, dynamic s)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("SECTION 8: DECISION OF MUNICIPAL VALUER").Bold().FontSize(8);

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem(2).Row(r =>
                    {
                        r.AutoItem().Text("DESCRIPTION OF THE PROPERTY/UNIT NO.: ").FontSize(8);
                        r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Description)).FontSize(8).AlignCenter();
                    });

                    row.RelativeItem(1).PaddingLeft(10).Row(r =>
                    {
                        r.AutoItem().Text("CATEGORY: ").FontSize(8);
                        r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Category)).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem(2).Row(r =>
                    {
                        r.AutoItem().Text("PHYSICAL ADDRESS/DOOR NO/FLAT NO.: ").FontSize(8);
                        r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.PhysicalAddress)).FontSize(8).AlignCenter();
                    });

                    row.RelativeItem(1).PaddingLeft(10).Row(r =>
                    {
                        r.AutoItem().Text("EXTENT: ").FontSize(8);
                        r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Extent)).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem(1).Row(r =>
                    {
                        r.AutoItem().Text("MARKET VALUE: ").FontSize(8);
                        r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text((string)Rand(s?.MarketValue)).FontSize(8).AlignCenter();
                    });

                    row.RelativeItem(2).PaddingLeft(10).Row(r =>
                    {
                        r.AutoItem().Text("NAME OF OWNER: ").FontSize(8);
                        r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.OwnerName)).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.AutoItem().Text("With Effect Date: YEAR ").FontSize(8);
                    row.ConstantItem(60).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Year)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(5).Text("MONTH ").FontSize(8);
                    row.ConstantItem(60).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Month)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(5).Text("DAY ").FontSize(8);
                    row.ConstantItem(60).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Day)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.AutoItem().Text("REASONS ").FontSize(8);
                    row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Reasons)).FontSize(8);
                });

                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                });

                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                });

                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem(2).Row(r =>
                    {
                        r.AutoItem().Text("NAME OF MUNICIPAL VALUER/ASSISTANT MUNICIPAL VALUER*: ").FontSize(8);
                        r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.ValuerName)).FontSize(8).AlignCenter();
                    });

                    row.AutoItem().PaddingLeft(10).Text("DATE: YEAR ").FontSize(8);
                    row.ConstantItem(40).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(3).Text("MONTH ").FontSize(8);
                    row.ConstantItem(40).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(3).Text("DAY ").FontSize(8);
                    row.ConstantItem(40).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(4).Text("* Delete whichever is not applicable").FontSize(7);

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.AutoItem().Text("SIGNATURE ").FontSize(8);
                    row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("").FontSize(8).AlignCenter();
                });
            });
        }

        // Section 9: Notification of Outcome
        private void BuildSection9(IContainer container, dynamic s)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("SECTION 9: NOTIFICATION OF OUTCOME").Bold().FontSize(8);

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.ConstantItem(200).Text("").FontSize(8);
                    row.RelativeItem(1).AlignCenter().Text("SIGNATURE").Bold().FontSize(8);
                    row.RelativeItem(1).AlignCenter().Text("PRINT NAME").Bold().FontSize(8);
                    row.ConstantItem(80).AlignCenter().Text("DATE").Bold().FontSize(8);
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.ConstantItem(200).Text("VALUATION ROLL ADJUSTED").FontSize(8);
                    row.RelativeItem(1).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                    row.RelativeItem(1).PaddingLeft(5).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                    row.ConstantItem(80).PaddingLeft(5).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.ConstantItem(200).Text($"{_wording.PartyLabel} NOTIFIED").FontSize(8);
                    row.RelativeItem(1).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                    row.RelativeItem(1).PaddingLeft(5).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                    row.ConstantItem(80).PaddingLeft(5).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.ConstantItem(200).Text("OWNER NOTIFIED").FontSize(8);
                    row.RelativeItem(1).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                    row.RelativeItem(1).PaddingLeft(5).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                    row.ConstantItem(80).PaddingLeft(5).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                });

                col.Item().PaddingTop(15).Text("OFFICIAL USE").Bold().FontSize(9);

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.AutoItem().Text($"{_wording.NumberLabel}").FontSize(8);
                    row.ConstantItem(150).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                    row.AutoItem().PaddingLeft(10).Text($"SIGNATURE OF PERSON WHO RECEIVED THE {_wording.InquiryLabel} ").FontSize(8);
                    row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.AutoItem().Text("NAME OF OWNER ").FontSize(8);
                    row.RelativeItem(2).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                    row.AutoItem().PaddingLeft(10).Text("DATE ").FontSize(8);
                    row.RelativeItem(1).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.AutoItem().Text($"NAME OF {_wording.PartyLabel} IF NOT THE SAME ").FontSize(8);
                    row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.AutoItem().Text("ERF NUMBER ").FontSize(8);
                    row.ConstantItem(150).BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                    row.AutoItem().PaddingLeft(10).Text("TOWNSHIP NAME ").FontSize(8);
                    row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("").FontSize(8);
                });
            });
        }



        private string GetReference()
        {
            try
            {
                dynamic m = _data.Main;
                return m?.Objection_No ?? m?.Appeal_No ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        private string GetPropertyDescription()
        {
            try
            {
                dynamic m = _data.Main;
                return m?.Property_Desc ?? m?.A_Property_Desc ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        private string GetStatus()
        {
            try
            {
                dynamic m = _data.Main;
                return m?.objection_Status ?? m?.Appeal_Status ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string StrD(dynamic value)
        {
            if (value == null) return "";
            string raw = value.ToString();
            return decimal.TryParse(raw, out decimal d) ? d.ToString("N2") : raw;
        }
      
        private static string Str(dynamic value) => (value ?? string.Empty).ToString();
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