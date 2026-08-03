using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

        await using var db = new InvalidNoticeReadDbContext(connectionString);

        var row = await (
            from notice in db.InvalidNotices.AsNoTracking()
            join objection in db.Objections.AsNoTracking()
                on (notice.ObjectionNo ?? string.Empty).Trim()
                equals (objection.ObjectionNo ?? string.Empty).Trim()
            where (notice.ObjectionNo ?? string.Empty).Trim() == objectionNo
                && (objection.UserId ?? string.Empty).Trim() == userId
                && ((objection.ObjectionStatus ?? string.Empty).Trim() == InvalidObjectionStatus
                    || (objection.ObjectionStatus ?? string.Empty).Trim() == InvalidOmissionStatus)
            orderby notice.Id descending
            select new InvalidNoticeRow
            {
                Id = notice.Id,
                ObjectionNo = notice.ObjectionNo,
                PremiseId = notice.PremiseId,
                ValuationKey = notice.ValuationKey,
                PropertyDescription = notice.PropertyDescription,
                OwnerName = notice.OwnerName,
                OwnerAddr1 = notice.OwnerAddr1,
                OwnerAddr2 = notice.OwnerAddr2,
                OwnerAddr3 = notice.OwnerAddr3,
                OwnerAddr4 = notice.OwnerAddr4,
                OwnerAddr5 = notice.OwnerAddr5,
                OwnerEmail = notice.OwnerEmail,
                ObjectorName = notice.ObjectorName,
                ObjectorAddr1 = notice.ObjectorAddr1,
                ObjectorAddr2 = notice.ObjectorAddr2,
                ObjectorAddr3 = notice.ObjectorAddr3,
                ObjectorAddr4 = notice.ObjectorAddr4,
                ObjectorAddr5 = notice.ObjectorAddr5,
                ObjectorEmail = notice.ObjectorEmail,
                RepresentativeName = notice.RepresentativeName,
                RepresentativeAddr1 = notice.RepresentativeAddr1,
                RepresentativeAddr2 = notice.RepresentativeAddr2,
                RepresentativeAddr3 = notice.RepresentativeAddr3,
                RepresentativeAddr4 = notice.RepresentativeAddr4,
                RepresentativeAddr5 = notice.RepresentativeAddr5,
                RepresentativeEmail = notice.RepresentativeEmail,
                BatchName = notice.BatchName,
                BatchDate = notice.BatchDate,
                LetterDate = notice.LetterDate,
                SentStatus = notice.SentStatus,
                SentDate = notice.SentDate,
                NoticeKind = notice.NoticeKind,
                ObjectorType = objection.ObjectorType,
                ObjectionStatus = objection.ObjectionStatus
            })
            .FirstOrDefaultAsync(cancellationToken);
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

    private sealed class InvalidNoticeReadDbContext : DbContext
    {
        private readonly string _connectionString;

        public InvalidNoticeReadDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbSet<InvalidNoticeEntity> InvalidNotices =>
            Set<InvalidNoticeEntity>();

        public DbSet<InvalidNoticeObjectionEntity> Objections =>
            Set<InvalidNoticeObjectionEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                _connectionString,
                sqlServer => sqlServer.CommandTimeout(60));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvalidNoticeEntity>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.ToTable("InvalidNoticeTable", "dbo");
                entity.Property(x => x.Id).HasColumnName("ID");
                entity.Property(x => x.ObjectionNo).HasColumnName("OBJECTION_NO");
                entity.Property(x => x.PremiseId).HasColumnName("PREMISE_ID");
                entity.Property(x => x.ValuationKey).HasColumnName("VALUATION_KEY");
                entity.Property(x => x.PropertyDescription).HasColumnName("PROPETY_DESC");
                entity.Property(x => x.OwnerName).HasColumnName("OWNER_NAME");
                entity.Property(x => x.OwnerAddr1).HasColumnName("OWNER_ADDR1");
                entity.Property(x => x.OwnerAddr2).HasColumnName("OWNER_ADDR2");
                entity.Property(x => x.OwnerAddr3).HasColumnName("OWNER_ADDR3");
                entity.Property(x => x.OwnerAddr4).HasColumnName("OWNER_ADDR4");
                entity.Property(x => x.OwnerAddr5).HasColumnName("OWNER_ADDR5");
                entity.Property(x => x.OwnerEmail).HasColumnName("OWNER_EMAIL");
                entity.Property(x => x.ObjectorName).HasColumnName("OBJECTOR_NAME");
                entity.Property(x => x.ObjectorAddr1).HasColumnName("OBJECTOR_ADDR1");
                entity.Property(x => x.ObjectorAddr2).HasColumnName("OBJECTOR_ADDR2");
                entity.Property(x => x.ObjectorAddr3).HasColumnName("OBJECTOR_ADDR3");
                entity.Property(x => x.ObjectorAddr4).HasColumnName("OBJECTOR_ADDR4");
                entity.Property(x => x.ObjectorAddr5).HasColumnName("OBJECTOR_ADDR5");
                entity.Property(x => x.ObjectorEmail).HasColumnName("OBJECTOR_EMAIL");
                entity.Property(x => x.RepresentativeName).HasColumnName("REP_NAME");
                entity.Property(x => x.RepresentativeAddr1).HasColumnName("REP_ADDR1");
                entity.Property(x => x.RepresentativeAddr2).HasColumnName("REP_ADDR2");
                entity.Property(x => x.RepresentativeAddr3).HasColumnName("REP_ADDR3");
                entity.Property(x => x.RepresentativeAddr4).HasColumnName("REP_ADDR4");
                entity.Property(x => x.RepresentativeAddr5).HasColumnName("REP_ADDR5");
                entity.Property(x => x.RepresentativeEmail).HasColumnName("REP_EMAIL");
                entity.Property(x => x.BatchName).HasColumnName("BATCH_NAME");
                entity.Property(x => x.BatchDate).HasColumnName("BATCH_DATE");
                entity.Property(x => x.LetterDate).HasColumnName("LETTER_DATE");
                entity.Property(x => x.SentStatus).HasColumnName("SENT_STATUS");
                entity.Property(x => x.SentDate).HasColumnName("SENT_DATE");
                entity.Property(x => x.NoticeKind).HasColumnName("NOTICE_KIND");
            });

            modelBuilder.Entity<InvalidNoticeObjectionEntity>(entity =>
            {
                entity.HasKey(x => x.ObjectionId);
                entity.ToTable("Obj_Property_Info", "dbo");
                entity.Property(x => x.ObjectionId).HasColumnName("Objection_ID");
                entity.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
                entity.Property(x => x.UserId).HasColumnName("UserID");
                entity.Property(x => x.ObjectorType).HasColumnName("Objector_Type");
                entity.Property(x => x.ObjectionStatus).HasColumnName("objection_Status");
            });
        }
    }

    private sealed class InvalidNoticeEntity
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
    }

    private sealed class InvalidNoticeObjectionEntity
    {
        public long ObjectionId { get; set; }
        public string? ObjectionNo { get; set; }
        public string? UserId { get; set; }
        public string? ObjectorType { get; set; }
        public string? ObjectionStatus { get; set; }
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
