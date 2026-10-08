using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using V2_Genesis.Services.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public sealed class Section53NoticeService : ISection53NoticeService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<Section53NoticeService> _logger;

    public Section53NoticeService(
        IConfiguration config,
        IWebHostEnvironment environment,
        ILogger<Section53NoticeService> logger)
    {
        _config = config;
        _environment = environment;
        _logger = logger;
    }

    public async Task<(byte[] Pdf, string FileName)> GenerateAsync(
        string rollSource,
        string objectionNo,
        string userId,
        bool allowAdministrativeAccess = false,
        CancellationToken cancellationToken = default)
    {
        rollSource = rollSource?.Trim() ?? string.Empty;
        objectionNo = objectionNo?.Trim() ?? string.Empty;
        userId = userId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(objectionNo))
            throw new ArgumentException("The objection number is required.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException();

        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var roll))
            throw new KeyNotFoundException("The valuation roll is not supported.");

        var connectionString = _config.GetConnectionString(roll.ConnectionKey)
            ?? throw new InvalidOperationException(
                $"Connection string '{roll.ConnectionKey}' was not found.");

        await using var db = new Section53ReadDbContext(connectionString);

        var data = await (
            from objection in db.Objections.AsNoTracking()
            join mvd in db.MvdDecisions.AsNoTracking()
                on (objection.ObjectionNo ?? string.Empty).Trim()
                equals (mvd.ObjectionNo ?? string.Empty).Trim()
            where (objection.ObjectionNo ?? string.Empty).Trim() == objectionNo
                && (allowAdministrativeAccess ||
                    (objection.UserId ?? string.Empty).Trim() == userId)
            select new { objection, mvd })
            .FirstOrDefaultAsync(cancellationToken);

        var row = data is null ? null : MapRow(data.objection, data.mvd);

        if (row is null)
            throw new KeyNotFoundException(
                "The Section 53 decision was not found for this account.");

        if (!CanDownload(row.ObjectionStatus))
            throw new InvalidOperationException(
                "The Section 53 notice is not available at the current objection stage.");

        var revised = IsTrue(row.ReviseMvd);
        if (revised)
            ApplyRevisedDecision(row);

        // "With Effective Date" = wefDateMVD (same source as eNotice).
        row.EffectiveDateText = await LoadEffectiveDateAsync(
            connectionString, row.ObjectionNo, revised, cancellationToken);

        var pdf = BuildPdf(row, GetRollName(rollSource), revised);
        var safeReference = SanitiseFilePart(row.ObjectionNo);

        _logger.LogInformation(
            "Generated Section 53 notice for {ObjectionNo} on {RollSource}. Revised={Revised}",
            row.ObjectionNo,
            rollSource,
            revised);

        return (pdf, $"{safeReference}_Section53_Valuer_Decision.pdf");
    }

    private static Section53Row MapRow(
        Section53ObjectionEntity objection,
        Section53MvdEntity mvd) => new()
        {
            ObjectionNo = objection.ObjectionNo,
            UserId = objection.UserId,
            ObjectionStatus = objection.ObjectionStatus?.Trim(),
            Addr1 = mvd.Addr1,
            Addr2 = mvd.Addr2,
            Addr3 = mvd.Addr3,
            Addr4 = mvd.Addr4,
            Addr5 = mvd.Addr5,
            PropertyDesc = mvd.PropertyDesc,
            ValuationKey = mvd.ValuationKey,
            GvCategory = mvd.GvCategory,
            GvCategory2 = mvd.GvCategory2,
            GvCategory3 = mvd.GvCategory3,
            GvExtent = mvd.GvExtent,
            GvExtent2 = mvd.GvExtent2,
            GvExtent3 = mvd.GvExtent3,
            GvMarketValue = First(mvd.GvMarketValue, mvd.GvMarketValueFallback),
            GvMarketValue2 = First(mvd.GvMarketValue2, mvd.GvMarketValueFallback2),
            GvMarketValue3 = First(mvd.GvMarketValue3, mvd.GvMarketValueFallback3),
            MvdCategory = mvd.MvdCategory,
            MvdCategory2 = mvd.MvdCategory2,
            MvdCategory3 = mvd.MvdCategory3,
            MvdExtent = mvd.MvdExtent,
            MvdExtent2 = mvd.MvdExtent2,
            MvdExtent3 = mvd.MvdExtent3,
            MvdMarketValue = First(mvd.MvdMarketValue, mvd.MvdMarketValueFallback),
            MvdMarketValue2 = First(mvd.MvdMarketValue2, mvd.MvdMarketValueFallback2),
            MvdMarketValue3 = First(mvd.MvdMarketValue3, mvd.MvdMarketValueFallback3),
            Section52Review = mvd.Section52Review,
            BatchDate = mvd.BatchDate,
            AppealStartDate = mvd.AppealStartDate,
            AppealCloseDate = mvd.AppealCloseDate,
            ReviseMvd = mvd.ReviseMvd?.ToString(),
            RevisedCategory = mvd.RevisedCategory,
            RevisedCategory2 = mvd.RevisedCategory2,
            RevisedCategory3 = mvd.RevisedCategory3,
            RevisedExtent = mvd.RevisedExtent,
            RevisedExtent2 = mvd.RevisedExtent2,
            RevisedExtent3 = mvd.RevisedExtent3,
            RevisedMarketValue = First(
            mvd.RevisedMarketValue,
            mvd.RevisedMarketValueFallback),
            RevisedMarketValue2 = First(
            mvd.RevisedMarketValue2,
            mvd.RevisedMarketValueFallback2),
            RevisedMarketValue3 = First(
            mvd.RevisedMarketValue3,
            mvd.RevisedMarketValueFallback3),
            RevisedSection52Review = mvd.RevisedSection52Review,
            RevisedBatchDate = mvd.RevisedBatchDate,
            RevisedAppealStartDate = mvd.RevisedAppealStartDate,
            RevisedAppealCloseDate = mvd.RevisedAppealCloseDate
        };

    // ── Effective date ────────────────────────────────────────────────
    // Objection_MVD.wefDateMVD (eNotice uses the same column). A revised MVD
    // uses Obj_Property_Info.wefDate_ReviseMVD when it is filled. The column
    // can be a date or text, so it is read as an object and formatted here.
    private async Task<string> LoadEffectiveDateAsync(
        string connectionString,
        string? objectionNo,
        bool revised,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectionNo))
            return string.Empty;

        var queries = new List<string>();
        if (revised)
            queries.Add("SELECT TOP (1) wefDate_ReviseMVD FROM dbo.Obj_Property_Info WHERE LTRIM(RTRIM(Objection_No)) = @No AND wefDate_ReviseMVD IS NOT NULL;");
        queries.Add("SELECT TOP (1) wefDateMVD FROM dbo.Objection_MVD WHERE LTRIM(RTRIM(Objection_No)) = @No AND wefDateMVD IS NOT NULL;");
        queries.Add("SELECT TOP (1) wefDateMVD FROM dbo.Obj_Property_Info WHERE LTRIM(RTRIM(Objection_No)) = @No AND wefDateMVD IS NOT NULL;");

        await using var conn = new SqlConnection(connectionString);

        foreach (var sql in queries)
        {
            try
            {
                var value = await conn.ExecuteScalarAsync<object?>(
                    new CommandDefinition(sql, new { No = objectionNo.Trim() },
                        commandTimeout: 30, cancellationToken: cancellationToken));

                var text = value switch
                {
                    null or DBNull => string.Empty,
                    DateTime d => V2_Genesis.Helpers.WefDateFormatter.Format(d),
                    DateTimeOffset o => V2_Genesis.Helpers.WefDateFormatter.Format(o.DateTime),
                    DateOnly d => V2_Genesis.Helpers.WefDateFormatter.Format(d.ToDateTime(TimeOnly.MinValue)),
                    _ => V2_Genesis.Helpers.WefDateFormatter.Format(value.ToString())
                };

                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Section53] Effective date lookup failed for {ObjectionNo}", objectionNo);
            }
        }

        return string.Empty;
    }

    // ── Section 53 MVD notice ─────────────────────────────────────────
    // Same layout and wording as eNotice (GV23_Notice Section53PdfService),
    // which sends this notice. Settings: appsettings "Section53Pdf".
    private byte[] BuildPdf(Section53Row row, string rollName, bool revised)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var za = CultureInfo.GetCultureInfo("en-ZA");
        var letterDate = revised
            ? row.RevisedBatchDate ?? row.BatchDate ?? DateTime.Today
            : row.BatchDate ?? DateTime.Today;
        var appealCloseDate = revised
            ? row.RevisedAppealCloseDate ?? row.AppealCloseDate
            : row.AppealCloseDate;

        var headerRelative = _config["Section53Pdf:HeaderImageRelativePath"] ?? "Images/Obj_Header.PNG";
        var headerPath = Path.Combine(new[] { _environment.WebRootPath }
            .Concat(headerRelative.Replace('\\', '/').TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            .ToArray());
        var portalUrl = _config["Section53Pdf:PortalUrl"] ?? "https://objections.joburg.org.za/";
        var contactLine = _config["Section53Pdf:ContactLine"]
            ?? "For any enquiries, please contact us on 011 407 6622 or 011 407 6597, or email valuationenquiries@joburg.org.za";
        var valuerName = _config["Section53Pdf:MunicipalValuerName"] ?? "S. Faiaz";
        var valuerTitle = _config["Section53Pdf:MunicipalValuerTitle"] ?? "Municipal Valuer";

        var title12 = TextStyle.Default.FontFamily("Arial").FontSize(12).SemiBold();
        var body9 = TextStyle.Default.FontFamily("Arial").FontSize(9);
        var body9b = TextStyle.Default.FontFamily("Arial").FontSize(9).SemiBold();
        var small7 = TextStyle.Default.FontFamily("Arial").FontSize(7).FontColor(Colors.Grey.Darken2);
        var red7b = TextStyle.Default.FontFamily("Arial").FontSize(7).SemiBold().FontColor(Colors.Red.Medium);

        var noticeMainTitle = revised ? "SECTION 53:REVISED MVD NOTICE" : "SECTION 53:MVD NOTICE";
        var noticeSubTitle = revised
            ? "Revised notification of outcome of objection in terms of section 53(1) of the Municipal Property Rates Act, No.6 of 2004 as amended"
            : "Notification of outcome of objection in terms of section 53(1) of the Municipal Property Rates Act, No.6 of 2004 as amended";
        var mvdDecisionSentence = revised
            ? "the revised Municipal Valuer’s decision is as follows:"
            : "the Municipal Valuer’s decision is as follows:";
        var originalNoticeDate = revised && row.BatchDate.HasValue
            ? row.BatchDate.Value.ToString("d MMMM yyyy", za)
            : string.Empty;
        var revisedNoticeText = string.IsNullOrWhiteSpace(originalNoticeDate)
            ? "This revised notice supersedes the previous Section 53 Municipal Valuer’s Decision notice and must be regarded as the official Section 53 Revised Municipal Valuer’s Decision notice."
            : $"This revised notice supersedes the Section 53 Municipal Valuer’s Decision notice dated {originalNoticeDate} and must be regarded as the official Section 53 Revised Municipal Valuer’s Decision notice.";

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginLeft(30);
                page.MarginRight(30);
                page.MarginTop(10);
                page.MarginBottom(10);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Footer()
                    .PaddingTop(8)
                    .AlignCenter()
                    .Text(t =>
                    {
                        t.Line("_______________________________________________").Style(small7);
                        t.Line("This is an official document generated by the City of Johannesburg Valuation Services Department").Style(small7);
                        t.Line($"Generated on: {letterDate:dd MMMM yyyy}").Style(small7);
                        t.Line((row.ValuationKey ?? string.Empty).Trim()).Style(red7b);
                    });

                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    if (File.Exists(headerPath))
                    {
                        col.Item().Image(headerPath, ImageScaling.FitWidth);
                        col.Item().PaddingTop(6);
                    }

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(left =>
                        {
                            AddLine(left, row.Addr1);
                            AddLine(left, row.Addr2);
                            AddLine(left, row.Addr3);
                            AddLine(left, row.Addr4);
                            AddLine(left, row.Addr5);
                        });

                        r.ConstantItem(180).AlignRight()
                            .Text(letterDate.ToString("dd MMMM yyyy", za))
                            .Style(body9);
                    });

                    col.Item().PaddingTop(2);
                    col.Item().AlignCenter().Text(noticeMainTitle).Style(title12);
                    col.Item().AlignCenter().Text(noticeSubTitle).Style(body9b);
                    col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(1);
                    col.Item().PaddingTop(1).Text("Dear Client").FontFamily("Arial").FontSize(9).Bold();

                    if (revised)
                    {
                        col.Item()
                            .PaddingLeft(3)
                            .Text(revisedNoticeText)
                            .Style(body9b)
                            .FontColor(Colors.Red.Darken2).SemiBold();
                    }

                    col.Item().PaddingTop(1);

                    col.Item().Text(t =>
                    {
                        t.Span("Notice is hereby given in terms of section 53(1) of the Municipal Property Rates Act No.6 of 2004 as amended, that the objection Nr. ").Style(body9);
                        t.Span(row.ObjectionNo ?? string.Empty).Style(body9b);
                        t.Span(" against the entry of the above property in or omitted from the valuation roll has been considered by the Municipal Valuer. ").Style(body9);
                        t.Span("After reviewing the objection and reasons provided therein and the submission of the owner if a submission was made, together with the available market information, ").Style(body9);
                        t.Span(mvdDecisionSentence).Style(body9);
                    });

                    col.Item().Text(t =>
                    {
                        t.Span("Property Description: ").Style(body9b);
                        t.Span(row.PropertyDesc ?? string.Empty).Style(body9);
                    });

                    col.Item().Element(container => BuildDecisionTable(container, row, rollName, revised));

                    col.Item().Text(t =>
                    {
                        t.Span("Section 52 Review: ").Style(body9b);
                        t.Span(row.Section52Review ?? string.Empty).Style(body9);
                    });

                    col.Item().Text(
                            "Kindly note that in terms of Section 52 of the Municipal Property Rates Act No.6 of 2004 as amended, if the value has changed by more than 10% upwards or downwards, an automatic review by the Valuation Appeal Board will be conducted who may confirm, amend or revoke the decision.")
                        .Style(body9b)
                        .Justify();

                    col.Item().Text("Right of Appeal").Style(body9b);

                    col.Item().PaddingTop(1).Text(t =>
                    {
                        t.Span("In terms of Section 54(1) an appeal to the Appeal Board against the above decision may be lodged in the prescribed manner online on the City’s online system on the following link: ").Style(body9);
                        t.Span(portalUrl).Style(body9).Bold();
                        t.Span(" or at the following address: Valuation Services: Administration, 1st Floor, East Wing, Jorissen Place, 66 Jorissen Street, Braamfontein on or before 15:00 on ").Style(body9);
                        t.Span(appealCloseDate.HasValue ? appealCloseDate.Value.ToString("dd MMMM yyyy", za) : string.Empty).Style(body9b);
                        t.Span(".").Style(body9);
                    });

                    col.Item().PaddingTop(1).Text(
                            "An acknowledgement letter will be auto-generated and should be kept as proof that the appeal was submitted. Please include this notice when submitting your appeal for ease of reference. Kindly note that objections relating to matters other than those stated above (e.g., owner’s name, street address, etc.) will not be dealt with as objections, but will be forwarded to the relevant department for amendment to the valuation roll in terms of Section 79 of the above Act. If a representative is appointed, proof of authorisation must be attached to the appeal form.")
                        .Style(body9)
                        .Justify();

                    if (!string.IsNullOrWhiteSpace(contactLine))
                        col.Item().PaddingTop(1).Text(contactLine).Style(body9).Bold();

                    col.Item().Text(valuerTitle).Style(body9b);
                    col.Item().PaddingTop(1).Text(valuerName).Style(body9b);
                });
            });
        }).GeneratePdf();
    }

    private static void BuildDecisionTable(
        IContainer container,
        Section53Row row,
        string rollName,
        bool revised)
    {
        var decisionHeading = revised
            ? "Revised Municipal Valuer’s Decision (MVD)"
            : "Municipal Valuer’s Decision (MVD)";

        // The effective date is printed on the main rows (roll and decision).
        // Split rows leave it blank (one effective date per property).
        var wef = row.EffectiveDateText ?? string.Empty;

        container.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.ConstantColumn(150);
                c.RelativeColumn();
                c.RelativeColumn();
            });

            t.Header(h =>
            {
                h.Cell().Element(BlueHeaderCell).Text("");
                h.Cell().Element(BlueHeaderCell).Text($"Entry in {rollName} (GVR2023).")
                    .FontFamily("Arial").FontSize(9).SemiBold().FontColor(Colors.White);
                h.Cell().Element(BlueHeaderCell).Text(decisionHeading)
                    .FontFamily("Arial").FontSize(9).SemiBold().FontColor(Colors.White);
            });

            DataRow(t, "Category", row.GvCategory, row.MvdCategory);
            DataRow(t, "Extent", FormatExtent(row.GvExtent), FormatExtent(row.MvdExtent));
            DataRow(t, "Market Value", FormatRand(row.GvMarketValue), FormatRand(row.MvdMarketValue));
            DataRow(t, "With Effective Date", wef, wef);

            if (HasSecondDecision(row))
            {
                DataRow(t, "", "", "");
                DataRow(t, "Category Split 1", row.GvCategory2, row.MvdCategory2);
                DataRow(t, "Extent Split 1", FormatExtent(row.GvExtent2), FormatExtent(row.MvdExtent2));
                DataRow(t, "Market Value Split 1", FormatRand(row.GvMarketValue2), FormatRand(row.MvdMarketValue2));
                DataRow(t, "With Effective Date", "", "");
            }

            if (HasThirdDecision(row))
            {
                DataRow(t, "", "", "");
                DataRow(t, "Category Split 2", row.GvCategory3, row.MvdCategory3);
                DataRow(t, "Extent Split 2", FormatExtent(row.GvExtent3), FormatExtent(row.MvdExtent3));
                DataRow(t, "Market Value Split 2", FormatRand(row.GvMarketValue3), FormatRand(row.MvdMarketValue3));
                DataRow(t, "With Effective Date", "", "");
            }
        });

        static void DataRow(TableDescriptor t, string label, string? left, string? right)
        {
            t.Cell().Element(CellBase).Text(label).FontFamily("Arial").FontSize(9).SemiBold();
            t.Cell().Element(CellBase).Text((left ?? string.Empty).Trim()).FontFamily("Arial").FontSize(9);
            t.Cell().Element(CellBase).Text((right ?? string.Empty).Trim()).FontFamily("Arial").FontSize(9);
        }

        static IContainer CellBase(IContainer c) =>
            c.Border(1).PaddingVertical(4).PaddingHorizontal(6);

        static IContainer BlueHeaderCell(IContainer c) =>
            c.Border(1)
             .Background(Color.FromRGB(70, 130, 180))
             .PaddingVertical(4)
             .PaddingHorizontal(6);
    }

    private static void AddLine(ColumnDescriptor column, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            column.Item().Text(value.Trim()).FontFamily("Arial").FontSize(9);
    }

    private static bool CanDownload(string? status) => status is not null &&
        (status.Equals("Notice-Sent", StringComparison.OrdinalIgnoreCase) ||
         status.Equals("Appeal-Closed", StringComparison.OrdinalIgnoreCase));

    private static bool IsTrue(string? value) =>
        value?.Trim() is "1" or "Y" or "Yes" or "YES" or "True" or "TRUE";

    private static void ApplyRevisedDecision(Section53Row row)
    {
        row.MvdCategory = First(row.RevisedCategory, row.MvdCategory);
        row.MvdCategory2 = First(row.RevisedCategory2, row.MvdCategory2);
        row.MvdCategory3 = First(row.RevisedCategory3, row.MvdCategory3);
        row.MvdExtent = First(row.RevisedExtent, row.MvdExtent);
        row.MvdExtent2 = First(row.RevisedExtent2, row.MvdExtent2);
        row.MvdExtent3 = First(row.RevisedExtent3, row.MvdExtent3);
        row.MvdMarketValue = First(row.RevisedMarketValue, row.MvdMarketValue);
        row.MvdMarketValue2 = First(row.RevisedMarketValue2, row.MvdMarketValue2);
        row.MvdMarketValue3 = First(row.RevisedMarketValue3, row.MvdMarketValue3);
        row.Section52Review = First(row.RevisedSection52Review, row.Section52Review);
        row.AppealStartDate = row.RevisedAppealStartDate ?? row.AppealStartDate;
        row.AppealCloseDate = row.RevisedAppealCloseDate ?? row.AppealCloseDate;
    }

    private static string? First(string? primary, string? fallback) =>
        !string.IsNullOrWhiteSpace(primary) ? primary.Trim() : fallback?.Trim();

    private static bool HasSecondDecision(Section53Row row) =>
        !string.IsNullOrWhiteSpace(row.GvCategory2) ||
        !string.IsNullOrWhiteSpace(row.GvExtent2) ||
        !string.IsNullOrWhiteSpace(row.GvMarketValue2) ||
        !string.IsNullOrWhiteSpace(row.MvdCategory2) ||
        !string.IsNullOrWhiteSpace(row.MvdExtent2) ||
        !string.IsNullOrWhiteSpace(row.MvdMarketValue2);

    private static bool HasThirdDecision(Section53Row row) =>
        !string.IsNullOrWhiteSpace(row.GvCategory3) ||
        !string.IsNullOrWhiteSpace(row.GvExtent3) ||
        !string.IsNullOrWhiteSpace(row.GvMarketValue3) ||
        !string.IsNullOrWhiteSpace(row.MvdCategory3) ||
        !string.IsNullOrWhiteSpace(row.MvdExtent3) ||
        !string.IsNullOrWhiteSpace(row.MvdMarketValue3);

    // Same formats as eNotice: "R 29 184 000", extent "1 174" or "1 174.50".
    private static string FormatRand(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var raw = value.Replace("R", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(",", string.Empty).Trim();
        raw = new string(raw.Where(ch => char.IsDigit(ch) || ch == '.' || ch == '-').ToArray());
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
            ? "R " + amount.ToString("#,##0", CultureInfo.InvariantCulture).Replace(",", " ")
            : value.Trim();
    }

    private static string FormatExtent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var raw = value.Replace(",", string.Empty).Trim();
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var extent))
            return value.Trim();
        return extent == Math.Truncate(extent)
            ? extent.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ")
            : extent.ToString("N2", CultureInfo.InvariantCulture).Replace(",", " ");
    }

    private static string FormatDate(DateTime? value) =>
        value?.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("en-ZA")) ?? string.Empty;

    private static string GetRollName(string rollSource) => rollSource switch
    {
        "Objection" => "General Valuation Roll",
        "Objection_Supp1" => "Supplementary Valuation Roll 1",
        "Objection_Supp2" => "Supplementary Valuation Roll 2",
        "Objection_Supp3" => "Supplementary Valuation Roll 3",
        "Objection_Supp4" => "Supplementary Valuation Roll 4",
        "Objection_Supp5" => "Supplementary Valuation Roll 5",
        _ => "Valuation Roll"
    };

    private static string SanitiseFilePart(string? value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string((value ?? "Section53")
            .Where(character => !invalid.Contains(character))
            .ToArray());
    }

    private sealed class Section53ReadDbContext : DbContext
    {
        private readonly string _connectionString;

        public Section53ReadDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbSet<Section53ObjectionEntity> Objections =>
            Set<Section53ObjectionEntity>();

        public DbSet<Section53MvdEntity> MvdDecisions =>
            Set<Section53MvdEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                _connectionString,
                sqlServer => sqlServer.CommandTimeout(60));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Section53ObjectionEntity>(entity =>
            {
                entity.HasKey(x => x.ObjectionId);
                entity.ToTable("Obj_Property_Info", "dbo");
                entity.Property(x => x.ObjectionId).HasColumnName("Objection_ID");
                entity.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
                entity.Property(x => x.UserId).HasColumnName("UserID");
                entity.Property(x => x.ObjectionStatus).HasColumnName("objection_Status");
            });

            modelBuilder.Entity<Section53MvdEntity>(entity =>
            {
                entity.HasNoKey();
                entity.ToTable("Objection_MVD", "dbo");
                entity.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
                entity.Property(x => x.Addr1).HasColumnName("ADDR1");
                entity.Property(x => x.Addr2).HasColumnName("ADDR2");
                entity.Property(x => x.Addr3).HasColumnName("ADDR3");
                entity.Property(x => x.Addr4).HasColumnName("ADDR4");
                entity.Property(x => x.Addr5).HasColumnName("ADDR5");
                entity.Property(x => x.PropertyDesc).HasColumnName("Property_desc");
                entity.Property(x => x.ValuationKey).HasColumnName("valuation_Key");
                entity.Property(x => x.GvCategory).HasColumnName("GV_Category");
                entity.Property(x => x.GvCategory2).HasColumnName("GV_Category2");
                entity.Property(x => x.GvCategory3).HasColumnName("GV_Category3");
                entity.Property(x => x.GvExtent).HasColumnName("GV_Extent");
                entity.Property(x => x.GvExtent2).HasColumnName("GV_Extent2");
                entity.Property(x => x.GvExtent3).HasColumnName("GV_Extent3");
                entity.Property(x => x.GvMarketValue).HasColumnName("GV_Market_Value");
                entity.Property(x => x.GvMarketValue2).HasColumnName("GV_Market_Value2");
                entity.Property(x => x.GvMarketValue3).HasColumnName("GV_Market_Value3");
                entity.Property(x => x.GvMarketValueFallback).HasColumnName("GVMarketValue");
                entity.Property(x => x.GvMarketValueFallback2).HasColumnName("GVMarketValue2");
                entity.Property(x => x.GvMarketValueFallback3).HasColumnName("GVMarketValue3");
                entity.Property(x => x.MvdCategory).HasColumnName("MVD_Category");
                entity.Property(x => x.MvdCategory2).HasColumnName("MVD_Category2");
                entity.Property(x => x.MvdCategory3).HasColumnName("MVD_Category3");
                entity.Property(x => x.MvdExtent).HasColumnName("MVD_Extent");
                entity.Property(x => x.MvdExtent2).HasColumnName("MVD_Extent2");
                entity.Property(x => x.MvdExtent3).HasColumnName("MVD_Extent3");
                entity.Property(x => x.MvdMarketValue).HasColumnName("MVD_Market_Value");
                entity.Property(x => x.MvdMarketValue2).HasColumnName("MVD_Market_Value2");
                entity.Property(x => x.MvdMarketValue3).HasColumnName("MVD_Market_Value3");
                entity.Property(x => x.MvdMarketValueFallback).HasColumnName("MVDMarketValue");
                entity.Property(x => x.MvdMarketValueFallback2).HasColumnName("MVDMarketValue2");
                entity.Property(x => x.MvdMarketValueFallback3).HasColumnName("MVDMarketValue3");
                entity.Property(x => x.Section52Review).HasColumnName("Section52Review");
                entity.Property(x => x.BatchDate).HasColumnName("Batch_Date");
                entity.Property(x => x.AppealStartDate).HasColumnName("Appeal_Start_Date");
                entity.Property(x => x.AppealCloseDate).HasColumnName("Appeal_Close_Date");
                entity.Property(x => x.ReviseMvd).HasColumnName("Revise_MVD");
                entity.Property(x => x.RevisedCategory).HasColumnName("ReviseMVD_Category");
                entity.Property(x => x.RevisedCategory2).HasColumnName("ReviseMVD_Category2");
                entity.Property(x => x.RevisedCategory3).HasColumnName("ReviseMVD_Category3");
                entity.Property(x => x.RevisedExtent).HasColumnName("ReviseMVD_Extent");
                entity.Property(x => x.RevisedExtent2).HasColumnName("ReviseMVD_Extent2");
                entity.Property(x => x.RevisedExtent3).HasColumnName("ReviseMVD_Extent3");
                entity.Property(x => x.RevisedMarketValue).HasColumnName("ReviseMVD_Market_Value");
                entity.Property(x => x.RevisedMarketValue2).HasColumnName("ReviseMVD_Market_Value2");
                entity.Property(x => x.RevisedMarketValue3).HasColumnName("ReviseMVD_Market_Value3");
                entity.Property(x => x.RevisedMarketValueFallback).HasColumnName("ReviseMVD_MarketValue");
                entity.Property(x => x.RevisedMarketValueFallback2).HasColumnName("ReviseMVD_MarketValue2");
                entity.Property(x => x.RevisedMarketValueFallback3).HasColumnName("ReviseMVD_MarketValue3");
                entity.Property(x => x.RevisedSection52Review).HasColumnName("Section52Review_Revise_MVD");
                entity.Property(x => x.RevisedBatchDate).HasColumnName("Batch_Date_ReviseMVD");
                entity.Property(x => x.RevisedAppealStartDate).HasColumnName("Appeal_Start_Date_ReviseMVD");
                entity.Property(x => x.RevisedAppealCloseDate).HasColumnName("Appeal_Close_Date_ReviseMVD");
            });
        }
    }

    private sealed class Section53ObjectionEntity
    {
        public long ObjectionId { get; set; }
        public string? ObjectionNo { get; set; }
        public string? UserId { get; set; }
        public string? ObjectionStatus { get; set; }
    }

    private sealed class Section53MvdEntity
    {
        public string? ObjectionNo { get; set; }
        public string? Addr1 { get; set; }
        public string? Addr2 { get; set; }
        public string? Addr3 { get; set; }
        public string? Addr4 { get; set; }
        public string? Addr5 { get; set; }
        public string? PropertyDesc { get; set; }
        public string? ValuationKey { get; set; }
        public string? GvCategory { get; set; }
        public string? GvCategory2 { get; set; }
        public string? GvCategory3 { get; set; }
        public string? GvExtent { get; set; }
        public string? GvExtent2 { get; set; }
        public string? GvExtent3 { get; set; }
        public string? GvMarketValue { get; set; }
        public string? GvMarketValue2 { get; set; }
        public string? GvMarketValue3 { get; set; }
        public string? GvMarketValueFallback { get; set; }
        public string? GvMarketValueFallback2 { get; set; }
        public string? GvMarketValueFallback3 { get; set; }
        public string? MvdCategory { get; set; }
        public string? MvdCategory2 { get; set; }
        public string? MvdCategory3 { get; set; }
        public string? MvdExtent { get; set; }
        public string? MvdExtent2 { get; set; }
        public string? MvdExtent3 { get; set; }
        public string? MvdMarketValue { get; set; }
        public string? MvdMarketValue2 { get; set; }
        public string? MvdMarketValue3 { get; set; }
        public string? MvdMarketValueFallback { get; set; }
        public string? MvdMarketValueFallback2 { get; set; }
        public string? MvdMarketValueFallback3 { get; set; }
        public string? Section52Review { get; set; }
        public DateTime? BatchDate { get; set; }
        public DateTime? AppealStartDate { get; set; }
        public DateTime? AppealCloseDate { get; set; }
        public bool? ReviseMvd { get; set; }
        public string? RevisedCategory { get; set; }
        public string? RevisedCategory2 { get; set; }
        public string? RevisedCategory3 { get; set; }
        public string? RevisedExtent { get; set; }
        public string? RevisedExtent2 { get; set; }
        public string? RevisedExtent3 { get; set; }
        public string? RevisedMarketValue { get; set; }
        public string? RevisedMarketValue2 { get; set; }
        public string? RevisedMarketValue3 { get; set; }
        public string? RevisedMarketValueFallback { get; set; }
        public string? RevisedMarketValueFallback2 { get; set; }
        public string? RevisedMarketValueFallback3 { get; set; }
        public string? RevisedSection52Review { get; set; }
        public DateTime? RevisedBatchDate { get; set; }
        public DateTime? RevisedAppealStartDate { get; set; }
        public DateTime? RevisedAppealCloseDate { get; set; }
    }

    private sealed class Section53Row
    {
        public string? ObjectionNo { get; set; }
        public string? UserId { get; set; }
        public string? ObjectionStatus { get; set; }
        public string? Addr1 { get; set; }
        public string? Addr2 { get; set; }
        public string? Addr3 { get; set; }
        public string? Addr4 { get; set; }
        public string? Addr5 { get; set; }
        public string? PropertyDesc { get; set; }
        public string? ValuationKey { get; set; }
        public string? GvCategory { get; set; }
        public string? GvCategory2 { get; set; }
        public string? GvCategory3 { get; set; }
        public string? GvExtent { get; set; }
        public string? GvExtent2 { get; set; }
        public string? GvExtent3 { get; set; }
        public string? GvMarketValue { get; set; }
        public string? GvMarketValue2 { get; set; }
        public string? GvMarketValue3 { get; set; }
        public string? MvdCategory { get; set; }
        public string? MvdCategory2 { get; set; }
        public string? MvdCategory3 { get; set; }
        public string? MvdExtent { get; set; }
        public string? MvdExtent2 { get; set; }
        public string? MvdExtent3 { get; set; }
        public string? MvdMarketValue { get; set; }
        public string? MvdMarketValue2 { get; set; }
        public string? MvdMarketValue3 { get; set; }
        public string? Section52Review { get; set; }
        public DateTime? BatchDate { get; set; }
        public DateTime? AppealStartDate { get; set; }
        public DateTime? AppealCloseDate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string? EffectiveDateText { get; set; }
        public string? ReviseMvd { get; set; }
        public string? RevisedCategory { get; set; }
        public string? RevisedCategory2 { get; set; }
        public string? RevisedCategory3 { get; set; }
        public string? RevisedExtent { get; set; }
        public string? RevisedExtent2 { get; set; }
        public string? RevisedExtent3 { get; set; }
        public string? RevisedMarketValue { get; set; }
        public string? RevisedMarketValue2 { get; set; }
        public string? RevisedMarketValue3 { get; set; }
        public string? RevisedSection52Review { get; set; }
        public DateTime? RevisedBatchDate { get; set; }
        public DateTime? RevisedAppealStartDate { get; set; }
        public DateTime? RevisedAppealCloseDate { get; set; }
    }
}
