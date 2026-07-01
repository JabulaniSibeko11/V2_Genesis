
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using V2_Genesis.Models;

namespace GV_Forms.Pdf
{
    public class FormCDocument : IDocument
    {
        private readonly InquiryAggregate _data;
        private readonly Wording _wording;
        private readonly IWebHostEnvironment _env;
        public FormCDocument(InquiryAggregate data, Wording wording,IWebHostEnvironment env)
        {
            _data = data;
            _wording = wording;
            _env = env;
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


            var s4 = _data.GetSection<dynamic>("Section4");//BuildSection4 uses Section5 data

            var s5 = _data.GetSection<dynamic>("Section5");//BuildSection5 uses Section6 data

            var s7 = _data.GetSection<dynamic>("Section6");//BuildSection6 uses Section7 data

            var s8 = _data.GetSection<dynamic>("Section8");
            var s9 = _data.GetSection<dynamic>("Section9");

            // Extract objection number, ERF/Unit/Portion No, Farm No, REG.DIV, and Area/Scheme Name
            string objectionNo = string.Empty;
            string erfUnitNo = string.Empty;
            string areaScheme = string.Empty;
            string farmNo = string.Empty;
            string regDiv = string.Empty;
            int startYear = 2023; // Default fallback
            int endYear = 2027; // Default fallback
            try
            {
                dynamic m = _data.Main;
                objectionNo = m?.Objection_No ?? m?.Appeal_No ?? string.Empty;

                // Add this line to get valuation year from your data
                int valuationYear = DateTime.Now.Year;
                // Calculate 5-year period
                endYear = valuationYear;
                startYear = valuationYear - 5;

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
                    // For full title, extract PORTION, ERF, or FARM number from Property_Desc
                    if (!string.IsNullOrEmpty(areaScheme))
                    {
                        // Look for "PORTION" followed by a number (e.g., "PORTION 483 DIEPSLOOT 388-JR")
                        var portionMatch = System.Text.RegularExpressions.Regex.Match(areaScheme, @"PORTION\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (portionMatch.Success && portionMatch.Groups.Count > 1)
                        {
                            erfUnitNo = portionMatch.Groups[1].Value;
                        }
                        else
                        {
                            // Look for "ERF" followed by a number
                            var erfMatch = System.Text.RegularExpressions.Regex.Match(areaScheme, @"ERF\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (erfMatch.Success && erfMatch.Groups.Count > 1)
                            {
                                erfUnitNo = erfMatch.Groups[1].Value;
                            }
                        }

                        // Extract FARM name/number (e.g., "DIEPSLOOT 388-JR" from "PORTION 483 DIEPSLOOT 388-JR")
                        var farmMatch = System.Text.RegularExpressions.Regex.Match(areaScheme, @"(?:PORTION\s+\d+\s+)?([A-Z\s]+\d+[-]?[A-Z]*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (farmMatch.Success && farmMatch.Groups.Count > 1)
                        {
                            farmNo = farmMatch.Groups[1].Value.Trim();
                        }

                        // Extract REG.DIV (Registration Division) - usually the last part after hyphen (e.g., "JR" from "388-JR")
                        var regDivMatch = System.Text.RegularExpressions.Regex.Match(areaScheme, @"[-]([A-Z]{2})$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (regDivMatch.Success && regDivMatch.Groups.Count > 1)
                        {
                            regDiv = regDivMatch.Groups[1].Value.ToUpper();
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
                    // Top Header Row - Complete Portion/Holding and Farm/Holding Name
                    header.Item().Row(topRow =>
                    {
                        topRow.RelativeItem().Row(leftRow =>
                        {
                            leftRow.AutoItem().Text("Complete: Portion/Holding:  ").FontSize(8);
                            leftRow.AutoItem().BorderBottom(1).PaddingBottom(2).Width(80).Text(erfUnitNo).FontSize(8).AlignCenter();
                            leftRow.AutoItem().PaddingLeft(10).Text("Farm/Holding Name:  ").FontSize(8);
                            leftRow.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(farmNo).FontSize(8).AlignCenter();
                        });
                        topRow.AutoItem().Text($"Form C {_wording.InquiryLabel}").BackgroundColor(GetFormHeaderColor()).Bold().FontSize(9);
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
                    header.Item()
    .PaddingTop(5)
    .Element(c =>
    {
        BuildFullBannerHeader(c, banner1Bytes, banner2Bytes);
    });

                    header.Item().PaddingTop(10).Text("FORM C: AGRICULTURAL HOLDINGS OR FARMS")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(5).Text("THE MUNICIPAL MANAGER")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem(1).Text("City of Johannesburg").FontSize(9).Bold();
                        row.RelativeItem(2).Row(objRow =>
                        {
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
                            dateRow.AutoItem().BorderBottom(1).Width(100).Text(startYear.ToString()).FontSize(8).AlignCenter().Bold();
                            dateRow.AutoItem().PaddingLeft(10).Text("TO 30 JUNE ").Bold().FontSize(8);
                            dateRow.AutoItem().BorderBottom(1).Width(100).Text(endYear.ToString()).FontSize(8).AlignCenter().Bold();
                        });

                        boxCol.Item().PaddingTop(8).Text("DESCRIPTION OF PROPERTY IN RESPECT OF WHICH THE OBJECTION IS MADE (Complete a separate form for each entry objected to)")
                            .Bold().FontSize(8);

                        boxCol.Item().PaddingTop(5).Row(erfRow =>
                        {
                            erfRow.AutoItem().Text("ERF/PORTION/ UNIT NO.: ").FontSize(8);
                            erfRow.AutoItem().BorderBottom(1).Width(150).Text(erfUnitNo).FontSize(8).AlignCenter();
                            erfRow.AutoItem().PaddingLeft(15).Text("SUBURB/SCHEME NAME: ").FontSize(8);
                            erfRow.RelativeItem().BorderBottom(1).Text(areaScheme).FontSize(8).AlignCenter();
                        });

                        boxCol.Item().PaddingTop(5).Row(farmRow =>
                        {
                            farmRow.AutoItem().Text("FARM NO: ").FontSize(8);
                            farmRow.RelativeItem().BorderBottom(1).Text(farmNo).FontSize(8).AlignCenter();
                            farmRow.AutoItem().PaddingLeft(15).Text("REG.DIV: ").FontSize(8);
                            farmRow.RelativeItem().BorderBottom(1).Text(regDiv).FontSize(8).AlignCenter();
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
                    // Top Header Row - Complete Portion/Holding and Farm/Holding Name
                    header.Item().Row(topRow =>
                    {
                        topRow.RelativeItem().Row(leftRow =>
                        {
                            leftRow.AutoItem().Text("Complete: Portion/Holding:  ").FontSize(8);
                            leftRow.AutoItem().BorderBottom(1).PaddingBottom(2).Width(80).Text(erfUnitNo).FontSize(8).AlignCenter();
                            leftRow.AutoItem().PaddingLeft(10).Text("Farm/Holding Name:  ").FontSize(8);
                            leftRow.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(farmNo).FontSize(8).AlignCenter();
                        });
                        topRow.AutoItem().Text($"Form C {_wording.InquiryLabel}").BackgroundColor(GetFormHeaderColor()).Bold().FontSize(9);
                    });

                    // Page Number
                    header.Item().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(8).Italic();
                        text.CurrentPageNumber().FontSize(8).Italic();
                        text.Span(" of ").FontSize(8).Italic();
                        text.TotalPages().FontSize(8).Italic();
                    });

                    header.Item().PaddingTop(5).Text("FORM C: AGRICULTURAL HOLDINGS OR FARMS")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(5).Text("THE MUNICIPAL MANAGER")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem(1).Text("City of Johannesburg").FontSize(9).Bold();
                        row.RelativeItem(2).Row(objRow =>
                        {
                            objRow.AutoItem().Text($"{_wording.NumberLabel}: ").FontSize(9).Bold();
                            objRow.RelativeItem().BorderBottom(1).Text(objectionNo).FontSize(9).AlignCenter();
                        });
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Element(c => { BuildSection2(c, s2); return c; });
                    col.Item().PaddingTop(10).PaddingBottom(35).Element(c => { BuildSection3(c, s3); return c; });
                    col.Item().PaddingTop(10).Element(c => { BuildSection35(c, s3); return c; });

                    col.Item().PaddingTop(10).PaddingBottom(210).Element(c => { BuildSection4(c, s4, erfUnitNo, areaScheme); return c; });

                    col.Item().PaddingTop(10).PaddingBottom(0).Element(c => { BuildSection5(c, s5); });
                    col.Item().PaddingTop(10).Element(c => { BuildSection6(c, s7); });
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
                    // Top Header Row - Complete Portion/Holding and Farm/Holding Name
                    header.Item().Row(topRow =>
                    {
                        topRow.RelativeItem().Row(leftRow =>
                        {
                            leftRow.AutoItem().Text("Complete: Portion/Holding:  ").FontSize(8);
                            leftRow.AutoItem().BorderBottom(1).PaddingBottom(2).Width(80).Text(erfUnitNo).FontSize(8).AlignCenter();
                            leftRow.AutoItem().PaddingLeft(10).Text("Farm/Holding Name:  ").FontSize(8);
                            leftRow.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(farmNo).FontSize(8).AlignCenter();
                        });
                        topRow.AutoItem().Text($"Form C {_wording.InquiryLabel}").BackgroundColor(GetFormHeaderColor()).Bold().FontSize(9);
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

                    header.Item().PaddingTop(10).Text("FORM C: AGRICULTURAL HOLDINGS OR FARMS")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(5).Text("THE MUNICIPAL MANAGER")
                        .Bold().FontSize(9).AlignLeft();

                    header.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem(1).Text("City of Johannesburg").FontSize(9).Bold();
                        row.RelativeItem(2).Row(objRow =>
                        {
                            objRow.AutoItem().Text($"{_wording.NumberLabel}: ").FontSize(9).Bold();
                            objRow.RelativeItem().BorderBottom(1).Text(objectionNo).FontSize(9).AlignCenter();
                        });
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    // OFFICIAL USE - Red Text with Line
                    col.Item().PaddingTop(3).Text("OFFICIAL USE")
                        .FontColor("#FF0000")
                        .Bold()
                        .FontSize(9);
                    col.Item().PaddingTop(3).LineHorizontal(1);
                    col.Item().Element(c => { BuildSection7(c, s7); return c; });
                    col.Item().PaddingTop(10).Element(c => { BuildSection8(c, s8); return c; });
                    col.Item().PaddingTop(10).Element(c => { BuildSection9(c, s9); return c; });
                });
            });
        }

        private void BuildSection1(IContainer container, dynamic s)
        {
            container.Column(col =>
            {
                // 1.1 OBJECTOR IS THE OWNER section
                col.Item().Border(2).Padding(10).Column(ownerBox =>
                {
                    ownerBox.Item().Text("SECTION 1: OBJECTOR INFORMATION").Bold().FontSize(9);
                    ownerBox.Item().PaddingTop(5).PaddingBottom(3).Text("1.1   OBJECTOR IS THE OWNER").Bold().FontSize(8);
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

                }.Where(x => !string.IsNullOrWhiteSpace(x?.ToString()))) ?? "").FontSize(8);
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
                    objectorBox.Item().Text("1.2 OBJECTOR IS NOT THE OWNER OR MUNICIPALITY IS THE OBJECTOR").Bold().FontSize(8);

                    objectorBox.Item().PaddingTop(5).Row(row =>
                    {
                        row.AutoItem().Text("NAME OF OBJECTOR:  ").FontSize(8);
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
                        row.AutoItem().Text("PHYSICAL ADDRESS OF OBJECTOR:  ").FontSize(8);
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
                        row.AutoItem().Text("POSTAL ADDRESS OF OBJECTOR: ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text(string.Join(" ", new[] {

                    s?.Objector_Postal_2,
                    s?.Objector_Postal_3,
                    s?.Objector_Postal_4,

                }.Where(x => !string.IsNullOrWhiteSpace(x?.ToString()))) ?? "").FontSize(8);
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
                        row.AutoItem().Text("STATUS OF OBJECTOR (eg. Tenant, Pending Purchaser, Municipality, etc) ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Objector_Status)).FontSize(8).AlignCenter();
                    });
                });

                // 1.3 AUTHORISED REPRESENTATIVE section
                col.Item().PaddingTop(10).Border(2).Padding(10).Column(repBox =>
                {
                    repBox.Item().Text("1.3 AUTHORISED REPRESENTATIVE OF THE OBJECTOR*").Bold().FontSize(8);

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

                }.Where(x => !string.IsNullOrWhiteSpace(x?.ToString()))) ?? "").FontSize(8);
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

                // ADDRESS line (changed from PHYSICAL ADDRESS to ADDRESS as per PDF)
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("ADDRESS: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.physical_address)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("CODE:").FontSize(8);
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

                // Second line for additional details (as shown in PDF)
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).MinHeight(15).Text("").FontSize(8);
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
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text(string.Equals(Str(s?.Compensation_Paid), "YES", StringComparison.OrdinalIgnoreCase) ? "X" : "")
                        .FontSize(8);

                    // NO label
                    row.AutoItem().PaddingLeft(5).Text("NO: ").FontSize(8);

                    // NO box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text(string.Equals(Str(s?.Compensation_Paid), "NO", StringComparison.OrdinalIgnoreCase) ? "X" : "")
                        .FontSize(8);

                    row.AutoItem().PaddingLeft(10).Text("IF YES DATE OF PAYMENT: ").FontSize(8);
                    row.ConstantColumn(100).BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Payment_Date)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(5).Text("AMOUNT R: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s?.Compensation_Amount)).FontSize(8).AlignCenter();
                });
            });
        }

        //section 3
        private void BuildSection3(IContainer container, dynamic s3)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("SECTION 3: DESCRIPTION OF RESIDENTIAL DWELLING ( FOR SECTIONAL TITLES SEE SECTION 4 ) ( INDICATE NUMBER OR STATE YES / NO )").Bold().FontSize(8);

                // 3.1 MAIN DWELLING
                col.Item().PaddingTop(8).Text("3.1 MAIN DWELLING").Bold().FontSize(8);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("NO OF BEDROOMS: ").FontSize(8);
                    row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_No_of_Bedroom)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("NO OF BATHROOMS: ").FontSize(8);
                    row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_No_of_BathRoom)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("KITCHEN: ").FontSize(8);
                    row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Kitchen)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("LOUNGE: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Lounge)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("DININGROOM: ").FontSize(8);
                    row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Dinning_Room)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("LOUNGE WITH DINING ROOM: ").FontSize(8);
                    row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Lounge_Dining_Room)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("STUDY: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Study)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("PLAYROOM: ").FontSize(8);
                    row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Play_Room)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("TELEVISION: ").FontSize(8);
                    row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Television)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("LAUNDRY: ").FontSize(8);
                    row.ConstantColumn(50).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Laundry)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("SEPARATE TOILET: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Seperate_Toilet)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("OTHER: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Dwell_Other1)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("OTHER: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Dwell_Other2)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text("OTHER: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Dwell_Other3)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("OTHER: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Dwell_Other4)).FontSize(8).AlignCenter();
                });

                // 3.2 OTHER BUILDINGS – ATTACH AS ANNEXURE A
                col.Item().PaddingTop(8).Text("3.2 OTHER BUILDINGS – ATTACH AS ANNEXURE A").Bold().FontSize(8);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("BUILDING NO.: ").FontSize(8);
                    row.ConstantColumn(100).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Building_No)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("DESCRIPTION: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Building_Description)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("SIZE (m2): ").FontSize(8);
                    row.ConstantColumn(100).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Building_Size)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("CONDITION: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Building_Condition)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("IS THE BUILDING FUNCTIONAL?: ").FontSize(8);
                    row.ConstantColumn(80).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Building_Functional)).FontSize(8).AlignCenter();
                });

                // 3.3 IS ANY PORTION OF THE PROPERTY USED FOR ANY PURPOSE OTHER THAN AGRICULTURAL?
                col.Item().PaddingTop(8).Column(innerCol =>
                {
                    innerCol.Item().Text("3.3 IS ANY PORTION OF THE PROPERTY USED FOR ANY PURPOSE OTHER THAN AGRICULTURAL? (e.g. Business, mining, eco-tourism, trading in or hunting of game)").FontSize(8);

                    innerCol.Item().PaddingTop(5).Row(tickRow =>
                    {
                        tickRow.AutoItem().Text("Tick YES: ").FontSize(8);
                        // YES box
                        tickRow.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                            .Text((string.Equals(Str(s3?.Agri_Another_Purpose_Not_Agriculture), "YES", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                            .FontSize(8);
                        // NO label
                        tickRow.AutoItem().PaddingLeft(10).Text("NO: ").FontSize(8);
                        // NO box
                        tickRow.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                            .Text((string.Equals(Str(s3?.Agri_Another_Purpose_Not_Agriculture), "NO", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                            .FontSize(8);
                    });

                    innerCol.Item().PaddingTop(5).Row(useRow =>
                    {
                        useRow.AutoItem().Text("IF YES: DESCRIBE THE USE(S): ").FontSize(8);
                        useRow.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Another_Purpose_Not_Agriculture_Desc)).FontSize(8);
                    });

                    innerCol.Item().BorderBottom(1).MinHeight(15).Text("").FontSize(8);
                    innerCol.Item().BorderBottom(1).MinHeight(15).Text("").FontSize(8);

                    innerCol.Item().PaddingTop(3).Text("IF NECESSARY PROVIDE ANNEXURE B").FontSize(8);
                });

                // 3.4 LAND ANALYSIS
                col.Item().PaddingTop(8).Text("3.4 LAND ANALYSIS:").Bold().FontSize(8);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("NON-AGRICULTURAL (REFER TO 3.3) (ha): ").FontSize(8);
                    row.ConstantColumn(80).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Non_Agricultural)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("GRAZING (ha): ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Grazing)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("UNDER IRRIGATION (ha): ").FontSize(8);
                    row.ConstantColumn(80).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Under_Irrigation)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("DRY LAND (ha): ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Dry_Land)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("PERMANENT CROPS (ha): ").FontSize(8);
                    row.ConstantColumn(80).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Permanent_Crop)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("OTHER (ha): ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Other_ha_1)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("OTHER (ha): ").FontSize(8);
                    row.ConstantColumn(80).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Other_ha_2)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("OTHER (ha): ").FontSize(8);
                    row.ConstantColumn(80).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Other_ha_3)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("TOTAL (ha): ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Total_ha)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("CONDITION OF FENCES: GOOD: ").FontSize(8);
                    // GOOD box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s3?.Agri_Fence_Condition), "GOOD", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                    // AVERAGE label
                    row.AutoItem().PaddingLeft(10).Text("AVERAGE: ").FontSize(8);
                    // AVERAGE box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s3?.Agri_Fence_Condition), "AVERAGE", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                    // POOR label
                    row.AutoItem().PaddingLeft(10).Text("POOR: ").FontSize(8);
                    // POOR box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s3?.Agri_Fence_Condition), "POOR", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                    row.AutoItem().PaddingLeft(10).Text("AREA GAME FENCED (ha): ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Game_Area_Fenced)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("No. OF BOREHOLES: ").FontSize(8);
                    row.ConstantColumn(100).BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Num_of_Boreholes)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("OUTPUT LITRES/HOUR DAMS CAPACITY: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s3?.Agri_Output_litres_Hours)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("IS THE PROPERTY EXPOSED TO A RIVER? YES: ").FontSize(8);
                    // YES box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s3?.Agri_Exposed_To_River), "YES", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                    // NO label
                    row.AutoItem().PaddingLeft(10).Text("NO: ").FontSize(8);
                    // NO box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s3?.Agri_Exposed_To_River), "NO", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                });
            });
        }

        private void BuildSection35(IContainer container, dynamic s35)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("3.5 OTHER").Bold().FontSize(8);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("IS YOUR PROPERTY AFFECTED BY LAND CLAIM? YES: ").FontSize(8);
                    // YES box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s35?.Agri_Land_Claim), "YES", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                    // NO label
                    row.AutoItem().PaddingLeft(10).Text("NO: ").FontSize(8);
                    // NO box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s35?.Agri_Land_Claim), "NO", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text("IF YES, DATE OF CLAIM: ").FontSize(8);
                    row.ConstantColumn(100).BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Claim_Date)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("GAZETTE NO: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Gazette_No)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("DO YOU HAVE WATER RIGHTS? YES: ").FontSize(8);
                    // YES box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s35?.Agri_Water_Rights), "YES", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                    // NO label
                    row.AutoItem().PaddingLeft(10).Text("NO: ").FontSize(8);
                    // NO box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s35?.Agri_Water_Rights), "NO", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text("IF YES, GIVE DETAILS: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Water_Rights_Details)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Column(innerCol =>
                {
                    innerCol.Item().Row(row =>
                    {
                        row.AutoItem().Text("HAVE YOU APPLIED FOR A REZONING OR CONSENT USE? YES: ").FontSize(8);
                        // YES box
                        row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                            .Text((string.Equals(Str(s35?.Agri_Rezoning_Consent_Use), "YES", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                            .FontSize(8);
                        // NO label
                        row.AutoItem().PaddingLeft(10).Text("NO: ").FontSize(8);
                        // NO box
                        row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                            .Text((string.Equals(Str(s35?.Agri_Rezoning_Consent_Use), "NO", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                            .FontSize(8);
                    });
                    innerCol.Item().PaddingTop(2).Text("(CONSENT USE e.g as guest houses, business, etc.)").FontSize(7);
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text("IF YES, GIVE DETAILS: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Consent_Use_Details)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("HAS YOUR AGRICULTURAL HOLDINGS PROPERTY BEEN EXCISED YES: ").FontSize(8);
                    // YES box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s35?.Agri_Land_Excised), "YES", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                    // NO label
                    row.AutoItem().PaddingLeft(10).Text("NO: ").FontSize(8);
                    // NO box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s35?.Agri_Land_Excised), "NO", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text("IF YES: NEW FARM DESCRIPTION: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_New_Farm_Desc)).FontSize(8);
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("HAS THE TOWNSHIP APPLIED FOR OR PROCLAIMED? YES: ").FontSize(8);
                    // YES box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s35?.Agri_Township_Applied), "YES", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                    // NO label
                    row.AutoItem().PaddingLeft(10).Text("NO: ").FontSize(8);
                    // NO box
                    row.ConstantColumn(15).Border(1).Height(12).AlignCenter().AlignMiddle()
                        .Text((string.Equals(Str(s35?.Agri_Township_Applied), "NO", StringComparison.OrdinalIgnoreCase) ? "X" : ""))
                        .FontSize(8);
                });

                col.Item().PaddingTop(3).Row(row =>
                {
                    row.AutoItem().Text("IF YES, GIVE DETAILS: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Township_Applied_Detail)).FontSize(8).AlignCenter();
                });

                // TENANT AND RENT INFORMATION - ANNEXURE C
                col.Item().PaddingTop(8).Text("TENANT AND RENT INFORMATION - ANNEXURE C").Bold().FontSize(8);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("NAME OF TENANT: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Tenant_Name)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("SIZE: ").FontSize(8);
                    row.ConstantColumn(100).BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Rental_Land_Size)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("RENTAL (EXCL VAT): ").FontSize(8);
                    row.ConstantColumn(120).BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Rental)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("ESCALATION: ").FontSize(8);
                    row.ConstantColumn(120).BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Escalation)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("OTHER CONTRIBUTIONS: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Other_contribution)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("TERM OF LEASE: ").FontSize(8);
                    row.ConstantColumn(120).BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Lease_Term)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("START DATE: ").FontSize(8);
                    row.ConstantColumn(120).BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Start_Date)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("USE: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s35?.Agri_Use)).FontSize(8).AlignCenter();
                });
            });
        }

        //Section5
        private void BuildSection4(IContainer container, dynamic s4, string erf, string schemeName)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("SECTION 4: MARKET INFORMATION").Bold().FontSize(8);

                col.Item().PaddingTop(5).Column(innerCol =>
                {
                    innerCol.Item().Text("IF YOUR PROPERTY IS CURRENTLY ON THE MARKET WHAT IS THE ASKING PRICE?").FontSize(8);

                    innerCol.Item().PaddingTop(3).Row(row =>
                    {
                        row.AutoItem().Text("R: ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s4?.CurrentAskingPrice)).FontSize(8).AlignCenter();
                        row.AutoItem().PaddingLeft(10).Text("OFFER RECEIVED R: ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s4?.CurrentOfferReceived)).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(8).Column(innerCol =>
                {
                    innerCol.Item().Text("IF YOUR PROPERTY HAS BEEN ON THE MARKET IN THE LAST 3 YEARS WHAT WAS THE ASKING PRICE?").FontSize(8).AlignCenter();

                    innerCol.Item().PaddingTop(3).Row(row =>
                    {
                        row.AutoItem().Text("R: ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s4?.Past3YearsAskingPrice)).FontSize(8).AlignCenter();
                        row.AutoItem().PaddingLeft(10).Text("OFFER RECEIVED R: ").FontSize(8);
                        row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s4?.Past3YearsOfferReceived)).FontSize(8).AlignCenter();
                    });
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("NAME OF AGENT: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s4?.AgentName)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("TEL NO.: ").FontSize(8);
                    row.ConstantColumn(150).BorderBottom(1).PaddingBottom(2).Text((string)Str(s4?.AgentTel)).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(8).Text("SALES TRANSACTIONS (OF OTHER PROPERTIES IN THE VICINITY) USED BY OBJECTOR IN DETERMINING THE MARKET VALUE OF THE PROPERTY OBJECTED TO").FontSize(8);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("ERF/UNIT NO.: ").FontSize(8);
                    row.ConstantColumn(150).BorderBottom(1).PaddingBottom(2).Text((string)(erf ?? Str(s4?.SalesErfNo))).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("SUBURB/FARM/SCHEME NAME: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)(schemeName ?? Str(s4?.SalesSchemeName))).FontSize(8).AlignCenter();
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("DATE OF SALE: ").FontSize(8);
                    row.ConstantColumn(150).BorderBottom(1).PaddingBottom(2).Text((string)Str(s4?.SalesDate)).FontSize(8).AlignCenter();
                    row.AutoItem().PaddingLeft(10).Text("SELLING PRICE: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s4?.SellingPrice)).FontSize(8).AlignCenter();
                });
            });
        }

        //Section 6
        private void BuildSection5(IContainer container, dynamic s)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text($"SECTION 5: {_wording.InquiryLabel} DETAILS").Bold().FontSize(8);

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

        //Declaration
        private void BuildSection6(IContainer container, dynamic s6)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("SECTION 6: DECLARATION").Bold().FontSize(8);

                col.Item().PaddingTop(5).Column(innerCol =>
                {
                    innerCol.Item().Text("ATTENTION IS HEREBY DRAWN TO SECTION 42(2) OF THE ACT WHICH STATES THAT WHERE ANY DOCUMENT, " +
                        "INFORMATION OR PARTICULARS WERE NOT PROVIDED WHEN REQUIRED IN TERMS OF SUBSECTION 42(1) OF THE ACT AND THE OWNER CONCERNED RELIES ON SUCH DOCUMENT," +
                        " INFORMATION OR PARTICULARS IN AN APPEAL TO AN APPEAL BOARD, THE APPEAL BOARD MAY MAKE AN ORDER AS TO COSTS IN TERMS OF SECTION 70 OF THE ACT IF THE APPEAL BOARD IS OF THE VIEW THAT THE FAILURE TO SO HAVE PROVIDED ANY SUCH DOCUMENT, " +
                        "INFORMATION OR PARTICULARS HAS PLACED AN UNNECESSARY BURDEN ON THE FUNCTIONS OF THE MUNICIPAL VALUER OR THE APPEAL BOARD.").FontSize(7).Bold();
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.AutoItem().Text("I / WE: ").FontSize(8).Bold();
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s6?.Signature_Name)).FontSize(8);
                    row.AutoItem().PaddingLeft(5).Text("HEREBY DECLARE THAT THE INFORMATION AND PARTICULARS SUPPLIED ARE TRUE AND CORRECT").FontSize(8).Bold();
                });

                string year = "";
                string month = "";
                string day = "";
                if (s6?.Declaration_Date != null)
                {
                    if (DateTime.TryParse((string)s6.Declaration_Date, out DateTime parsedDate))
                    {
                        year = parsedDate.Year.ToString();
                        month = parsedDate.Month.ToString("00");
                        day = parsedDate.Day.ToString("00");
                    }
                }
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
                        if (!string.IsNullOrEmpty((string?)s6?.Signature_Picture))
                        {
                            try
                            {
                                var base64 = ((string)s6.Signature_Picture).Split(',').Last();
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

        private void BuildSection7(IContainer container, dynamic s)
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
        private void BuildSection8(IContainer container, dynamic s)
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

        private void BuildSection9(IContainer container, dynamic s9)
        {
            container.Border(2).Padding(10).Column(col =>
            {
                col.Item().Text("OFFICIAL USE").Bold().FontSize(8);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text($"{_wording.NumberLabel}").FontSize(8);
                    row.ConstantColumn(150).BorderBottom(1).PaddingBottom(2).Text((string)Str(s9?.Objection_No)).FontSize(8);
                    row.AutoItem().PaddingLeft(10).Text($"SIGNATURE OF PERSON WHO RECEIVED THE {_wording.InquiryLabel}: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s9?.ReceiverSignature)).FontSize(8);
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("NAME OF OWNER: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s9?.OwnerName)).FontSize(8);
                    row.AutoItem().PaddingLeft(10).Text("DATE: ").FontSize(8);
                    row.ConstantColumn(150).BorderBottom(1).PaddingBottom(2).Text((string)Str(s9?.Date)).FontSize(8);
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text($"NAME OF {_wording.PartyLabel} IF NOT THE SAME: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s9?.ObjectorName)).FontSize(8);
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text("ERF NUMBER: ").FontSize(8);
                    row.ConstantColumn(150).BorderBottom(1).PaddingBottom(2).Text((string)Str(s9?.ErfNumber)).FontSize(8);
                    row.AutoItem().PaddingLeft(10).Text("TOWNSHIP NAME: ").FontSize(8);
                    row.RelativeColumn().BorderBottom(1).PaddingBottom(2).Text((string)Str(s9?.TownshipName)).FontSize(8);
                });

                col.Item().PaddingTop(5).BorderBottom(1).MinHeight(20).Text((string)Str(s9?.AdditionalNotes)).FontSize(8);
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