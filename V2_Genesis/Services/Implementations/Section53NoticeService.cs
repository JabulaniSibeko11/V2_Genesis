using Dapper;
using Microsoft.Data.SqlClient;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
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

        const string sql = """
            SELECT TOP (1)
                p.Objection_No                         AS ObjectionNo,
                p.UserID                               AS UserId,
                LTRIM(RTRIM(p.objection_Status))       AS ObjectionStatus,
                m.ADDR1                                AS Addr1,
                m.ADDR2                                AS Addr2,
                m.ADDR3                                AS Addr3,
                m.ADDR4                                AS Addr4,
                m.ADDR5                                AS Addr5,
                m.Property_desc                        AS PropertyDesc,
                m.valuation_Key                        AS ValuationKey,
                m.GV_Category                          AS GvCategory,
                m.GV_Category2                         AS GvCategory2,
                m.GV_Category3                         AS GvCategory3,
                m.GV_Extent                            AS GvExtent,
                m.GV_Extent2                           AS GvExtent2,
                m.GV_Extent3                           AS GvExtent3,
                COALESCE(NULLIF(m.GV_Market_Value, ''), m.GVMarketValue)
                                                       AS GvMarketValue,
                COALESCE(NULLIF(m.GV_Market_Value2, ''), m.GVMarketValue2)
                                                       AS GvMarketValue2,
                COALESCE(NULLIF(m.GV_Market_Value3, ''), m.GVMarketValue3)
                                                       AS GvMarketValue3,
                m.MVD_Category                         AS MvdCategory,
                m.MVD_Category2                        AS MvdCategory2,
                m.MVD_Category3                        AS MvdCategory3,
                m.MVD_Extent                           AS MvdExtent,
                m.MVD_Extent2                          AS MvdExtent2,
                m.MVD_Extent3                          AS MvdExtent3,
                COALESCE(NULLIF(m.MVD_Market_Value, ''), m.MVDMarketValue)
                                                       AS MvdMarketValue,
                COALESCE(NULLIF(m.MVD_Market_Value2, ''), m.MVDMarketValue2)
                                                       AS MvdMarketValue2,
                COALESCE(NULLIF(m.MVD_Market_Value3, ''), m.MVDMarketValue3)
                                                       AS MvdMarketValue3,
                m.Section52Review                      AS Section52Review,
                m.Batch_Date                           AS BatchDate,
                m.Appeal_Start_Date                    AS AppealStartDate,
                m.Appeal_Close_Date                    AS AppealCloseDate,
                TRY_CONVERT(datetime2, m.WEFDATEMVD)   AS EffectiveDate,
                CONVERT(nvarchar(20), m.Revise_MVD)    AS ReviseMvd,
                m.ReviseMVD_Category                   AS RevisedCategory,
                m.ReviseMVD_Category2                  AS RevisedCategory2,
                m.ReviseMVD_Category3                  AS RevisedCategory3,
                m.ReviseMVD_Extent                     AS RevisedExtent,
                m.ReviseMVD_Extent2                    AS RevisedExtent2,
                m.ReviseMVD_Extent3                    AS RevisedExtent3,
                COALESCE(NULLIF(m.ReviseMVD_Market_Value, ''), m.ReviseMVD_MarketValue)
                                                       AS RevisedMarketValue,
                COALESCE(NULLIF(m.ReviseMVD_Market_Value2, ''), m.ReviseMVD_MarketValue2)
                                                       AS RevisedMarketValue2,
                COALESCE(NULLIF(m.ReviseMVD_Market_Value3, ''), m.ReviseMVD_MarketValue3)
                                                       AS RevisedMarketValue3,
                m.Section52Review_Revise_MVD            AS RevisedSection52Review,
                m.Batch_Date_ReviseMVD                  AS RevisedBatchDate,
                m.Appeal_Start_Date_ReviseMVD           AS RevisedAppealStartDate,
                m.Appeal_Close_Date_ReviseMVD           AS RevisedAppealCloseDate
            FROM dbo.Obj_Property_Info AS p
            INNER JOIN dbo.Objection_MVD AS m
                ON LTRIM(RTRIM(m.Objection_No)) = LTRIM(RTRIM(p.Objection_No))
            WHERE LTRIM(RTRIM(p.Objection_No)) = @ObjectionNo
              AND p.UserID = @UserId;
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            sql,
            new { ObjectionNo = objectionNo, UserId = userId },
            commandType: CommandType.Text,
            commandTimeout: 60,
            cancellationToken: cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<Section53Row>(command);

        if (row is null)
            throw new KeyNotFoundException(
                "The Section 53 decision was not found for this account.");

        if (!CanDownload(row.ObjectionStatus))
            throw new InvalidOperationException(
                "The Section 53 notice is not available at the current objection stage.");

        var revised = IsTrue(row.ReviseMvd);
        if (revised)
            ApplyRevisedDecision(row);

        var pdf = BuildPdf(row, GetRollName(rollSource), revised);
        var safeReference = SanitiseFilePart(row.ObjectionNo);

        _logger.LogInformation(
            "Generated Section 53 notice for {ObjectionNo} on {RollSource}. Revised={Revised}",
            row.ObjectionNo,
            rollSource,
            revised);

        return (pdf, $"{safeReference}_Section53_Valuer_Decision.pdf");
    }

    private byte[] BuildPdf(Section53Row row, string rollName, bool revised)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var letterDate = revised
            ? row.RevisedBatchDate ?? row.BatchDate ?? DateTime.Today
            : row.BatchDate ?? DateTime.Today;
        var appealCloseDate = revised
            ? row.RevisedAppealCloseDate ?? row.AppealCloseDate
            : row.AppealCloseDate;
        var headerPath = Path.Combine(
            _environment.WebRootPath,
            "Images",
            "Obj_Header.PNG");

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Footer().AlignCenter().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    footer.Item().PaddingTop(4).Text(
                        "Official document — City of Johannesburg Valuation Services")
                        .FontSize(7).FontColor(Colors.Grey.Darken2);
                    footer.Item().Text(row.ValuationKey ?? string.Empty)
                        .FontSize(7).FontColor(Colors.Red.Medium);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(7);

                    if (File.Exists(headerPath))
                        column.Item().Image(headerPath, ImageScaling.FitWidth);

                    column.Item().Row(addressRow =>
                    {
                        addressRow.RelativeItem().Column(address =>
                        {
                            AddLine(address, row.Addr1);
                            AddLine(address, row.Addr2);
                            AddLine(address, row.Addr3);
                            AddLine(address, row.Addr4);
                            AddLine(address, row.Addr5);
                        });
                        addressRow.ConstantItem(170).AlignRight()
                            .Text(letterDate.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("en-ZA")));
                    });

                    column.Item().AlignCenter()
                        .Text(revised ? "SECTION 53: REVISED MVD NOTICE" : "SECTION 53: MVD NOTICE")
                        .FontSize(12).Bold();
                    column.Item().AlignCenter()
                        .Text("Notification of outcome of objection in terms of section 53(1) of the Municipal Property Rates Act, No. 6 of 2004 as amended")
                        .Bold();
                    column.Item().LineHorizontal(1.5f).LineColor(Colors.Grey.Darken2);
                    column.Item().Text("Dear Client").Bold();

                    if (revised)
                    {
                        column.Item().Text(
                            "This revised notice supersedes the previous Section 53 Municipal Valuer’s Decision notice and is the official revised decision.")
                            .Bold().FontColor(Colors.Red.Darken2);
                    }

                    column.Item().Text(text =>
                    {
                        text.Span("Notice is hereby given in terms of section 53(1) that objection number ");
                        text.Span(row.ObjectionNo ?? string.Empty).Bold();
                        text.Span(" has been considered by the Municipal Valuer. The decision is recorded below.");
                    });
                    column.Item().Text(text =>
                    {
                        text.Span("Property Description: ").Bold();
                        text.Span(row.PropertyDesc ?? string.Empty);
                    });

                    column.Item().Element(container => BuildDecisionTable(container, row, rollName, revised));

                    column.Item().Text(text =>
                    {
                        text.Span("Section 52 Review: ").Bold();
                        text.Span(row.Section52Review ?? string.Empty);
                    });
                    column.Item().Text(
                        "If the value has changed by more than 10% upwards or downwards, an automatic review by the Valuation Appeal Board may be conducted in terms of section 52.")
                        .Bold().Justify();
                    column.Item().Text("Right of Appeal").Bold();
                    column.Item().Text(text =>
                    {
                        text.Span("An appeal may be lodged in the prescribed manner on the City’s online system");
                        if (appealCloseDate.HasValue)
                        {
                            text.Span(" on or before 15:00 on ");
                            text.Span(appealCloseDate.Value.ToString(
                                "dd MMMM yyyy",
                                CultureInfo.GetCultureInfo("en-ZA"))).Bold();
                        }
                        text.Span(".");
                    });
                    column.Item().Text("Municipal Valuer").Bold();
                    column.Item().Text("City of Johannesburg Valuation Services");
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
            ? "Revised Municipal Valuer’s Decision"
            : "Municipal Valuer’s Decision";

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(110);
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            HeaderCell(table, string.Empty);
            HeaderCell(table, $"Entry in {rollName}");
            HeaderCell(table, decisionHeading);

            AddDecisionRow(table, "Category", row.GvCategory, row.MvdCategory);
            AddDecisionRow(table, "Extent", FormatExtent(row.GvExtent), FormatExtent(row.MvdExtent));
            AddDecisionRow(table, "Market Value", FormatRand(row.GvMarketValue), FormatRand(row.MvdMarketValue));
            AddDecisionRow(table, "Effective Date", string.Empty, FormatDate(row.EffectiveDate));

            if (HasSecondDecision(row))
            {
                AddDecisionRow(table, "Category Split 1", row.GvCategory2, row.MvdCategory2);
                AddDecisionRow(table, "Extent Split 1", FormatExtent(row.GvExtent2), FormatExtent(row.MvdExtent2));
                AddDecisionRow(table, "Market Value Split 1", FormatRand(row.GvMarketValue2), FormatRand(row.MvdMarketValue2));
            }

            if (HasThirdDecision(row))
            {
                AddDecisionRow(table, "Category Split 2", row.GvCategory3, row.MvdCategory3);
                AddDecisionRow(table, "Extent Split 2", FormatExtent(row.GvExtent3), FormatExtent(row.MvdExtent3));
                AddDecisionRow(table, "Market Value Split 2", FormatRand(row.GvMarketValue3), FormatRand(row.MvdMarketValue3));
            }
        });
    }

    private static void HeaderCell(TableDescriptor table, string value) =>
        table.Cell().Border(1).Background(Color.FromRGB(70, 130, 180))
            .Padding(5).Text(value).Bold().FontColor(Colors.White);

    private static void AddDecisionRow(
        TableDescriptor table,
        string label,
        string? gv,
        string? decision)
    {
        table.Cell().Border(1).Padding(5).Text(label).Bold();
        table.Cell().Border(1).Padding(5).Text(gv ?? string.Empty);
        table.Cell().Border(1).Padding(5).Text(decision ?? string.Empty);
    }

    private static void AddLine(ColumnDescriptor column, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            column.Item().Text(value.Trim());
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

    private static string FormatRand(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var clean = value.Replace("R", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(",", string.Empty).Trim();
        return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
            ? "R " + amount.ToString("N0", CultureInfo.GetCultureInfo("en-ZA"))
            : value.Trim();
    }

    private static string FormatExtent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var clean = value.Replace(",", string.Empty).Trim();
        return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var extent)
            ? extent.ToString("N2", CultureInfo.GetCultureInfo("en-ZA"))
            : value.Trim();
    }

    private static string FormatDate(DateTime? value) =>
        value?.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("en-ZA")) ?? string.Empty;

    private static string GetRollName(string rollSource) => rollSource switch
    {
        "Objection" => "General Valuation Roll 2023",
        "Objection_Supp1" => "Supplementary Roll 1",
        "Objection_Supp2" => "Supplementary Roll 2",
        "Objection_Supp3" => "Supplementary Roll 3",
        "Objection_Supp4" => "Supplementary Roll 4",
        "Objection_Supp5" => "Supplementary Roll 5",
        _ => "Valuation Roll"
    };

    private static string SanitiseFilePart(string? value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string((value ?? "Section53")
            .Where(character => !invalid.Contains(character))
            .ToArray());
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
