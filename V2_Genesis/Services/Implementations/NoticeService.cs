using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using V2_Genesis.Models;
using V2_Genesis.Models.Results;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Notice;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Services.Implementations;

public class NoticeService : INoticeService
{
    private readonly IPropertySearchService _search;
    private readonly IWebHostEnvironment _env;
    private readonly NoticeRollSettings _noticeSettings;
    private readonly RollDatesSettings _rollDates;
    private readonly ILogger<NoticeService> _logger;

    private const string HEADER_IMAGE = "Images/Obj_Header.PNG";

    public NoticeService(
        IPropertySearchService search,
        IWebHostEnvironment env,
        IOptions<NoticeRollSettings> noticeOpts,
        IOptions<RollDatesSettings> rollDatesOpts,
        ILogger<NoticeService> logger)
    {
        _search = search;
        _env = env;
        _noticeSettings = noticeOpts.Value;
        _rollDates = rollDatesOpts.Value;
        _logger = logger;

        // Set QuestPDF community licence (free for open-source / internal tools)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Section 49 ────────────────────────────────────────────────────
    public async Task<(byte[] Pdf, string FileName)> GenerateSection49Async(
        string rollSource,
        string unitKey,
        string valuationKey)
    {
        var items = await _search.GetPropertyDetailsAsync(
            rollSource, unitKey, valuationKey);

        if (!items.Any())
            throw new InvalidOperationException(
                $"No property found for UnitKey={unitKey}, ValuationKey={valuationKey}");

        var roll = _noticeSettings.For(rollSource);
        var dates = _rollDates.For(rollSource);
        var main = items.First();

        var fileName = $"Section49_{SanitiseName(main.PropertyDesc ?? unitKey)}.pdf";

        var pdfBytes = GenerateSection49Pdf(items, main, roll, dates);

        // ── Save copy to disk ─────────────────────────────────────────
        await SaveToDiskAsync(roll.Section49Path, main.PropertyDesc, pdfBytes);

        return (pdfBytes, fileName);
    }

    // ── PDF build ─────────────────────────────────────────────────────
    private byte[] GenerateSection49Pdf(
        List<PropertyDetailResult> items,
        PropertyDetailResult main,
        NoticeRollEntry roll,
        RollDateEntry dates)
    {
        var headerPath = Path.Combine(_env.WebRootPath, HEADER_IMAGE);
        var signaturePath = Path.Combine(_env.ContentRootPath, roll.SignatureFile);

        var periodText = $"{dates.OpenDate:dd MMMM yyyy} – {dates.VisibleUntil:dd MMMM yyyy} until 15:00";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                page.Content().Column(col =>
                {
                    // ══════════════ PAGE 1 ══════════════════════════
                    // Header image
                    if (File.Exists(headerPath))
                        col.Item().Width(500).Image(headerPath);

                    col.Item().Height(8);

                    // Owner address + date row
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(addr =>
                        {
                            foreach (var line in new[]
                                { main.ADDR1, main.ADDR2, main.ADDR3,
                                  main.ADDR4, main.ADDR5 }
                                .Where(a => !string.IsNullOrWhiteSpace(a)))
                            {
                                addr.Item().Text(line!)
                                    .FontSize(9).Bold();
                            }
                        });
                        row.RelativeItem().AlignRight()
                            .Text(DateTime.Now.ToString("dd MMMM yyyy"))
                            .FontSize(10);
                    });

                    col.Item().Height(10);

                    // ── Title ─────────────────────────────────────
                    col.Item().AlignCenter()
                        .Text("CITY OF JOHANNESBURG")
                        .FontSize(13).Bold();

                    col.Item().Height(4);

                    col.Item().AlignCenter()
                        .Text($"PUBLIC NOTICE CALLING FOR INSPECTION OF THE {roll.RollTitle.ToUpper()} AND LODGING OF OBJECTIONS")
                        .FontSize(10).Bold();

                    col.Item().Height(6);

                    col.Item().BorderBottom(1).BorderColor("#555555");

                    col.Item().Height(8);

                    // ── Notice paragraphs ──────────────────────────
                    col.Item().PaddingBottom(6).Text(t =>
                    {
                        t.Justify();
                        t.Span("Notice is hereby given in terms of Section 49(1)(a)(i) read together with section 78(2) of the ");
                        t.Span("Local Government: Municipal Property Rates Act No. 6 of 2004").Bold();
                        t.Span($" as amended, that the valuation roll for the financial years ");
                        t.Span(roll.FinancialYears).Bold();
                        t.Span(" is open for public inspection from ");
                        t.Span(periodText).Bold();
                        t.Span(".");
                        if (!string.IsNullOrEmpty(dates.ExtendedPeriodText))
                        {
                            t.Span(" " + dates.ExtendedPeriodText).Bold();
                        }
                        t.Span(" In addition, the valuation roll is available on the City's website ");
                        t.Span("www.joburg.org.za").Bold();
                        t.Span(", under the GVR Online tile on the home page.");
                    });

                    col.Item().PaddingBottom(6).Text(
                        "An invitation is hereby made in terms of section 49(1)(a)(ii) of the Act to any owner of property or other person " +
                        "who so desires to lodge an objection with the Municipal Manager in respect of any matter reflected in, or omitted " +
                        "from, the valuation roll within the above mentioned inspection period.")
                        .Justify();

                    col.Item().PaddingBottom(6).Text(t =>
                    {
                        t.Justify();
                        t.Span("Attention is specifically drawn to the fact that in terms of section 50(2) of the Act an objection must be in relation to a ");
                        t.Span("specific individual property").Bold();
                        t.Span(" and not against the valuation roll as such. Objections can be submitted online at ");
                        t.Span("www.joburg.org.za").Bold();
                        t.Span(", under the GVR Online tile on the home page.");
                    });

                    col.Item().PaddingBottom(8).Text(
                        "The completed forms could be returned to the following address or preferably submitted online.")
                        .Justify();

                    // ── Address box ────────────────────────────────
                    col.Item().Background("#F0F0F0").Border(1).BorderColor("#888888")
                        .Padding(8).Column(box =>
                        {
                            box.Item().Text("Valuation Services: Administration").Bold().FontSize(9);
                            box.Item().Text("Jorissen Place, 66 Jorissen Street, Braamfontein, East Wing, 1st Floor").FontSize(9);
                        });

                    col.Item().Height(8);

                    col.Item().Text(
                        "The acknowledgement letter will be generated by the online system and should be kept as proof that the objection was submitted.")
                        .Justify();

                    // ══════════════ PAGE BREAK ═══════════════════════
                    col.Item().PageBreak();

                    // ══════════════ PAGE 2 ══════════════════════════
                    col.Item().BorderBottom(1).BorderColor("#555555");
                    col.Item().Height(6);

                    col.Item().AlignCenter()
                        .Text($"PROPERTY DETAILS AS LISTED IN {roll.RollTitle.ToUpper()}")
                        .FontSize(10).Bold();

                    col.Item().Height(6);

                    // Property info box
                    col.Item().Background("#F0F7FF").Border(1).BorderColor("#4682B4")
                        .Padding(7).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t =>
                                {
                                    t.Span("Property Description: ").Bold();
                                    t.Span(main.PropertyDesc ?? "–");
                                });
                                c.Item().Text(t =>
                                {
                                    t.Span("Physical Address: ").Bold();
                                    t.Span(main.LisStreetAddress ?? "–");
                                });
                            });
                        });

                    col.Item().Height(6);

                    // Property valuation table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);   // Market Value
                            c.RelativeColumn(2);   // Extent
                            c.RelativeColumn(3);   // Category
                            c.RelativeColumn(4);   // Remarks
                        });

                        // Headers
                        static IContainer HeaderCell(IContainer c) =>
                            c.Background("#4682B4").Padding(5).AlignCenter();

                        foreach (var h in new[] { "Market Value", "Extent", "Property Category", "Remarks" })
                            table.Cell().Element(HeaderCell)
                                .Text(h).FontColor(Colors.White).Bold().FontSize(8);

                        // Rows
                        bool alt = false;
                        foreach (var item in items)
                        {
                            var bg = alt ? Colors.Grey.Lighten5 : Colors.White;
                            alt = !alt;

                            IContainer DataCell(IContainer c) =>
                                c.Background(bg).Padding(4);

                            table.Cell().Element(DataCell).AlignRight()
                                .Text(FormatZAR(item.MarketValue)).FontSize(8);
                            table.Cell().Element(DataCell).AlignCenter()
                                .Text(item.RateableArea ?? "–").FontSize(8);
                            table.Cell().Element(DataCell)
                                .Text(item.CatDesc ?? "–").FontSize(8);
                            table.Cell().Element(DataCell)
                                .Text(item.Reason ?? "–").FontSize(8);
                        }
                    });

                    col.Item().Height(8);

                    // Closing date warning
                    col.Item().Background("#FFF5E6").Border(1.5f).BorderColor("#FF8C00")
                        .Padding(6).AlignCenter()
                        .Text($"⚠ CLOSING DATE FOR OBJECTIONS IS 15:00 ON {dates.VisibleUntil:dd MMMM yyyy}")
                        .Bold().FontSize(9);

                    col.Item().Height(8);

                    // Contact info
                    col.Item().PaddingBottom(8).Text(t =>
                    {
                        t.Span("For further enquiries: ").Bold();
                        t.Span("Tel. 011 407-6622 or 011 407-6597  |  valuationenquiries@joburg.org.za");
                    });

                    // NB title
                    col.Item().AlignCenter()
                        .Text("NB. REQUIRED DOCUMENTATION FOR OBJECTIONS")
                        .FontSize(10).Bold();
                    col.Item().Height(6);

                    // Two-column requirements
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        // Residential cell
                        table.Cell().Background("#F0F8FF").Border(1).BorderColor("#4682B4")
                            .Padding(8).Column(c =>
                            {
                                c.Item().Text("Residential Properties").Bold().FontSize(9);
                                c.Item().Height(4);
                                foreach (var line in new[]
                                {
                                "• Market evidence (sold properties within immediate area)",
                                "• Number of bedrooms and bathrooms",
                                "• Improvements (swimming pool, etc.)",
                                "• Age of improvements",
                                "• Any adverse conditions affecting value",
                                "• Building sizes and types (garage, granny flat)",
                                "• Any other additional information",
                                "",
                                "OR",
                                "• Motivated valuation report from registered Valuer"
                            })
                                    c.Item().Text(line).FontSize(8);
                            });

                        // Business cell
                        table.Cell().Background("#FFF8F0").Border(1).BorderColor("#FF8C00")
                            .Padding(8).Column(c =>
                            {
                                c.Item().Text("Business Properties").Bold().FontSize(9);
                                c.Item().Height(4);
                                foreach (var line in new[]
                                {
                                "• Rent Roll (if there are tenants)",
                                "• Size of building (if no tenants)",
                                "• Actual use of building",
                                "• Income / Expenditure",
                                "• Number of parking bays",
                                "• Condition of the building (attach photos)",
                                "• Any other additional information",
                                "",
                                "OR",
                                "• Motivated valuation report from registered Valuer"
                            })
                                    c.Item().Text(line).FontSize(8);
                            });
                    });

                    col.Item().Height(8);

                    // Representatives warning
                    col.Item().Background("#FFE0E0").Border(2).BorderColor("#DC143C")
                        .Padding(8).Column(c =>
                        {
                            c.Item().Text("⚠ REPRESENTATIVES:").Bold().FontSize(9);
                            c.Item().Height(4);
                            c.Item().Text(
                                "Letter of authorisation MUST be signed by the registered owner and attached to the objection form.")
                                .FontSize(8);
                        });

                    col.Item().Height(10);

                    // Signature
                    if (File.Exists(signaturePath))
                        col.Item().Width(150).Image(signaturePath);

                    col.Item().Height(6);

                    // Footer
                    col.Item().BorderTop(1).BorderColor("#AAAAAA")
                        .PaddingTop(6).AlignCenter().Column(f =>
                        {
                            f.Item().Text(
                                "This is an official document generated by the City of Johannesburg Valuation Services Department")
                                .FontSize(7).FontColor("#666666");
                            f.Item().Text($"Generated on: {DateTime.Now:dd MMMM yyyy}")
                                .FontSize(7).FontColor("#666666");
                        });
                });
            });
        }).GeneratePdf();
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private static string FormatZAR(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return "–";
        if (!decimal.TryParse(val.Replace(",", "").Replace("R", "").Trim(),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var n))
            return val;
        return "R " + n.ToString("N0", new System.Globalization.CultureInfo("en-ZA"));
    }

    private static string SanitiseName(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? "Property"
            : string.Concat(name.Split(Path.GetInvalidFileNameChars()))
                    .Replace(" ", "_")[..Math.Min(name.Length, 80)];

    private async Task SaveToDiskAsync(string folderPath, string? propertyDesc, byte[] pdf)
    {
        try
        {
            var safeName = SanitiseName(propertyDesc);
            var dir = Path.Combine(folderPath, safeName);
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"Section49_{safeName}.pdf");
            await File.WriteAllBytesAsync(path, pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Section 49 PDF to disk for property: {Prop}",
                propertyDesc);
        }
    }
}