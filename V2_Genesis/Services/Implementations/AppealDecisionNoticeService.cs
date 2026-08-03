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

public sealed class AppealDecisionNoticeService : IAppealDecisionNoticeService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AppealDecisionNoticeService> _logger;

    public AppealDecisionNoticeService(
        IConfiguration config,
        IWebHostEnvironment environment,
        ILogger<AppealDecisionNoticeService> logger)
    {
        _config = config;
        _environment = environment;
        _logger = logger;
    }

    public async Task<(byte[] Pdf, string FileName)> GenerateAsync(
        string rollSource,
        string referenceNumber,
        string userId,
        CancellationToken cancellationToken = default)
    {
        rollSource = rollSource?.Trim() ?? string.Empty;
        referenceNumber = referenceNumber?.Trim() ?? string.Empty;
        userId = userId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(referenceNumber))
            throw new ArgumentException("The appeal reference number is required.");
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException();
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var roll))
            throw new KeyNotFoundException("The valuation roll is not supported.");

        var connectionString = _config.GetConnectionString(roll.ConnectionKey)
            ?? throw new InvalidOperationException(
                $"Connection string '{roll.ConnectionKey}' was not found.");

        const string sql = """
            SELECT TOP (1)
                d.Objection_No                AS ObjectionNo,
                d.Appeal_No                   AS AppealNo,
                d.ADDR1                       AS Addr1,
                d.ADDR2                       AS Addr2,
                d.ADDR3                       AS Addr3,
                d.ADDR4                       AS Addr4,
                d.ADDR5                       AS Addr5,
                d.Appeal_Type                 AS AppealType,
                d.Premise_iD                  AS PremiseId,
                d.Unit_Key                    AS UnitKey,
                d.valuation_Key               AS ValuationKey,
                d.Property_desc               AS PropertyDescription,
                d.Email                       AS Email,
                d.Batch_Date                  AS BatchDate,
                d.App_Market_Value            AS MarketValue,
                d.App_Market_Value2           AS MarketValue2,
                d.App_Market_Value3           AS MarketValue3,
                d.App_Extent                  AS Extent,
                d.App_Extent2                 AS Extent2,
                d.App_Extent3                 AS Extent3,
                d.App_Category                AS Category,
                d.App_Category2               AS Category2,
                d.App_Category3               AS Category3,
                d.Email_date                  AS EmailDate,
                d.ERF                         AS Erf,
                d.PTN                         AS Portion,
                d.RE                          AS Remainder,
                d.Town                        AS Town,
                d.A_UserID                    AS DecisionUserId,
                d.Notice_Status               AS NoticeStatus,
                LTRIM(RTRIM(a.Appeal_Status)) AS AppealStatus
            FROM dbo.Appeal_Decision AS d
            INNER JOIN dbo.Obj_Property_Info_Appeal AS a
                ON (
                    LTRIM(RTRIM(a.Appeal_No)) = LTRIM(RTRIM(d.Appeal_No))
                    OR LTRIM(RTRIM(a.Obj_Ref)) = LTRIM(RTRIM(d.Objection_No))
                    OR LTRIM(RTRIM(a.Objection_No)) = LTRIM(RTRIM(d.Objection_No))
                )
            WHERE (
                    LTRIM(RTRIM(d.Appeal_No)) = @ReferenceNumber
                    OR LTRIM(RTRIM(d.Objection_No)) = @ReferenceNumber
                  )
              AND a.A_UserID = @UserId
              AND LTRIM(RTRIM(a.Appeal_Status)) = 'App-Finalized'
            ORDER BY COALESCE(d.Email_date, d.Batch_Date) DESC;
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            sql,
            new { ReferenceNumber = referenceNumber, UserId = userId },
            commandType: CommandType.Text,
            commandTimeout: 60,
            cancellationToken: cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<AppealDecisionRow>(command);
        if (row is null)
            throw new KeyNotFoundException(
                "The final appeal outcome was not found for this account.");

        var isSection52Review = string.Equals(
            row.DecisionUserId?.Trim(),
            "System_Generated",
            StringComparison.OrdinalIgnoreCase);
        var pdf = BuildPdf(row, isSection52Review);
        var safeReference = SanitiseFilePart(
            isSection52Review ? row.ObjectionNo : row.AppealNo ?? row.ObjectionNo);
        var publicType = isSection52Review
            ? "Section52_Review_Decision"
            : "Appeal_Decision";

        _logger.LogInformation(
            "Generated {DecisionType} for {ReferenceNumber} on {RollSource}",
            publicType,
            referenceNumber,
            rollSource);

        return (pdf, $"{safeReference}_{publicType}.pdf");
    }

    private byte[] BuildPdf(AppealDecisionRow row, bool isSection52Review)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var letterDate = row.EmailDate ?? row.BatchDate ?? DateTime.Today;
        var headerPath = Path.Combine(
            _environment.WebRootPath,
            "Images",
            "Obj_Header.PNG");
        var title = isSection52Review
            ? "VALUATION APPEAL BOARD: OUTCOME – SECTION 52 REVIEW DECISION FOR THE GENERAL VALUATION ROLL 2023"
            : "VALUATION APPEAL BOARD: OUTCOME – APPEAL DECISION FOR THE GENERAL VALUATION ROLL 2023";
        var shortTitle = isSection52Review
            ? "SECTION 52 REVIEW DECISION"
            : "APPEAL DECISION";
        var recipient = row.Addr1?.Trim();
        var greeting = string.IsNullOrWhiteSpace(recipient)
            ? "Dear Sir/Madam"
            : $"Dear {recipient}";

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginLeft(30);
                page.MarginRight(30);
                page.MarginTop(10);
                page.MarginBottom(10);
                page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(9));

                page.Footer().PaddingTop(8).AlignCenter().Text(text =>
                {
                    text.Line("_______________________________________________")
                        .FontSize(7).FontColor(Colors.Grey.Darken2);
                    text.Line("This is an official document generated by the City of Johannesburg Valuation Services Department")
                        .FontSize(7).FontColor(Colors.Grey.Darken2);
                    text.Line($"Generated on: {letterDate:dd MMMM yyyy}")
                        .FontSize(7).FontColor(Colors.Grey.Darken2);
                    text.Line(row.ValuationKey ?? string.Empty)
                        .FontSize(7).Bold().FontColor(Colors.Red.Medium);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(6);

                    if (File.Exists(headerPath))
                        column.Item().Image(headerPath, ImageScaling.FitWidth);

                    column.Item().PaddingTop(6).Row(addressRow =>
                    {
                        addressRow.RelativeItem().Column(address =>
                        {
                            AddLine(address, row.Addr1);
                            AddLine(address, row.Addr2);
                            AddLine(address, row.Addr3);
                            AddLine(address, row.Addr4);
                            AddLine(address, row.Addr5);
                            if (!string.IsNullOrWhiteSpace(row.Email))
                                address.Item().PaddingTop(5).Text(row.Email.Trim()).Bold();
                        });
                        addressRow.ConstantItem(180).AlignRight()
                            .Text(letterDate.ToString(
                                "dd MMMM yyyy",
                                CultureInfo.GetCultureInfo("en-ZA")));
                    });

                    column.Item().PaddingTop(5).AlignCenter()
                        .Text(shortTitle).FontSize(12).Bold();
                    column.Item().AlignCenter().Text(title).Bold();
                    column.Item().LineHorizontal(1.5f).LineColor(Colors.Grey.Darken2);
                    column.Item().PaddingTop(3).Text(greeting).Bold();
                    column.Item().Text(
                        "With reference to the above matter, the City advises that the Valuation Appeal Board has resolved the matter relating to the property below:")
                        .Justify();
                    column.Item().Text(text =>
                    {
                        text.Span("Property Description: ").Bold();
                        text.Span(row.PropertyDescription ?? string.Empty);
                    });

                    column.Item().Element(container =>
                        BuildPropertyTable(container, row, isSection52Review));

                    column.Item().PaddingTop(3)
                        .Text("Resolved inter alia as follows:").Bold();
                    column.Item().Element(container => BuildDecisionTable(container, row));

                    column.Item().PaddingTop(5).Text("Please note the following:").Bold();
                    Bullet(column,
                        "The decision will be implemented on the Land Information System within 30 days.");
                    Bullet(column,
                        "Written reasons may be requested within 30 days from the date of this letter by emailing valuationenquiries@joburg.org.za.");
                    Bullet(column,
                        "A person aggrieved by this decision may take the matter on review to the High Court of South Africa at their own cost.");

                    column.Item().PaddingTop(10).Text("Regards,").Bold();
                    column.Item().PaddingTop(8)
                        .Text("SECRETARY: VALUATION APPEAL BOARD").Bold();
                    column.Item().Text("CITY OF JOHANNESBURG").Bold();
                });
            });
        }).GeneratePdf();
    }

    private static void BuildPropertyTable(
        IContainer container,
        AppealDecisionRow row,
        bool isSection52Review)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                if (!isSection52Review) columns.RelativeColumn();
            });

            HeaderCell(table, "SUBURB");
            HeaderCell(table, "ERF NUMBER");
            HeaderCell(table, "PORTION");
            HeaderCell(table, "RE");
            HeaderCell(table, "OBJECTION NO");
            if (!isSection52Review) HeaderCell(table, "APPEAL NO");

            BodyCell(table, row.Town);
            BodyCell(table, row.Erf);
            BodyCell(table, row.Portion);
            BodyCell(table, row.Remainder);
            BodyCell(table, row.ObjectionNo);
            if (!isSection52Review) BodyCell(table, row.AppealNo);
        });
    }

    private static void BuildDecisionTable(IContainer container, AppealDecisionRow row)
    {
        var decisions = new[]
        {
            (row.Category, row.Extent, row.MarketValue),
            (row.Category2, row.Extent2, row.MarketValue2),
            (row.Category3, row.Extent3, row.MarketValue3)
        }.Where(decision =>
            !string.IsNullOrWhiteSpace(decision.Item1) ||
            !string.IsNullOrWhiteSpace(decision.Item2) ||
            !string.IsNullOrWhiteSpace(decision.Item3)).ToList();

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            HeaderCell(table, "Property Category");
            HeaderCell(table, "Area/m²");
            HeaderCell(table, "Market Value");

            if (decisions.Count == 0)
            {
                BodyCell(table, string.Empty);
                BodyCell(table, string.Empty);
                BodyCell(table, string.Empty);
            }
            else
            {
                foreach (var decision in decisions)
                {
                    BodyCell(table, decision.Item1);
                    BodyCell(table, FormatExtent(decision.Item2));
                    BodyCell(table, FormatRand(decision.Item3));
                }
            }
        });
    }

    private static void HeaderCell(TableDescriptor table, string value) =>
        table.Cell().Border(1).Background(Color.FromRGB(70, 130, 180))
            .Padding(4).Text(value).Bold().FontColor(Colors.White);

    private static void BodyCell(TableDescriptor table, string? value) =>
        table.Cell().Border(1).Padding(4).Text(value?.Trim() ?? string.Empty);

    private static void AddLine(ColumnDescriptor column, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            column.Item().Text(value.Trim());
    }

    private static void Bullet(ColumnDescriptor column, string value)
    {
        column.Item().PaddingTop(4).Row(row =>
        {
            row.ConstantItem(12).Text("•");
            row.RelativeItem().Text(value);
        });
    }

    private static string FormatRand(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var clean = new string(value
            .Replace("R", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(",", string.Empty)
            .Where(character => char.IsDigit(character) || character is '.' or '-')
            .ToArray());
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

    private static string SanitiseFilePart(string? value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string((value ?? "Appeal")
            .Where(character => !invalid.Contains(character)).ToArray());
    }

    private sealed class AppealDecisionRow
    {
        public string? ObjectionNo { get; set; }
        public string? AppealNo { get; set; }
        public string? Addr1 { get; set; }
        public string? Addr2 { get; set; }
        public string? Addr3 { get; set; }
        public string? Addr4 { get; set; }
        public string? Addr5 { get; set; }
        public string? AppealType { get; set; }
        public string? PremiseId { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PropertyDescription { get; set; }
        public string? Email { get; set; }
        public DateTime? BatchDate { get; set; }
        public string? MarketValue { get; set; }
        public string? MarketValue2 { get; set; }
        public string? MarketValue3 { get; set; }
        public string? Extent { get; set; }
        public string? Extent2 { get; set; }
        public string? Extent3 { get; set; }
        public string? Category { get; set; }
        public string? Category2 { get; set; }
        public string? Category3 { get; set; }
        public DateTime? EmailDate { get; set; }
        public string? Erf { get; set; }
        public string? Portion { get; set; }
        public string? Remainder { get; set; }
        public string? Town { get; set; }
        public string? DecisionUserId { get; set; }
        public string? NoticeStatus { get; set; }
        public string? AppealStatus { get; set; }
    }
}
