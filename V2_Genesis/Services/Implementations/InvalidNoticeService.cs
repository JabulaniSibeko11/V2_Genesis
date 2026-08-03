using Dapper;
using Microsoft.Data.SqlClient;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using V2_Genesis.Services.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public sealed class InvalidNoticeService : IInvalidNoticeService
{
    private const string InvalidObjectionStatus = "Notice-Sent-Invalid-Objection";
    private const string InvalidOmissionStatus = "Notice-Sent-Invalid-Omission";

    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<InvalidNoticeService> _logger;

    public InvalidNoticeService(
        IConfiguration config,
        IWebHostEnvironment environment,
        ILogger<InvalidNoticeService> logger)
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
                n.ID                         AS Id,
                n.OBJECTION_NO               AS ObjectionNo,
                n.PREMISE_ID                 AS PremiseId,
                n.VALUATION_KEY              AS ValuationKey,
                n.PROPETY_DESC               AS PropertyDescription,
                n.OWNER_NAME                 AS OwnerName,
                n.OWNER_ADDR1                AS OwnerAddr1,
                n.OWNER_ADDR2                AS OwnerAddr2,
                n.OWNER_ADDR3                AS OwnerAddr3,
                n.OWNER_ADDR4                AS OwnerAddr4,
                n.OWNER_ADDR5                AS OwnerAddr5,
                n.OWNER_EMAIL                AS OwnerEmail,
                n.OBJECTOR_NAME              AS ObjectorName,
                n.OBJECTOR_ADDR1             AS ObjectorAddr1,
                n.OBJECTOR_ADDR2             AS ObjectorAddr2,
                n.OBJECTOR_ADDR3             AS ObjectorAddr3,
                n.OBJECTOR_ADDR4             AS ObjectorAddr4,
                n.OBJECTOR_ADDR5             AS ObjectorAddr5,
                n.OBJECTOR_EMAIL             AS ObjectorEmail,
                n.REP_NAME                   AS RepresentativeName,
                n.REP_ADDR1                  AS RepresentativeAddr1,
                n.REP_ADDR2                  AS RepresentativeAddr2,
                n.REP_ADDR3                  AS RepresentativeAddr3,
                n.REP_ADDR4                  AS RepresentativeAddr4,
                n.REP_ADDR5                  AS RepresentativeAddr5,
                n.REP_EMAIL                  AS RepresentativeEmail,
                n.BATCH_NAME                 AS BatchName,
                n.BATCH_DATE                 AS BatchDate,
                n.LETTER_DATE                AS LetterDate,
                n.SENT_STATUS                AS SentStatus,
                n.SENT_DATE                  AS SentDate,
                n.NOTICE_KIND                AS NoticeKind,
                p.Objector_Type              AS ObjectorType,
                LTRIM(RTRIM(p.objection_Status)) AS ObjectionStatus
            FROM dbo.InvalidNoticeTable AS n
            INNER JOIN dbo.Obj_Property_Info AS p
                ON LTRIM(RTRIM(p.Objection_No)) = LTRIM(RTRIM(n.OBJECTION_NO))
            WHERE LTRIM(RTRIM(n.OBJECTION_NO)) = @ObjectionNo
              AND p.UserID = @UserId
              AND LTRIM(RTRIM(p.objection_Status)) IN
                  ('Notice-Sent-Invalid-Objection', 'Notice-Sent-Invalid-Omission')
            ORDER BY n.ID DESC;
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            sql,
            new { ObjectionNo = objectionNo, UserId = userId },
            commandType: CommandType.Text,
            commandTimeout: 60,
            cancellationToken: cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<InvalidNoticeRow>(command);
        if (row is null)
            throw new KeyNotFoundException(
                "The invalid objection notice was not found for this account.");

        var kind = ResolveKind(row.NoticeKind);
        ValidateKindMatchesStatus(kind, row.ObjectionStatus);
        var recipient = ResolveRecipient(row);
        var pdf = BuildPdf(row, recipient, kind);
        var safeReference = SanitiseFilePart(row.ObjectionNo);
        var publicType = kind == InvalidNoticeKind.InvalidOmission
            ? "Invalid_Omission_Notice"
            : "Invalid_Objection_Notice";

        _logger.LogInformation(
            "Generated {NoticeKind} for {ObjectionNo} on {RollSource}",
            kind,
            row.ObjectionNo,
            rollSource);

        return (pdf, $"{safeReference}_{publicType}.pdf");
    }

    private byte[] BuildPdf(
        InvalidNoticeRow row,
        NoticeRecipient recipient,
        InvalidNoticeKind kind)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var letterDate = row.LetterDate ?? row.BatchDate ?? row.SentDate ?? DateTime.Today;
        var headerPath = Path.Combine(
            _environment.WebRootPath,
            "Images",
            "Obj_Header.PNG");
        var title = kind == InvalidNoticeKind.InvalidOmission
            ? "INVALID OMISSION OBJECTION"
            : "INVALID OBJECTION";
        var explanation = kind == InvalidNoticeKind.InvalidOmission
            ? "Please be advised that the objection submitted cannot be considered. The records indicate that the objection was lodged against the incorrect property description."
            : "Please be advised that the objection submitted cannot be considered. The records indicate that the objection was lodged against a property description or property that does not exist on the official applicable property register.";

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
                    column.Spacing(7);

                    if (File.Exists(headerPath))
                        column.Item().Height(95).AlignCenter()
                            .Image(headerPath, ImageScaling.FitArea);

                    column.Item().Row(addressRow =>
                    {
                        addressRow.RelativeItem().Column(address =>
                        {
                            AddAddressLine(address, recipient.Name, true);
                            foreach (var line in recipient.AddressLines)
                                AddAddressLine(address, line);
                        });
                        addressRow.ConstantItem(180).AlignRight()
                            .Text(letterDate.ToString("dd MMMM yyyy"));
                    });

                    column.Item().PaddingTop(6).AlignCenter()
                        .Text("CITY OF JOHANNESBURG").FontSize(12).Bold();
                    column.Item().AlignCenter().Text(title).FontSize(10).Bold();
                    column.Item().LineHorizontal(1.5f);
                    column.Item().PaddingTop(4)
                        .Text(string.IsNullOrWhiteSpace(recipient.Name)
                            ? "Dear Sir/Madam"
                            : $"Dear {recipient.Name}")
                        .Bold();

                    column.Item().PaddingTop(4).Text(text =>
                    {
                        text.Span("Objection number: ").Bold();
                        text.Span(row.ObjectionNo ?? string.Empty);
                    });
                    column.Item().Text(text =>
                    {
                        text.Span("Property Description: ").Bold();
                        text.Span(row.PropertyDescription ?? string.Empty);
                    });
                    column.Item().PaddingTop(4).Text(explanation).Justify();
                    column.Item().Text(
                        "As a result, a Section 53 notice will not be issued because the objection is not valid for the reason stated above.")
                        .Bold().Justify();

                    Bullet(column,
                        "If you wish to lodge an objection using the applicable property details, ensure that the correct property description is used.");

                    column.Item().PaddingTop(8)
                        .Text("For enquiries: 011 407-6622 | valuationenquiries@joburg.org.za")
                        .Bold();
                    column.Item().PaddingTop(10).Text("S. Faiaz").Bold();
                    column.Item().Text("Municipal Valuer").Bold();
                });
            });
        }).GeneratePdf();
    }

    private static InvalidNoticeKind ResolveKind(string? value)
    {
        var normalised = value?.Trim().Replace(" ", string.Empty)
            .Replace("-", string.Empty).Replace("_", string.Empty);

        if (normalised?.Contains("omission", StringComparison.OrdinalIgnoreCase) == true)
            return InvalidNoticeKind.InvalidOmission;
        if (normalised?.Contains("objection", StringComparison.OrdinalIgnoreCase) == true)
            return InvalidNoticeKind.InvalidObjection;

        throw new InvalidOperationException(
            "The notice kind is missing or unsupported in InvalidNoticeTable.");
    }

    private static void ValidateKindMatchesStatus(
        InvalidNoticeKind kind,
        string? status)
    {
        var expectedStatus = kind == InvalidNoticeKind.InvalidOmission
            ? InvalidOmissionStatus
            : InvalidObjectionStatus;

        if (!string.Equals(status?.Trim(), expectedStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "The invalid notice type does not match the objection status.");
    }

    private static NoticeRecipient ResolveRecipient(InvalidNoticeRow row)
    {
        var type = row.ObjectorType ?? string.Empty;

        if (type.Contains("Representative", StringComparison.OrdinalIgnoreCase) &&
            HasRecipient(row.RepresentativeName, row.RepresentativeAddr1))
        {
            return Recipient(row.RepresentativeName,
                row.RepresentativeAddr1, row.RepresentativeAddr2,
                row.RepresentativeAddr3, row.RepresentativeAddr4,
                row.RepresentativeAddr5);
        }

        if (type.Contains("Third", StringComparison.OrdinalIgnoreCase) &&
            HasRecipient(row.ObjectorName, row.ObjectorAddr1))
        {
            return Recipient(row.ObjectorName,
                row.ObjectorAddr1, row.ObjectorAddr2, row.ObjectorAddr3,
                row.ObjectorAddr4, row.ObjectorAddr5);
        }

        if (HasRecipient(row.OwnerName, row.OwnerAddr1))
            return Recipient(row.OwnerName,
                row.OwnerAddr1, row.OwnerAddr2, row.OwnerAddr3,
                row.OwnerAddr4, row.OwnerAddr5);

        if (HasRecipient(row.ObjectorName, row.ObjectorAddr1))
            return Recipient(row.ObjectorName,
                row.ObjectorAddr1, row.ObjectorAddr2, row.ObjectorAddr3,
                row.ObjectorAddr4, row.ObjectorAddr5);

        return Recipient(row.RepresentativeName,
            row.RepresentativeAddr1, row.RepresentativeAddr2,
            row.RepresentativeAddr3, row.RepresentativeAddr4,
            row.RepresentativeAddr5);
    }

    private static bool HasRecipient(string? name, string? address) =>
        !string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(address);

    private static NoticeRecipient Recipient(
        string? name,
        params string?[] addressLines) => new(
            name?.Trim() ?? string.Empty,
            addressLines.Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line!.Trim()).Take(5).ToList());

    private static void AddAddressLine(
        ColumnDescriptor column,
        string? value,
        bool bold = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var text = column.Item().Text(value.Trim());
        if (bold) text.Bold();
    }

    private static void Bullet(ColumnDescriptor column, string value)
    {
        column.Item().PaddingTop(4).Row(row =>
        {
            row.ConstantItem(12).Text("•");
            row.RelativeItem().Text(value).Justify();
        });
    }

    private static string SanitiseFilePart(string? value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string((value ?? "Objection")
            .Where(character => !invalid.Contains(character)).ToArray());
    }

    private enum InvalidNoticeKind
    {
        InvalidObjection,
        InvalidOmission
    }

    private sealed record NoticeRecipient(
        string Name,
        IReadOnlyList<string> AddressLines);

    private sealed class InvalidNoticeRow
    {
        public long Id { get; set; }
        public string? ObjectionNo { get; set; }
        public string? PremiseId { get; set; }
        public string? ValuationKey { get; set; }
        public string? PropertyDescription { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerAddr1 { get; set; }
        public string? OwnerAddr2 { get; set; }
        public string? OwnerAddr3 { get; set; }
        public string? OwnerAddr4 { get; set; }
        public string? OwnerAddr5 { get; set; }
        public string? OwnerEmail { get; set; }
        public string? ObjectorName { get; set; }
        public string? ObjectorAddr1 { get; set; }
        public string? ObjectorAddr2 { get; set; }
        public string? ObjectorAddr3 { get; set; }
        public string? ObjectorAddr4 { get; set; }
        public string? ObjectorAddr5 { get; set; }
        public string? ObjectorEmail { get; set; }
        public string? RepresentativeName { get; set; }
        public string? RepresentativeAddr1 { get; set; }
        public string? RepresentativeAddr2 { get; set; }
        public string? RepresentativeAddr3 { get; set; }
        public string? RepresentativeAddr4 { get; set; }
        public string? RepresentativeAddr5 { get; set; }
        public string? RepresentativeEmail { get; set; }
        public string? BatchName { get; set; }
        public DateTime? BatchDate { get; set; }
        public DateTime? LetterDate { get; set; }
        public string? SentStatus { get; set; }
        public DateTime? SentDate { get; set; }
        public string? NoticeKind { get; set; }
        public string? ObjectorType { get; set; }
        public string? ObjectionStatus { get; set; }
    }
}
