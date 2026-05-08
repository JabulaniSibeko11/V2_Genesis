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
    public Task<(byte[] Pdf, string FileName)> GenerateAcknowledgementAsync(
    AcknowledgementData data)
    {
        var roll = _noticeSettings.For(data.RollSource);
        var dates = _rollDates.For(data.RollSource);
        var fileName = $"Acknowledgement_{SanitiseName(data.ObjectionNo)}.pdf";

        var pdfBytes = BuildAcknowledgementPdf(data, roll, dates);

        // Save copy to disk (non-blocking)
        _ = SaveAckToDiskAsync(roll, data, pdfBytes);

        return Task.FromResult((pdfBytes, fileName));
    }

    // ── PDF builder ───────────────────────────────────────────────────────
    private byte[] BuildAcknowledgementPdf(
        AcknowledgementData data,
        NoticeRollEntry roll,
        RollDateEntry? dates)
    {
        var headerPath = Path.Combine(_env.WebRootPath, HEADER_IMAGE);
        var signaturePath = Path.Combine(_env.ContentRootPath, roll.SignatureFile);
        var titleLabel = data.IsMulti
            ? "ACKNOWLEDGEMENT OF MULTIPURPOSE OBJECTION"
            : "ACKNOWLEDGEMENT OF OBJECTION";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                page.Content().Column(col =>
                {
                    // ── Header ────────────────────────────────────────
                    if (File.Exists(headerPath))
                        col.Item().Width(500).Image(headerPath);

                    col.Item().Height(8);

                    // Date right-aligned
                    col.Item().AlignRight()
                        .Text(DateTime.Now.ToString("dd MMMM yyyy"))
                        .FontSize(10);

                    col.Item().Height(6);
                    col.Item().BorderBottom(1).BorderColor("#555555");
                    col.Item().Height(6);

                    // ── Title ─────────────────────────────────────────
                    col.Item().AlignCenter()
                        .Text("CITY OF JOHANNESBURG")
                        .FontSize(13).Bold();

                    col.Item().Height(4);

                    col.Item().AlignCenter()
                        .Text(titleLabel)
                        .FontSize(11).Bold();

                    col.Item().Height(4);

                    col.Item().AlignCenter()
                        .Text(roll.RollTitle.ToUpper())
                        .FontSize(10).Bold();

                    col.Item().Height(10);

                    // ── Objection reference box ───────────────────────
                    col.Item()
                        .Background("#1a1a1a")
                        .Border(1).BorderColor("#e6b000")
                        .Padding(10).Column(box =>
                        {
                            box.Item().AlignCenter()
                                .Text("OBJECTION REFERENCE NUMBER")
                                .FontColor("#e6b000").FontSize(9).Bold();
                            box.Item().Height(4);
                            box.Item().AlignCenter()
                                .Text(data.ObjectionNo)
                                .FontColor(Colors.White).FontSize(16).Bold();
                            box.Item().Height(4);
                            box.Item().AlignCenter()
                                .Text($"Submitted: {data.SubmissionTime}")
                                .FontColor("#e6b000").FontSize(8);
                        });

                    col.Item().Height(10);

                    // ── Property info — Section 1 ─────────────────────
                    AckSectionTable(col,
                        label: data.IsMulti ? "SECTION 1 — PROPERTY DETAILS" : "PROPERTY DETAILS",
                        oldDesc: data.Old_PropertyDescription,
                        oldCat: data.Old_Category,
                        oldAddr: data.Old_Address,
                        oldExt: data.Old_Extent,
                        oldMv: data.Old_MarketValue,
                        oldOwner: data.Old_Owner,
                        newDesc: data.New_PropertyDescription,
                        newCat: data.New_Category,
                        newAddr: data.New_Address,
                        newExt: data.New_Extent,
                        newMv: data.New_MarketValue,
                        newOwner: data.New_Owner,
                        isFirst: true);

                    // ── Multi: Section 2 ──────────────────────────────
                    if (data.IsMulti)
                    {
                        col.Item().Height(8);
                        AckSectionTable(col,
                            label: "SECTION 2 — SECOND USE DETAILS",
                            oldDesc: null, oldAddr: null,
                            oldOwner: null, newDesc: null,
                            newAddr: null, newOwner: null,
                            oldCat: data.Old2_Category,
                            oldExt: data.Old2_Extent,
                            oldMv: data.Old2_MarketValue,
                            newCat: data.New2_Category,
                            newExt: data.New2_Extent,
                            newMv: data.New2_MarketValue,
                            isFirst: false);

                        // ── Multi: Section 3 (only if any value exists)
                        bool hasSection3 =
                            !string.IsNullOrWhiteSpace(data.Old3_Category) ||
                            !string.IsNullOrWhiteSpace(data.New3_Category);

                        if (hasSection3)
                        {
                            col.Item().Height(8);
                            AckSectionTable(col,
                                label: "SECTION 3 — THIRD USE DETAILS",
                                oldDesc: null, oldAddr: null,
                                oldOwner: null, newDesc: null,
                                newAddr: null, newOwner: null,
                                oldCat: data.Old3_Category,
                                oldExt: data.Old3_Extent,
                                oldMv: data.Old3_MarketValue,
                                newCat: data.New3_Category,
                                newExt: data.New3_Extent,
                                newMv: data.New3_MarketValue,
                                isFirst: false);
                        }
                    }

                    col.Item().Height(10);

                    // ── Reason ────────────────────────────────────────
                    if (!string.IsNullOrWhiteSpace(data.ObjectionReason))
                    {
                        col.Item().Background("#FFF9E6").Border(1).BorderColor("#e6b000")
                            .Padding(8).Column(r =>
                            {
                                r.Item().Text("REASON FOR OBJECTION").Bold().FontSize(8);
                                r.Item().Height(3);
                                r.Item().Text(data.ObjectionReason).FontSize(8);
                            });
                        col.Item().Height(8);
                    }

                    // ── Documents submitted ───────────────────────────
                    col.Item().Background("#F0F0F0").Border(1).BorderColor("#888")
                        .Padding(8).Row(r =>
                        {
                            r.AutoItem()
                            .Text("📎 Supporting Documents Submitted:")
                            .Bold().FontSize(9);
                            r.RelativeItem().AlignRight()
                            .Text($"{data.FileCount} file(s)")
                            .Bold().FontSize(9).FontColor("#1a1a1a");
                        });

                    col.Item().Height(10);

                    // ── Closing note ──────────────────────────────────
                    col.Item().Background("#E8F5E9").Border(1).BorderColor("#388E3C")
                        .Padding(8).Column(c =>
                        {
                            c.Item().Text("IMPORTANT").Bold().FontSize(9).FontColor("#1B5E20");
                            c.Item().Height(3);
                            c.Item().Text(
                            "Please keep this acknowledgement as proof that your objection was successfully submitted. " +
                            "The Municipal Valuer will consider your objection and communicate the outcome in due course. " +
                            "Should you require assistance, contact Valuation Services on 011 407-6622.")
                            .FontSize(8);
                        });

                    col.Item().Height(14);

                    // ── Closing date ──────────────────────────────────
                    if (dates is not null)
                    {
                        col.Item().Background("#FFF5E6").Border(1.5f).BorderColor("#FF8C00")
                            .Padding(6).AlignCenter()
                            .Text($"⚠ OBJECTION PERIOD CLOSES: {dates.VisibleUntil:dd MMMM yyyy} AT 15:00")
                            .Bold().FontSize(9);

                        col.Item().Height(10);
                    }

                    // ── Signature ─────────────────────────────────────
                    if (File.Exists(signaturePath))
                        col.Item().Width(150).Image(signaturePath);

                    col.Item().Height(4);

                    // ── Footer ────────────────────────────────────────
                    col.Item().BorderTop(1).BorderColor("#AAAAAA").PaddingTop(6)
                        .AlignCenter().Column(f =>
                        {
                            f.Item().Text(
                            "Official document — City of Johannesburg Valuation Services")
                            .FontSize(7).FontColor("#666666");
                            f.Item().Text($"Generated: {DateTime.Now:dd MMMM yyyy HH:mm}")
                            .FontSize(7).FontColor("#666666");
                        });
                });
            });
        }).GeneratePdf();
    }

    // ── Helper: renders one section's comparison table ────────────────────
    private static void AckSectionTable(
        ColumnDescriptor col,
        string label,
        string? oldDesc, string? oldCat, string? oldAddr,
        string? oldExt, string? oldMv, string? oldOwner,
        string? newDesc, string? newCat, string? newAddr,
        string? newExt, string? newMv, string? newOwner,
        bool isFirst)
    {
        // Section label
        col.Item().Background("#1a1a1a").Padding(6).Row(r =>
        {
            r.RelativeItem()
                .Text(label)
                .FontColor("#e6b000").Bold().FontSize(9);
        });

        // Comparison table: Field | Roll Value | Objector's Value
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.5f);   // Field
                c.RelativeColumn(3.5f);   // Roll (old)
                c.RelativeColumn(3.5f);   // Claimed (new)
            });

            // Header row
            static IContainer TH(IContainer c) =>
                c.Background("#333333").Padding(6).AlignCenter();

            table.Cell().Element(TH).Text("Field").FontColor(Colors.White).Bold().FontSize(8);
            table.Cell().Element(TH).Text("Current (Roll)").FontColor(Colors.White).Bold().FontSize(8);
            table.Cell().Element(TH).Text("Objector's Value").FontColor(Colors.White).Bold().FontSize(8);

            // Rows — only render if at least one of old/new has a value
            void Row(string field, string? oldVal, string? newVal)
            {
                if (string.IsNullOrWhiteSpace(oldVal) && string.IsNullOrWhiteSpace(newVal))
                    return;

                bool changed = !string.IsNullOrWhiteSpace(newVal) &&
                               newVal != oldVal;

                static IContainer TD(IContainer c, bool highlight = false) =>
                    c.Background(highlight ? "#FFF9C4" : Colors.White)
                     .BorderBottom(0.5f).BorderColor("#EEEEEE")
                     .Padding(5);

                table.Cell().Element(c => TD(c)).Text(field).Bold().FontSize(8);
                table.Cell().Element(c => TD(c)).Text(oldVal ?? "–").FontSize(8);
                table.Cell().Element(c => TD(c, changed)).Column(nc =>
                {
                    nc.Item().Text(string.IsNullOrWhiteSpace(newVal) ? "No change" : newVal)
                        .FontSize(8)
                        .FontColor(changed ? "#B45309" : Colors.Black);
                    if (changed)
                        nc.Item().Text("✱ Changed").FontSize(6).FontColor("#B45309");
                });
            }

            // Only show description/address/owner for Section 1 (isFirst)
            if (isFirst)
            {
                Row("Property Description", oldDesc, newDesc);
                Row("Owner", oldOwner, newOwner);
                Row("Physical Address", oldAddr, newAddr);
            }
            Row("Category", oldCat, newCat);
            Row("Extent (m²)", oldExt, newExt);
            Row("Market Value", oldMv, newMv);
        });
    }

    // ── Save acknowledgement to disk ──────────────────────────────────────
    private async Task SaveAckToDiskAsync(
        NoticeRollEntry roll, AcknowledgementData data, byte[] pdf)
    {
        try
        {
            var dir = Path.Combine(roll.Section49Path
                           .Replace("Section49", "Acknowledgements"),
                           SanitiseName(data.ObjectionNo));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir,
                           $"Acknowledgement_{SanitiseName(data.ObjectionNo)}.pdf");
            await File.WriteAllBytesAsync(path, pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to save acknowledgement to disk for objection: {Obj}",
                data.ObjectionNo);
        }
    }
    public Task<(byte[] Pdf, string FileName)> GenerateAttachmentConfirmationAsync(
    string objectionNo, string rollSource,
    int fileCount, List<string> fileNames)
    {
        var roll = _noticeSettings.For(rollSource);
        var fileName = $"Attachment_{SanitiseName(objectionNo)}.pdf";
        var header = Path.Combine(_env.WebRootPath, HEADER_IMAGE);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                page.Content().Column(col =>
                {
                    // Header
                    if (File.Exists(header))
                        col.Item().Width(500).Image(header);

                    col.Item().Height(8);

                    col.Item().AlignRight()
                        .Text(DateTime.Now.ToString("dd MMMM yyyy HH:mm"))
                        .FontSize(9);

                    col.Item().Height(6);
                    col.Item().BorderBottom(1).BorderColor("#555555");
                    col.Item().Height(6);

                    col.Item().AlignCenter()
                        .Text("CITY OF JOHANNESBURG").FontSize(13).Bold();

                    col.Item().Height(4);

                    col.Item().AlignCenter()
                        .Text("ATTACHMENT UPLOAD CONFIRMATION").FontSize(10).Bold();

                    col.Item().Height(6);
                    col.Item().BorderBottom(1).BorderColor("#555555");
                    col.Item().Height(8);

                    // Notice
                    col.Item().PaddingBottom(6).Text(t =>
                    {
                        t.Span("This is to confirm that your documents have been " +
                               "successfully uploaded. ");
                        t.Span("IMPORTANT: ").Bold();
                        t.Span("You have 48 hours from original submission to " +
                               "upload outstanding evidence.");
                    });

                    // Reference box
                    col.Item().Background("#F0F0F0").Border(1).BorderColor("#888888")
                        .Padding(8).Column(box =>
                        {
                            box.Item().Text(t =>
                            {
                                t.Span("Objection/Appeal Number: ").Bold();
                                t.Span(objectionNo);
                            });
                            box.Item().Text(t =>
                            {
                                t.Span("Roll: ").Bold();
                                t.Span(roll.RollTitle);
                            });
                            box.Item().Text(t =>
                            {
                                t.Span("Upload Date/Time: ").Bold();
                                t.Span(DateTime.Now.ToString("dd MMMM yyyy HH:mm"));
                            });
                            box.Item().Text(t =>
                            {
                                t.Span("Total Documents Uploaded: ").Bold();
                                t.Span(fileCount.ToString());
                            });
                        });

                    col.Item().Height(10);

                    // File list
                    col.Item().BorderBottom(1).BorderColor("#DDDDDD");
                    col.Item().Height(4);

                    col.Item().AlignCenter()
                        .Text("UPLOADED DOCUMENTS").Bold().FontSize(9);

                    col.Item().Height(6);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(30);   // #
                            c.RelativeColumn();     // filename
                        });

                        // Header
                        static IContainer TH(IContainer c) =>
                            c.Background("#1a1a1a").Padding(6);

                        table.Cell().Element(TH)
                            .Text("#").FontColor(Colors.White).Bold().FontSize(8);
                        table.Cell().Element(TH)
                            .Text("File Name").FontColor(Colors.White).Bold().FontSize(8);

                        bool alt = false;
                        for (int i = 0; i < fileNames.Count; i++)
                        {
                           
                            var bg = alt ? Colors.Grey.Lighten5 : Colors.White;
                            alt = !alt;

                            IContainer TD(IContainer c) =>
                                c.Background(bg).BorderBottom(0.5f)
                                 .BorderColor("#EEEEEE").Padding(5);

                            table.Cell().Element(TD)
                                .Text((i + 1).ToString()).FontSize(8);
                            table.Cell().Element(TD)
                                .Text(fileNames[i]).FontSize(8);
                        }
                    });

                    col.Item().Height(14);

                    // Footer
                    col.Item().BorderTop(1).BorderColor("#AAAAAA").PaddingTop(6)
                        .AlignCenter().Column(f =>
                        {
                            f.Item().Text(
                            "Official document — City of Johannesburg " +
                            "Valuation Services Department")
                            .FontSize(7).FontColor("#666666");
                            f.Item().Text(
                            $"Generated: {DateTime.Now:dd MMMM yyyy HH:mm}")
                            .FontSize(7).FontColor("#666666");
                        });
                });
            });
        }).GeneratePdf();

        return Task.FromResult((pdf, fileName));
    }
}