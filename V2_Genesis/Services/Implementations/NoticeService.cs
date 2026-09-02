using Dapper;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data.SqlClient;
using V2_Genesis.Models;
using V2_Genesis.Helpers;
using V2_Genesis.Models.Notice;
using V2_Genesis.Models.Results;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Notice;
using V2_Genesis.Services.PropertySearch;
using static QuestPDF.Helpers.Colors;

namespace V2_Genesis.Services.Implementations;

public class NoticeService : INoticeService
{
    private readonly IPropertySearchService _search;
    private readonly IWebHostEnvironment _env;
    private readonly NoticeRollSettings _noticeSettings;
    private readonly RollDatesSettings _rollDates;
    private readonly ILogger<NoticeService> _logger;
    private readonly IConfiguration _config;

    private const string HEADER_IMAGE = "Images/Obj_Header.PNG";

    public NoticeService(
        IPropertySearchService search,
        IWebHostEnvironment env,
        IOptions<NoticeRollSettings> noticeOpts,
        IOptions<RollDatesSettings> rollDatesOpts,
        ILogger<NoticeService> logger,
        IConfiguration config)
    {
        _search = search;
        _env = env;
        _noticeSettings = noticeOpts.Value;
        _rollDates = rollDatesOpts.Value;
        _config = config;
        _logger = logger;

        // Set QuestPDF community licence (free for open-source / internal tools)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Roll registry ────────────────────────────────────────────────
    private record RollCfg(
        string ConnKey,
        string RootPath,
        string Short,
        string Name);

    private List<RollCfg> Rolls => new()
    {
        new("DefaultConnection",
            _config["ObjectionRolls:Objection:RootPath"]       ?? "",
            "GV", "General Valuation Roll 2023"),
        new("Sup1Connection",
            _config["ObjectionRolls:Objection_Supp1:RootPath"] ?? "",
            "SUP1", "Supplementary Roll 1"),
        new("Sup2Connection",
            _config["ObjectionRolls:Objection_Supp2:RootPath"] ?? "",
            "SUP2", "Supplementary Roll 2"),
        new("Sup3Connection",
            _config["ObjectionRolls:Objection_Supp3:RootPath"] ?? "",
            "SUP3", "Supplementary Roll 3"),
         new("Sup4Connection",
            _config["ObjectionRolls:Objection_Supp4:RootPath"] ?? "",
            "SUP4", "Supplementary Roll 4"),
           new("Sup5Connection",
            _config["ObjectionRolls:Objection_Supp5:RootPath"] ?? "",
            "SUP5", "Supplementary Roll 5"),
    };
    private string Section49Root => _config["AppSettings:Section49RootPath"] ?? "";
    private string AppealRoot => _config["AppSettings:AppealRootPath"] ?? "";
    private string QueryRoot => _config["ObjectionRolls:Objection_Query:QueryRootPath"] ?? "";




    // ── Section 49 ──────────────────────────────────────────────────────────────
    public async Task<(byte[] Pdf, string FileName)> GenerateSection49Async(
        string rollSource,
        string unitKey,
        string valuationKey)
    {
        var items = await _search.GetPropertyDetailsAsync(
            rollSource,
            unitKey,
            valuationKey);

        if (!items.Any())
            throw new InvalidOperationException(
                $"No property found for UnitKey={unitKey}, ValuationKey={valuationKey}");

        var roll = _noticeSettings.For(rollSource);
        var dates = _rollDates.For(rollSource);
        var main = items.First();

        var fileName =
            $"Section49_{SanitiseName(main.PropertyDesc ?? unitKey)}.pdf";

        var pdfBytes =
            GenerateSection49Pdf(
                items,
                main,
                roll,
                dates,
                rollSource);

        await SaveToDiskAsync(
            roll.Section49Path,
            main.PropertyDesc,
            pdfBytes);

        return (pdfBytes, fileName);
    }

    public async Task<(byte[] Pdf, string FileName)>
        GenerateSection49ForObjectionAsync(
            string rollSource,
            string unitKey,
            string valuationKey,
            string objectionNo,
            string propertyDescription)
    {
        if (!IsSupportedSection49Roll(rollSource))
        {
            throw new InvalidOperationException(
                $"Section 49 is not available for roll source '{rollSource}'.");
        }

        if (string.IsNullOrWhiteSpace(objectionNo))
            throw new ArgumentException(
                "Objection number is required.",
                nameof(objectionNo));

        unitKey = FloatKeyHelper.Normalize(unitKey);
        valuationKey = FloatKeyHelper.Normalize(valuationKey);

        if (string.IsNullOrWhiteSpace(unitKey))
            throw new ArgumentException(
                "A valid unit key is required.",
                nameof(unitKey));

        if (string.IsNullOrWhiteSpace(valuationKey))
            throw new ArgumentException(
                "A valid valuation key is required.",
                nameof(valuationKey));

        var items = await _search.GetPropertyDetailsAsync(
            rollSource,
            unitKey,
            valuationKey);

        if (items is null || items.Count == 0)
        {
            throw new InvalidOperationException(
                $"No roll property found for UnitKey={unitKey}, ValuationKey={valuationKey}.");
        }

        var roll = _noticeSettings.For(rollSource);
        var dates = _rollDates.For(rollSource);
        var main = items.First();

        var finalPropertyDescription =
            string.IsNullOrWhiteSpace(propertyDescription)
                ? main.PropertyDesc ?? unitKey
                : propertyDescription.Trim();

        var fileName =
            $"{SanitiseName(objectionNo)}_" +
            $"{SanitiseName(finalPropertyDescription)}_Section49.pdf";

        var pdfBytes =
            GenerateSection49Pdf(
                items,
                main,
                roll,
                dates,
                rollSource);

        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            throw new InvalidOperationException(
                $"Section 49 PDF is empty for {objectionNo}.");
        }

        _logger.LogInformation(
            "[Section49 Submission] Generated {FileName} for {ObjectionNo}. " +
            "Roll={RollSource}, UnitKey={UnitKey}, ValuationKey={ValuationKey}",
            fileName,
            objectionNo,
            rollSource,
            unitKey,
            valuationKey);

        return (pdfBytes, fileName);
    }

    private static bool IsSupportedSection49Roll(string? rollSource)
    {
        return rollSource?.Trim() switch
        {
            "Objection" => true,
            "Objection_Supp1" => true,
            "Objection_Supp2" => true,
            "Objection_Supp3" => true,
            "Objection_Supp4" => true,
            _ => false
        };
    }

    // ── Current official one-page Section 49 PDF ────────────────────────────────
    private byte[] GenerateSection49Pdf(
        List<PropertyDetailResult> items,
        PropertyDetailResult main,
        NoticeRollEntry roll,
        RollDateEntry dates,
        string rollSource)
    {
        var headerPath =
            Path.Combine(
                _env.WebRootPath,
                HEADER_IMAGE);

        var signaturePath =
            ResolveSection49SignaturePath(
                rollSource,
                roll);

        static string Safe(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();

        var culture =
            System.Globalization.CultureInfo
                .GetCultureInfo("en-ZA");

        var title12 =
            TextStyle.Default
                .FontFamily("Arial")
                .FontSize(12)
                .SemiBold();

        var sub10b =
            TextStyle.Default
                .FontFamily("Arial")
                .FontSize(10)
                .SemiBold();

        var body9 =
            TextStyle.Default
                .FontFamily("Arial")
                .FontSize(9);

        var body9b =
            TextStyle.Default
                .FontFamily("Arial")
                .FontSize(9)
                .SemiBold();

        var small7 =
            TextStyle.Default
                .FontFamily("Arial")
                .FontSize(7)
                .FontColor(Colors.Grey.Darken2);

        // Genesis VisibleUntil already represents the final configured closing date
        // for the selected roll, including an extension where applicable.
        var closingDate =
            dates.VisibleUntil;

        var inspectionWindowText =
            $"{dates.OpenDate:dd MMMM yyyy} – " +
            $"{closingDate:dd MMMM yyyy} until 15:00";

        var rollDisplayName =
            Safe(roll.RollTitle).ToUpperInvariant();

        var rollDisplayTitle =
            Safe(roll.RollTitle);

        var recipient =
            Safe(main.ADDR1);

        var greeting =
            string.IsNullOrWhiteSpace(recipient)
                ? "Dear Property Owner"
                : $"Dear: {recipient}";

        // The current notice application caters for compact split/multipurpose
        // notices. Keep the Section 49 table to a maximum of four rows so the
        // official notice remains on one A4 page.
        var noticeRows =
            items
                .Take(4)
                .ToList();

        if (noticeRows.Count == 0)
            noticeRows.Add(main);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);

                // Match the current GV23_Notice compact layout.
                page.MarginLeft(30);
                page.MarginRight(30);
                page.MarginTop(10);
                page.MarginBottom(10);

                page.DefaultTextStyle(
                    x => x
                        .FontFamily("Arial")
                        .FontSize(9));

                page.Footer()
                    .PaddingTop(5)
                    .AlignCenter()
                    .Text(t =>
                    {
                        t.Line(
                                "_______________________________________________")
                            .Style(small7);

                        t.Line(
                                "This is an official document generated by the City of Johannesburg Valuation Services Department")
                            .Style(small7);

                        t.Line(
                                $"Generated on: {DateTime.Now:dd MMMM yyyy}")
                            .Style(small7);

                        if (!string.IsNullOrWhiteSpace(main.ValuationKey))
                        {
                            t.Line(Safe(main.ValuationKey))
                                .FontFamily("Arial")
                                .FontSize(7)
                                .SemiBold()
                                .FontColor(Colors.Red.Medium);
                        }
                    });

                page.Content()
                    .Column(col =>
                    {
                        col.Spacing(4);

                        // ── City header ────────────────────────────────────
                        if (File.Exists(headerPath))
                        {
                            col.Item()
                                .Image(
                                    headerPath,
                                    ImageScaling.FitWidth);
                        }

                        // ── Owner postal address + notice date ─────────────
                        col.Item()
                            .PaddingTop(3)
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .Text(t =>
                                    {
                                        foreach (var line in new[]
                                        {
                                            main.ADDR1,
                                            main.ADDR2,
                                            main.ADDR3,
                                            main.ADDR4,
                                            main.ADDR5
                                        }.Where(x =>
                                            !string.IsNullOrWhiteSpace(x)))
                                        {
                                            t.Span(
                                                    Safe(line) +
                                                    Environment.NewLine)
                                                .Style(sub10b);
                                        }
                                    });

                                row.ConstantItem(150)
                                    .AlignRight()
                                    .Text(
                                        DateTime.Now.ToString(
                                            "dd MMMM yyyy",
                                            culture))
                                    .Style(body9);
                            });

                        // ── Official title ─────────────────────────────────
                        col.Item()
                            .AlignCenter()
                            .Text("CITY OF JOHANNESBURG")
                            .Style(title12);

                        col.Item()
                            .AlignCenter()
                            .Text(
                                $"PUBLIC NOTICE CALLING FOR INSPECTION OF THE " +
                                $"{rollDisplayName} AND LODGING OF OBJECTIONS")
                            .Style(sub10b);

                        col.Item()
                            .LineHorizontal(1.5f)
                            .LineColor(Colors.Grey.Darken2);

                        col.Item()
                            .PaddingTop(2)
                            .Text(greeting)
                            .Style(body9b);

                        // ── Current legal wording from GV23_Notice ─────────
                        col.Item()
                            .Text(t =>
                            {
                                t.Span(
                                        "Notice is hereby given in terms of Section 49(1)(a)(i) " +
                                        "read together with section 78(2) of the ")
                                    .Style(body9);

                                t.Span(
                                        "Local Government: Municipal Property Rates Act No. 6 of 2004")
                                    .Style(body9b);

                                t.Span(
                                        $" as amended hereinafter referred to as the \"Act\", " +
                                        $"that the {rollDisplayTitle} for the financial years ")
                                    .Style(body9);

                                t.Span(
                                        Safe(roll.FinancialYears))
                                    .Style(body9b);

                                t.Span(
                                        " is open for public inspection at the centre listed below, from ")
                                    .Style(body9);

                                t.Span(
                                        inspectionWindowText)
                                    .Style(body9b);

                                t.Span(
                                        ". In addition, the valuation roll is available on the City's website ")
                                    .Style(body9);

                                t.Span(
                                        "www.joburg.org.za")
                                    .Style(body9b);

                                t.Span(
                                        ", under the GVR Online tile on the home page.")
                                    .Style(body9);
                            });

                        if (!string.IsNullOrWhiteSpace(
                                dates.ExtendedPeriodText))
                        {
                            col.Item()
                                .Text(
                                    dates.ExtendedPeriodText.Trim())
                                .Style(body9b);
                        }

                        col.Item()
                            .Text(
                                $"An invitation is hereby made in terms of section 49(1)(a)(ii) " +
                                $"read together with section 78(2) of the Act to any owner of property " +
                                $"or other person who so desires that may wish to lodge an objection " +
                                $"with the Municipal Manager in respect of any matter reflected in, " +
                                $"or omitted from, the {rollDisplayTitle}. The objection must be " +
                                $"submitted within the above mentioned inspection period.")
                            .Style(body9)
                            .Justify();

                        col.Item()
                            .Text(t =>
                            {
                                t.Span(
                                        "Attention is specifically drawn to the fact that in terms " +
                                        "of section 50(2) of the Act an objection must be in relation to a ")
                                    .Style(body9);

                                t.Span(
                                        "specific individual property")
                                    .Style(body9b);

                                t.Span(
                                        $" and not against the {rollDisplayTitle} as such. " +
                                        "The lodging of objections in terms of Chapter 4(d) of the " +
                                        "Regulations to the Act can be done at the centre listed below " +
                                        "or preferably online at ")
                                    .Style(body9);

                                t.Span(
                                        "www.joburg.org.za")
                                    .Style(body9b);

                                t.Span(
                                        ", under the GVR Online tile on the home page.")
                                    .Style(body9);
                            });

                        col.Item()
                            .Text(
                                "The completed forms could be returned to the following address " +
                                "or preferably submitted online on the online objection system.")
                            .Style(body9);

                        // ── Administration address ────────────────────────
                        col.Item()
                            .Background(
                                Color.FromRGB(
                                    240,
                                    240,
                                    240))
                            .Padding(4)
                            .Column(box =>
                            {
                                box.Item()
                                    .Text(
                                        "Valuation Services: Administration")
                                    .Style(body9b);

                                box.Item()
                                    .Text(
                                        "Jorissen Place, 66 Jorissen Street, Braamfontein, " +
                                        "East Wing, 1st Floor")
                                    .Style(body9);
                            });

                        col.Item()
                            .Text(
                                "The acknowledgement letter will be generated by the online system " +
                                "and should be kept as proof that the objection was submitted.")
                            .Style(body9);

                        // IMPORTANT:
                        // No PageBreak here. Property details stay on page 1.

                        col.Item()
                            .LineHorizontal(1.5f)
                            .LineColor(Colors.Grey.Darken2);

                        col.Item()
                            .AlignCenter()
                            .Text(
                                $"PROPERTY DETAILS AS LISTED IN {rollDisplayName}")
                            .Style(sub10b);

                        // ── Blue property detail box ──────────────────────
                        col.Item()
                            .Background(
                                Color.FromRGB(
                                    245,
                                    250,
                                    255))
                            .Border(1)
                            .BorderColor(
                                Color.FromRGB(
                                    70,
                                    130,
                                    180))
                            .Padding(5)
                            .Column(box =>
                            {
                                box.Item()
                                    .Text(t =>
                                    {
                                        t.Span(
                                                "Property Description: ")
                                            .Style(body9b);

                                        t.Span(
                                                Safe(main.PropertyDesc))
                                            .Style(body9);
                                    });

                                box.Item()
                                    .Text(t =>
                                    {
                                        t.Span(
                                                "Physical Address: ")
                                            .Style(body9b);

                                        t.Span(
                                                Safe(main.LisStreetAddress))
                                            .Style(body9);
                                    });
                            });

                        // ── Current blue Section 49 valuation table ───────
                        col.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(75);
                                    columns.ConstantColumn(105);
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    void HeaderCell(string text)
                                    {
                                        header.Cell()
                                            .Border(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten1)
                                            .Background(
                                                Color.FromRGB(
                                                    70,
                                                    130,
                                                    180))
                                            .PaddingVertical(3)
                                            .PaddingHorizontal(5)
                                            .Text(text)
                                            .FontFamily("Arial")
                                            .FontSize(8)
                                            .SemiBold()
                                            .FontColor(
                                                Colors.White);
                                    }

                                    HeaderCell(
                                        "Property Category");

                                    HeaderCell(
                                        "Area/m²");

                                    HeaderCell(
                                        "Market Value");

                                    HeaderCell(
                                        "With Effective Date");
                                });

                                void DataCell(
                                    string? text,
                                    bool right = false,
                                    bool center = false)
                                {
                                    var cell =
                                        table.Cell()
                                            .Border(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten1)
                                            .PaddingVertical(3)
                                            .PaddingHorizontal(5);

                                    if (right)
                                        cell = cell.AlignRight();
                                    else if (center)
                                        cell = cell.AlignCenter();

                                    cell.Text(
                                            string.IsNullOrWhiteSpace(text)
                                                ? "-"
                                                : text.Trim())
                                        .FontFamily("Arial")
                                        .FontSize(8);
                                }

                                foreach (var item in noticeRows)
                                {
                                    DataCell(
                                        item.CatDesc);

                                    DataCell(
                                        item.RateableArea,
                                        right: true);

                                    DataCell(
                                        FormatZAR(
                                            item.MarketValue),
                                        right: true);

                                    DataCell(
                                        item.WefDate,
                                        center: true);
                                }
                            });

                        // ── Closing date ──────────────────────────────────
                        col.Item()
                            .PaddingTop(4)
                            .AlignCenter()
                            .Text(
                                $"⚠ CLOSING DATE FOR OBJECTIONS IS 15:00 ON " +
                                $"{closingDate:dd MMMM yyyy}"
                                    .ToUpperInvariant())
                            .Style(body9b);

                        // ── Roll-specific official signature ─────────────
                        if (!string.IsNullOrWhiteSpace(
                                signaturePath) &&
                            File.Exists(signaturePath))
                        {
                            col.Item()
                                .PaddingTop(3)
                                .AlignLeft()
                                .Width(185)
                                .Height(55)
                                .Image(
                                    signaturePath,
                                    ImageScaling.FitArea);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[Section49] Signature image was not found for roll {RollSource}. " +
                                "ResolvedPath={SignaturePath}",
                                rollSource,
                                signaturePath);
                        }
                    });
            });
        }).GeneratePdf();
    }

    // ── Roll-specific signature resolver ───────────────────────────────────────
    // This deliberately resolves from wwwroot/Images by roll so an outdated
    // appsettings SignatureFile cannot cause the wrong official to sign a notice.
    private string ResolveSection49SignaturePath(
        string rollSource,
        NoticeRollEntry roll)
    {
        var imageFolder =
            Path.Combine(
                _env.WebRootPath,
                "Images");

        var expectedNames =
            rollSource?.Trim() switch
            {
                "Objection" =>
                    new[]
                    {
                        "Signature_Gv23.PNG",
                        "Signature_Gv23.png",
                        "Signature_GV23.PNG",
                        "Signature_GV23.png"
                    },

                "Objection_Supp1" =>
                    new[]
                    {
                        "Signature_Sup1.PNG",
                        "Signature_Sup1.png"
                    },

                "Objection_Supp2" =>
                    new[]
                    {
                        "Signature_Sup2.PNG",
                        "Signature_Sup2.png"
                    },

                "Objection_Supp3" =>
                    new[]
                    {
                        "Signature_Sup3.PNG",
                        "Signature_Sup3.png"
                    },

                "Objection_Supp4" =>
                    new[]
                    {
                        "Signature_Sup4.PNG",
                        "Signature_Sup4.png"
                    },

                _ =>
                    Array.Empty<string>()
            };

        foreach (var fileName in expectedNames)
        {
            var candidate =
                Path.Combine(
                    imageFolder,
                    fileName);

            if (File.Exists(candidate))
                return candidate;
        }

        // Compatibility fallback only.
        // New GV23-Sup4 notices should normally resolve above.
        if (!string.IsNullOrWhiteSpace(
                roll.SignatureFile))
        {
            var configured =
                Path.Combine(
                    _env.ContentRootPath,
                    roll.SignatureFile);

            if (File.Exists(configured))
                return configured;
        }

        // Final legacy fallback.
        foreach (var legacyName in new[]
        {
            "signature.png",
            "Signature.png",
            "Signature.PNG"
        })
        {
            var legacy =
                Path.Combine(
                    imageFolder,
                    legacyName);

            if (File.Exists(legacy))
                return legacy;
        }

        return string.Empty;
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

    private static string SanitiseName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Property";

        var safe = string.Concat(name.Split(Path.GetInvalidFileNameChars()))
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace(":", "_")
            .Replace("*", "_")
            .Replace("?", "_")
            .Replace("\"", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace("|", "_")
            .Trim();

        while (safe.Contains("__"))
            safe = safe.Replace("__", "_");

        if (string.IsNullOrWhiteSpace(safe))
            safe = "Property";

        return safe.Length > 90 ? safe[..90] : safe;
    }
    private static string BuildAcknowledgementFileName(AcknowledgementData data)
    {
        var referenceNo = SanitiseName(data.ObjectionRef);
        var propertyDesc = SanitiseName(data.Old_PropertyDescription);
        var datePart = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        return $"{referenceNo}_{propertyDesc}_Acknowledgement_{datePart}.pdf";
    }
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
        var fileName = BuildAcknowledgementFileName(data);

        var pdfBytes = BuildAcknowledgementPdf(data, roll, dates);

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
        var hasHeader = File.Exists(headerPath);

        var now = DateTime.Now;
        var letterDate = now.ToString("dd MMMM yyyy");
        var generatedDate = now.ToString("dd MMMM yyyy HH:mm");
        var evidenceCloseDate = ResolveSubmissionDate(data.SubmissionTime, now).AddHours(48);

        var sourceType = ResolveAcknowledgementSource(data.PropertyFrom);
        var isLis = sourceType == AcknowledgementSource.Lis;
        var isOmission = sourceType == AcknowledgementSource.Omission;
        var rollTitle = string.IsNullOrWhiteSpace(roll.RollTitle)
            ? data.RollSource
            : roll.RollTitle.Trim();

        string actionWord = data.IsAppeal ? "appeal" : "objection";
        string actionWordUpper = data.IsAppeal ? "APPEAL" : "OBJECTION";

        string titleLabel =
            data.IsAppeal
                ? data.IsMulti
                    ? "MULTIPURPOSE APPEAL ACKNOWLEDGEMENT"
                    : "APPEAL ACKNOWLEDGEMENT"
                : data.IsMulti
                    ? "MULTIPURPOSE OBJECTION ACKNOWLEDGEMENT"
                    : "OBJECTION ACKNOWLEDGEMENT";

        string referenceLabel = data.IsAppeal
            ? "Appeal Number:"
            : "Objection Number:";

        string listedTitle = isLis
            ? "PROPERTY DETAILS AS LISTED IN LIS"
            : $"PROPERTY DETAILS AS LISTED IN {rollTitle.ToUpperInvariant()}";

        string sourceSummary = sourceType switch
        {
            AcknowledgementSource.Lis =>
                $"LIS IN {rollTitle.ToUpperInvariant()}",
            AcknowledgementSource.Omission =>
                $"OMISSION IN {rollTitle.ToUpperInvariant()}",
            _ when dates is not null =>
                $"{rollTitle.ToUpperInvariant()}\n{actionWordUpper} PERIOD: " +
                $"{dates.OpenDate:dd MMMM yyyy} AT 08:00 - " +
                $"{dates.VisibleUntil:dd MMMM yyyy} AT 15:00",
            _ => rollTitle.ToUpperInvariant()
        };

        string requestedTitle = data.IsAppeal
            ? "PROPERTY DETAILS AS APPEALED"
            : "PROPERTY DETAILS AS OBJECTED";

        string reasonTitle = data.IsAppeal
            ? "REASONS FOR APPEAL"
            : "REASONS FOR OBJECTION";

        string requiredDocsTitle = data.IsAppeal
            ? "REQUIRED DOCUMENTATION FOR APPEAL"
            : "REQUIRED DOCUMENTATION FOR OBJECTION";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    // HEADER IMAGE
                    if (hasHeader)
                        //col.Item().Height(80).Image(headerPath, ImageScaling.FitArea);
                        col.Item().AlignCenter().Width(500).Height(100).Image(headerPath, ImageScaling.FitArea);
                    // DATE
                    //col.Item().AlignRight()
                    //    .Text(letterDate)
                    //    .FontSize(10)
                    //    .SemiBold();

                    // TITLE
                    col.Item().AlignCenter()
                        .Text("CITY OF JOHANNESBURG")
                        .FontSize(13)
                        .Bold();

                    col.Item().AlignCenter()
                        .Text(titleLabel)
                        .FontSize(12)
                        .Bold();

                    col.Item().BorderBottom(1).BorderColor("#555555");

                    // INTRO
                    col.Item().Text(
                        $"This is to acknowledge that your {actionWord} has been successfully received and logged. Details below for your records.")
                        .FontSize(9);

                    col.Item().Text(
                        "IMPORTANT NOTICE: You have 48 hours from submission to upload outstanding evidence. " +
                        $"Evidence upload closes: {evidenceCloseDate:dd MMMM yyyy 'at' HH:mm}.")
                        .FontSize(9)
                        .Bold();

                    // REFERENCE DETAILS
                    col.Item().AlignCenter()
                        .Text("REFERENCE DETAILS")
                        .FontSize(10)
                        .Bold();

                    col.Item()
                        .Background("#eeeeee")
                        .Border(1)
                        .BorderColor("#444444")
                        .Padding(10)
                        .Column(refBox =>
                        {
                            RefRow(refBox, "Property Description:", data.Old_PropertyDescription);
                            RefRow(refBox, referenceLabel, data.ObjectionRef);
                            RefRow(refBox, "PIN:", data.ObjectionNo);
                            RefRow(refBox, "Date Captured:", data.SubmissionTime);
                        });

                    col.Item().BorderBottom(1).BorderColor("#555555");

                    // PROPERTY DETAILS AS LISTED
                    col.Item().AlignCenter()
                        .Text(listedTitle)
                        .FontSize(10)
                        .Bold();

                    PropertyTable(
                        col,
                        isOmission ? null : data.Old_PropertyDescription,
                        isOmission ? null : data.Old_Category,
                        isOmission ? null : data.Old_Address,
                        isOmission ? null : FormatAcknowledgementMarketValue(data.Old_MarketValue),
                        isOmission ? null : data.Old_Extent,
                        isOmission ? null : data.Old_Owner,
                        !isOmission && data.IsMulti,
                        isOmission ? null : data.Old2_Category,
                        isOmission ? null : FormatAcknowledgementMarketValue(data.Old2_MarketValue),
                        isOmission ? null : data.Old2_Extent,
                        isOmission ? null : data.Old3_Category,
                        isOmission ? null : FormatAcknowledgementMarketValue(data.Old3_MarketValue),
                        isOmission ? null : data.Old3_Extent);

                    col.Item().BorderBottom(1).BorderColor("#555555");

                    // PROPERTY DETAILS AS OBJECTED/APPEALED
                    col.Item().AlignCenter()
                        .Text(requestedTitle)
                        .FontSize(10)
                        .Bold();

                    PropertyTable(
                        col,
                        data.New_PropertyDescription,
                        data.New_Category,
                        data.New_Address,
                        FormatAcknowledgementMarketValue(data.New_MarketValue),
                        data.New_Extent,
                        data.New_Owner,
                        data.IsMulti,
                        data.New2_Category,
                        FormatAcknowledgementMarketValue(data.New2_MarketValue),
                        data.New2_Extent,
                        data.New3_Category,
                        FormatAcknowledgementMarketValue(data.New3_MarketValue),
                        data.New3_Extent);

                    // REASONS
                    if (!string.IsNullOrWhiteSpace(data.ObjectionReason))
                    {
                        col.Item().BorderBottom(1).BorderColor("#555555");

                        col.Item().AlignCenter()
                            .Text(reasonTitle)
                            .FontSize(10)
                            .Bold();

                        col.Item()
                            .Background("#eaf4fb")
                            .Border(1)
                            .BorderColor("#444444")
                            .Padding(8)
                            .Text(data.ObjectionReason)
                            .FontSize(8);
                    }

                    // REQUIRED DOCUMENTATION
                    col.Item().BorderBottom(1).BorderColor("#555555");

                    col.Item().AlignCenter()
                        .Text(requiredDocsTitle)
                        .FontSize(10)
                        .Bold();

                    col.Item()
                        .Text($"You have uploaded {data.FileCount} Document(s)")
                        .FontSize(9);

                    var docs = data.UploadedDocumentNames ?? new List<string>();



                    col.Item().Element(e => SupportingDocumentsBlock(e, docs));
                    // SOURCE / ROLL PERIOD
                    col.Item()
                        .Background("#FFF5E6")
                        .Border(1)
                        .BorderColor("#FF8C00")
                        .Padding(6)
                        .AlignCenter()
                        .Text(sourceSummary)
                        .Bold()
                        .FontSize(8);

                    // SIGNATURE
                    //if (File.Exists(signaturePath))
                    //{
                    //    col.Item().Height(8);
                    //    col.Item().Width(150).Image(signaturePath);
                    //}
                });

                // FOOTER - exactly like the letter style
                page.Footer()
                    .PaddingTop(5)
                    .AlignCenter()
                    .Column(f =>
                    {
                        f.Item()
                            .Text("This is an official document generated by the City of Johannesburg")
                            .FontSize(7)
                            .FontColor("#666666");

                        f.Item()
                            .Text($"Generated on: {generatedDate}")
                            .FontSize(7)
                            .FontColor("#666666");

                        if (!isOmission && !string.IsNullOrWhiteSpace(data.ValuationKey))
                        {
                            f.Item()
                                .Text(data.ValuationKey)
                                .FontSize(8)
                                .SemiBold()
                                .FontColor("#cc0000");
                        }
                    });
            });
        }).GeneratePdf();


        static DateTime ResolveSubmissionDate(string? value, DateTime fallback)
        {
            if (DateTime.TryParse(value, out var parsed))
                return parsed;

            return fallback;
        }

        static string? FormatAcknowledgementMarketValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : FormatZAR(value);

        static AcknowledgementSource ResolveAcknowledgementSource(string? value)
        {
            var source = value?.Trim() ?? string.Empty;

            if (source.Contains("LIS", StringComparison.OrdinalIgnoreCase))
                return AcknowledgementSource.Lis;

            if (source.Contains("Omission", StringComparison.OrdinalIgnoreCase) ||
                source.Contains("Omitted", StringComparison.OrdinalIgnoreCase))
                return AcknowledgementSource.Omission;

            return AcknowledgementSource.Roll;
        }


        static void RefRow(ColumnDescriptor col, string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                value = "—";

            col.Item().Text(t =>
            {
                t.Span(label + " ").Bold();
                t.Span(value);
            });
        }

        static void PropertyTable(
            ColumnDescriptor col,
            string? propertyDesc,
            string? category,
            string? physicalAddress,
            string? marketValue,
            string? extent,
            string? owner,
            bool isMulti,
            string? category2,
            string? marketValue2,
            string? extent2,
            string? category3,
            string? marketValue3,
            string? extent3)
        {
            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.2f);
                    c.RelativeColumn(1.6f);
                    c.RelativeColumn(1.7f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(0.8f);
                    c.RelativeColumn(1.1f);
                });

                TH(t, "Property Description");
                TH(t, "Category");
                TH(t, "Physical Address");
                TH(t, "Market Value");
                TH(t, "Extent");
                TH(t, "Owner");

                TD(t, propertyDesc);
                TD(t, category);
                TD(t, physicalAddress);
                TD(t, marketValue);
                TD(t, extent);
                TD(t, owner);

                if (isMulti && HasAny(category2, marketValue2, extent2))
                {
                    TD(t, "");
                    TD(t, category2);
                    TD(t, "");
                    TD(t, marketValue2);
                    TD(t, extent2);
                    TD(t, "");
                }

                if (isMulti && HasAny(category3, marketValue3, extent3))
                {
                    TD(t, "");
                    TD(t, category3);
                    TD(t, "");
                    TD(t, marketValue3);
                    TD(t, extent3);
                    TD(t, "");
                }
            });
        }

        static bool HasAny(params string?[] values)
            => values.Any(v => !string.IsNullOrWhiteSpace(v));

        static void TH(TableDescriptor t, string text)
        {
            t.Cell()
                .Background("#3f7fb5")
                .Border(1)
                .BorderColor("#222222")
                .Padding(5)
                .Text(text)
                .FontSize(8)
                .FontColor(Colors.White)
                .Bold()
                .AlignCenter();
        }

        static void TD(TableDescriptor t, string? text)
        {
            t.Cell()
                .Border(1)
                .BorderColor("#222222")
                .Padding(5)
                .Text(string.IsNullOrWhiteSpace(text) ? "" : text)
                .FontSize(8);
        }
    }

    private enum AcknowledgementSource
    {
        Roll,
        Lis,
        Omission
    }
    // ── Helper: renders one section's comparison table ────────────────────
    private static void SupportingDocumentsBlock(IContainer container, List<string> docs)
    {
        docs ??= new List<string>();

        var left = docs.Take(5).ToList();
        var right = docs.Skip(5).Take(5).ToList();

        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1);
                cols.RelativeColumn(1);
            });

            t.Cell()
                .Border(1)
                .BorderColor("#444444")
                .Background("#eaf4fb")
                .Padding(8)
                .Column(c =>
                {
                    c.Item()
                        .Text("Uploaded Documents (1–5):")
                        .FontSize(8)
                        .Bold();

                    c.Item().Height(4);

                    if (left.Any())
                    {
                        foreach (var d in left)
                        {
                            c.Item()
                                .Text("• " + d)
                                .FontSize(8);
                        }
                    }
                    else
                    {
                        c.Item()
                            .Text("No documents uploaded.")
                            .FontSize(8)
                            .Italic()
                            .FontColor("#666666");
                    }
                });

            t.Cell()
                .Border(1)
                .BorderColor("#444444")
                .Background("#eaf4fb")
                .Padding(8)
                .Column(c =>
                {
                    c.Item()
                        .Text("Uploaded Documents (6–10):")
                        .FontSize(8)
                        .Bold();

                    c.Item().Height(4);

                    if (right.Any())
                    {
                        foreach (var d in right)
                        {
                            c.Item()
                                .Text("• " + d)
                                .FontSize(8);
                        }
                    }
                    else
                    {
                        c.Item()
                            .Text("")
                            .FontSize(8);
                    }
                });
        });
    }
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
     NoticeRollEntry roll,
     AcknowledgementData data,
     byte[] pdf,
     string fileName)
    {
        try
        {
            var safeRef = SanitiseName(data.ObjectionRef);

            var dir = Path.Combine(
                roll.Section49Path.Replace("Section49", "Acknowledgements"),
                safeRef);

            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, fileName);

            await File.WriteAllBytesAsync(path, pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save acknowledgement to disk for reference: {Ref}",
                data.ObjectionRef);
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
    public Task<(byte[] Pdf, string FileName)> GenerateSection51AcknowledgementAsync(
    string objectionNo, string rollSource,
    int fileCount, List<string> fileNames)
    {
        var roll = _noticeSettings.For(rollSource);
        var header = Path.Combine(_env.WebRootPath, HEADER_IMAGE);
        var fileName = $"Section51_{SanitiseName(objectionNo)}.pdf";

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                page.Content().Column(col =>
                {
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
                        .Text("SECTION 51 ACKNOWLEDGEMENT").FontSize(10).Bold();
                    col.Item().Height(4);
                    col.Item().AlignCenter()
                        .Text("DOCUMENT UPLOAD CONFIRMATION").FontSize(9).Bold();
                    col.Item().Height(8);
                    col.Item().BorderBottom(1).BorderColor("#555555");
                    col.Item().Height(8);

                    // Success line
                    col.Item().AlignCenter().PaddingBottom(10)
                        .Text("✓ Your documents have been successfully uploaded.")
                        .FontSize(10).Bold().FontColor("#166534");

                    // Details box
                    col.Item().Background("#F0F7FF").Border(1.5f).BorderColor("#4682B4")
                        .Padding(10).Column(box =>
                        {
                            box.Item().Text(t => {
                                t.Span("Objection Number: ").Bold();
                                t.Span(objectionNo);
                            });
                            box.Item().Height(3);
                            box.Item().Text(t => {
                                t.Span("Roll: ").Bold();
                                t.Span(roll.RollTitle);
                            });
                            box.Item().Height(3);
                            box.Item().Text(t => {
                                t.Span("Documents Uploaded: ").Bold();
                                t.Span($"{fileCount} document(s)");
                            });
                            box.Item().Height(3);
                            box.Item().Text(t => {
                                t.Span("Upload Date/Time: ").Bold();
                                t.Span(DateTime.Now.ToString("dd MMMM yyyy HH:mm"));
                            });
                        });

                    col.Item().Height(10);

                    // File list
                    col.Item().AlignCenter()
                        .Text("UPLOADED DOCUMENTS").Bold().FontSize(9);
                    col.Item().Height(6);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(30);
                            c.RelativeColumn();
                        });

                        static IContainer TH(IContainer c) =>
                            c.Background("#4682B4").Padding(6);

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
                            table.Cell().Element(TD).Text((i + 1).ToString()).FontSize(8);
                            table.Cell().Element(TD).Text(fileNames[i]).FontSize(8);
                        }
                    });

                    col.Item().Height(12);

                    // Warning
                    col.Item().Background("#FFF5E6").Border(1.5f).BorderColor("#FF8C00")
                        .Padding(8).Column(w =>
                        {
                            w.Item().Text("⚠ IMPORTANT NOTE").Bold().FontSize(9)
                            .FontColor("#8B4500");
                            w.Item().Height(4);
                            w.Item().Text(
                            "Please keep this acknowledgement as proof of your Section 51 " +
                            "document submission.")
                            .FontSize(8);
                        });

                    col.Item().Height(10);
                    col.Item()
                          .DefaultTextStyle(x => x.FontSize(8))
                          .Text(t =>
                          {
                              t.Span("For enquiries: ").Bold();
                              t.Span("Tel. 011 407-6622  |  valuationenquiries@joburg.org.za");
                          });

                    col.Item().Height(14);
                    col.Item().BorderTop(1).BorderColor("#AAAAAA").PaddingTop(6)
                        .AlignCenter().Column(f =>
                        {
                            f.Item().Text("Official document — City of Johannesburg Valuation Services")
                            .FontSize(7).FontColor("#666666");
                            f.Item().Text($"Generated: {DateTime.Now:dd MMMM yyyy HH:mm}")
                            .FontSize(7).FontColor("#666666");
                        });
                });
            });
        }).GeneratePdf();

        // Save to disk
        _ = SaveSection51ToDiskAsync(rollSource, objectionNo, pdf);

        return Task.FromResult((pdf, fileName));
    }

    private async Task SaveSection51ToDiskAsync(
        string rollSource, string objectionNo, byte[] pdf)
    {
        try
        {
            var path = _config[$"Section51Rolls:{rollSource}:FileRootPath"];
            if (string.IsNullOrEmpty(path)) return;
            var dir = Path.Combine(path, SanitiseName(objectionNo));
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(
                Path.Combine(dir, $"Section51_{SanitiseName(objectionNo)}.pdf"), pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Section51] Save PDF failed for {ObjNo}", objectionNo);
        }
    }
    // ════════════════════════════════════════════════════════════════
    public async Task<NoticesDashboardViewModel> GetNoticesDashboardAsync(
        string userId, string displayName)
    {
        var vm = new NoticesDashboardViewModel { DisplayName = displayName };

        await Task.WhenAll(
            LoadObjectionNoticesAsync(userId, vm),
            LoadAppealNoticesAsync(userId, vm),
            LoadQueryNoticesAsync(userId, vm)
        );

        // Build calendar from objection notices that have appeal dates
        vm.CalendarEvents = vm.ObjectionNotices
            .Where(n => n.AppealOpenDate.HasValue && n.AppealCloseDate.HasValue)
            .Select(n => new AppealCalendarEvent
            {
                ObjectionNo = n.ReferenceNo,
                PropertyDesc = n.PropertyDesc,
                RollName = n.RollName,
                OpenDate = n.AppealOpenDate!.Value,
                CloseDate = n.AppealCloseDate!.Value,
            })
            // Deduplicate — one calendar event per objection
            .GroupBy(e => e.ObjectionNo)
            .Select(g => g.First())
            .OrderBy(e => e.CloseDate)
            .ToList();

        return vm;
    }

    // ── Objection notices (all rolls) ─────────────────────────────────
    private async Task LoadObjectionNoticesAsync(
        string userId, NoticesDashboardViewModel vm)
    {
        foreach (var roll in Rolls)
        {
            try
            {
                var connStr = _config.GetConnectionString(roll.ConnKey)!;
                await using var conn = new SqlConnection(connStr);

                var objections = await conn.QueryAsync(
                    @"SELECT Objection_No, Objection_Status, Property_Desc
                      FROM   dbo.Obj_Property_Info
                      WHERE  UserID = @UserId",
                    new { UserId = userId });

                foreach (var obj in objections)
                {
                    var objNo = obj.Objection_No?.ToString() ?? "";
                    var status = obj.Objection_Status?.ToString() ?? "";
                    var propDesc = obj.Property_Desc?.ToString() ?? "";
                    var objRoot = Path.Combine(roll.RootPath, objNo);

                    // ── Section 49 (by property desc, separate root) ────
                    var s49 = FindNoticeFile(Section49Root, propDesc);
                    if (s49.exists)
                        vm.ObjectionNotices.Add(Notice(
                            objNo, propDesc, roll.Name,
                            NoticeType.Section49, "Section 49 – Invitation to Object",
                            s49.path, s49.ext, null, null, null));

                    // ── Section 51 ──────────────────────────────────────
                    var s51 = FindNoticeFile(objRoot, "Section51");
                    if (s51.exists)
                        vm.ObjectionNotices.Add(Notice(
                            objNo, propDesc, roll.Name,
                            NoticeType.Section51, "Section 51 – Third Party Notice",
                            s51.path, s51.ext, null, null, null));

                    // ── Section 53 + appeal dates (status = Notice-Sent) ─
                    if (status.Equals("Notice-Sent",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var s53 = FindNoticeFile(objRoot, "Section53");
                        // get appeal dates from Objection_MVD
                        DateTime? openDate = null;
                        DateTime? closeDate = null;
                        try
                        {
                            var mvd = await conn.QueryFirstOrDefaultAsync(
                                @"SELECT Appeal_Start_Date, Appeal_Close_Date
                                  FROM   dbo.Objection_MVD
                                  WHERE  Objection_No = @ObjNo",
                                new { ObjNo = objNo });
                            if (mvd != null)
                            {
                                openDate = mvd.Appeal_Start_Date;
                                closeDate = mvd.Appeal_Close_Date;
                            }
                        }
                        catch { /* MVD may not exist on supp roll DBs */ }

                        if (s53.exists || openDate.HasValue)
                            vm.ObjectionNotices.Add(Notice(
                                objNo, propDesc, roll.Name,
                                NoticeType.Section53,
                                "Section 53 – Valuer Decision (MVD)",
                                s53.path, s53.ext,
                                null, openDate, closeDate));
                    }

                    // ── Section 52 Review (.eml) ────────────────────────
                    var s52 = FindNoticeFile(objRoot, "Section52");
                    if (s52.exists)
                        vm.ObjectionNotices.Add(Notice(
                            objNo, propDesc, roll.Name,
                            NoticeType.Section52Review, "Section 52 – Review Outcome",
                            s52.path, s52.ext, null, null, null));

                    // ── Invalid Objection/Omission ──────────────────────
                    var inv = FindNoticeFile(objRoot, "Invalid");
                    if (inv.exists)
                        vm.ObjectionNotices.Add(Notice(
                            objNo, propDesc, roll.Name,
                            NoticeType.InvalidObjection, "Invalid Objection / Omission Notice",
                            inv.path, inv.ext, null, null, null));

                    // ── Dear Johnny ─────────────────────────────────────
                    var dj = FindNoticeFile(objRoot, "DearJohnny");
                    if (dj.exists)
                        vm.ObjectionNotices.Add(Notice(
                            objNo, propDesc, roll.Name,
                            NoticeType.DearJohnny, "Dear Owner – Multi-Roll Notice",
                            dj.path, dj.ext, null, null, null));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Notices] Objection load failed for roll {Roll}", roll.Short);
            }
        }
    }

    // ── Appeal notices (all rolls) ────────────────────────────────────
    private async Task LoadAppealNoticesAsync(
        string userId, NoticesDashboardViewModel vm)
    {
        foreach (var roll in Rolls)
        {
            try
            {
                var connStr = _config.GetConnectionString(roll.ConnKey)!;
                await using var conn = new SqlConnection(connStr);

                var appeals = await conn.QueryAsync(
                    @"SELECT Appeal_No, A_Property_Desc
                      FROM   dbo.Obj_Property_Info_Appeal
                      WHERE  A_UserID = @UserId",
                    new { UserId = userId });

                foreach (var appeal in appeals)
                {
                    var appNo = appeal.Appeal_No?.ToString() ?? "";
                    var propDesc = appeal.A_Property_Desc?.ToString() ?? "";

                    // Appeal Decision notice (.eml in appeal root folder)
                    var appDecision = FindNoticeFile(AppealRoot, appNo);
                    if (appDecision.exists)
                        vm.AppealNotices.Add(Notice(
                            appNo, propDesc, roll.Name,
                            NoticeType.AppealDecision, "Appeal Decision Notice",
                            appDecision.path, appDecision.ext,
                            null, null, null));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Notices] Appeal load failed for roll {Roll}", roll.Short);
            }
        }
    }

    // ── Section 78 query notices ──────────────────────────────────────
    private async Task LoadQueryNoticesAsync(
        string userId, NoticesDashboardViewModel vm)
    {
        try
        {
            var connStr = _config.GetConnectionString("QueryConnection")!;
            await using var conn = new SqlConnection(connStr);

            var queries = await conn.QueryAsync(
                @"SELECT q.Query_No, s1.Old_Property_Description AS Property_Desc
                  FROM   dbo.Que_Property_Info  q
                  JOIN   dbo.Obj_Section1        s1
                         ON s1.Objection_Ref_S1 = q.Query_No
                  WHERE  q.UserID = @UserId",
                new { UserId = userId });

            foreach (var q in queries)
            {
                var queryNo = q.Query_No?.ToString() ?? "";
                var propDesc = q.Property_Desc?.ToString() ?? "";

                // Section 78 outcome — same pattern as Section 49
                var outcome = FindNoticeFile(QueryRoot, propDesc);
                if (outcome.exists)
                    vm.QueryNotices.Add(Notice(
                        queryNo, propDesc, "Query / Review",
                        NoticeType.Section78Outcome, "Section 78 Query Outcome",
                        outcome.path, outcome.ext,
                        null, null, null));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Notices] Query load failed");
        }
    }

    // ── Find first file in a folder ──────────────────────────────────
    public (bool exists, string path, string ext) FindNoticeFile(
        string parentFolder, string subFolder)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(parentFolder)) return (false, "", "");
            var dir = Path.Combine(parentFolder, subFolder);
            if (!Directory.Exists(dir)) return (false, "", "");

            // Accept PDF or EML
            var file = Directory.GetFiles(dir, "*.pdf").FirstOrDefault()
                    ?? Directory.GetFiles(dir, "*.eml").FirstOrDefault();

            if (file is null) return (false, "", "");
            return (true, file, Path.GetExtension(file).ToLower());
        }
        catch
        {
            return (false, "", "");
        }
    }

    // ── Helper: build a NoticeItem ────────────────────────────────────
    private static NoticeItem Notice(
        string refNo, string propDesc, string rollName,
        NoticeType type, string label,
        string path, string ext,
        DateTime? issued,
        DateTime? appealOpen, DateTime? appealClose)
    {
        DateTime? fileDate = null;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            fileDate = File.GetLastWriteTime(path);

        return new NoticeItem
        {
            ReferenceNo = refNo,
            PropertyDesc = propDesc,
            RollName = rollName,
            Type = type,
            TypeLabel = label,
            IssuedDate = issued ?? fileDate,
            FilePath = path,
            FileExt = ext,
            FileExists = !string.IsNullOrEmpty(path) && File.Exists(path),
            AppealOpenDate = appealOpen,
            AppealCloseDate = appealClose,
        };
    }
}

