using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using V2_Genesis.Services.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public sealed class DearJohnnyNoticeService : IDearJohnnyNoticeService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DearJohnnyNoticeService> _logger;

    public DearJohnnyNoticeService(
        IConfiguration config,
        IWebHostEnvironment environment,
        ILogger<DearJohnnyNoticeService> logger)
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

        await using var db = new DearJohnnyReadDbContext(connectionString);

        var row = await (
            from notice in db.DearJohnnyNotices.AsNoTracking()
            join objection in db.Objections.AsNoTracking()
                on (notice.ObjectionNo ?? string.Empty).Trim()
                equals (objection.ObjectionNo ?? string.Empty).Trim()
            where (notice.ObjectionNo ?? string.Empty).Trim() == objectionNo
                && (allowAdministrativeAccess ||
                    (objection.UserId ?? string.Empty).Trim() == userId)
                && (objection.ObjectionStatus ?? string.Empty).Trim() ==
                    "Notice-Sent-Dear-Johnny"
            orderby notice.Id descending
            select new DearJohnnyRow
            {
                Id = notice.Id,
                ObjectionNo = notice.ObjectionNo,
                ObjectorType = objection.ObjectorType,
                PropertyDescription = objection.PropertyDescription,
                ObjectorName = notice.ObjectorName,
                ObjectorEmail = notice.ObjectorEmail,
                LetterDate = notice.LetterDate,
                ValuationKey = notice.ValuationKey,
                BatchName = notice.BatchName,
                BatchDate = notice.BatchDate,
                ObjectionStatus = objection.ObjectionStatus
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            throw new KeyNotFoundException(
                "The objection outcome notice was not found for this account.");

        var pdf = BuildPdf(row, GetRollName(rollSource));
        var safeReference = SanitiseFilePart(row.ObjectionNo);

        _logger.LogInformation(
            "Generated previous-valuation-process outcome notice for {ObjectionNo} on {RollSource}",
            row.ObjectionNo,
            rollSource);

        return (pdf, $"{safeReference}_Objection_Outcome_Notice.pdf");
    }

    private byte[] BuildPdf(DearJohnnyRow row, string rollName)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var letterDate = row.LetterDate ?? row.BatchDate ?? DateTime.Today;
        var headerPath = Path.Combine(
            _environment.WebRootPath,
            "Images",
            "Obj_Header.PNG");
        var fullName = string.Join(" ", new[] { row.ObjectorName, row.ObjectorSurname }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim()));
        var greeting = string.IsNullOrWhiteSpace(fullName)
            ? "Dear Sir/Madam"
            : $"Dear {fullName}";

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
                            AddAddressLine(address, fullName, true);
                            AddAddressLine(address, row.ObjectorAddress);
                        });
                        addressRow.ConstantItem(180).AlignRight()
                            .Text(letterDate.ToString("dd MMMM yyyy"));
                    });

                    column.Item().PaddingTop(6).AlignCenter()
                        .Text("CITY OF JOHANNESBURG").FontSize(12).Bold();
                    column.Item().AlignCenter()
                        .Text("OBJECTION OUTCOME NOTICE").FontSize(10).Bold();
                    column.Item().LineHorizontal(1.5f);
                    column.Item().PaddingTop(4).Text(greeting).Bold();

                    column.Item().PaddingTop(4).Text(text =>
                    {
                        text.Span("Property Description: ").Bold();
                        text.Span(row.PropertyDescription ?? string.Empty);
                    });

                    column.Item().Text(text =>
                    {
                        text.Span("Objection number ");
                        text.Span(row.ObjectionNo ?? string.Empty).Bold();
                        text.Span(" received for the above property was not considered because the property was subject to a previous legislative process relating to the General Valuation Roll 2023, which was concluded before ");
                        text.Span(rollName).Bold();
                        text.Span(". The property had previously been considered through an objection, section 52 review or appeal process.");
                    });

                    column.Item().PaddingTop(4)
                        .Text("A Section 53 notice will not be issued because this objection is not valid for the reason stated above.")
                        .Bold().Justify();

                    Bullet(column,
                        "The owner or an authorised representative may submit an application for condonation for a late appeal, together with written reasons for the late submission.");
                    Bullet(column,
                        "Late-appeal condonation applications are considered by the Committee. Approval is not guaranteed and remains at the Committee’s discretion.");
                    Bullet(column,
                        "Applications may be emailed to valuationenquiries@joburg.org.za or delivered to Valuation Administration, 1st Floor, Jorissen Place, 66 Jorissen Street, Braamfontein.");

                    column.Item().PaddingTop(8)
                        .Text("For enquiries: 011 407-6622 | valuationenquiries@joburg.org.za")
                        .Bold();
                    column.Item().PaddingTop(10).Text("S. Faiaz").Bold();
                    column.Item().Text("Municipal Valuer").Bold();
                });
            });
        }).GeneratePdf();
    }

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

    private static string GetRollName(string rollSource) => rollSource switch
    {
        "Objection" => "General Valuation Roll 2023",
        "Objection_Supp1" => "Supplementary Roll 1",
        "Objection_Supp2" => "Supplementary Roll 2",
        "Objection_Supp3" => "Supplementary Roll 3",
        "Objection_Supp4" => "Supplementary Roll 4",
        "Objection_Supp5" => "Supplementary Roll 5",
        _ => "the current valuation roll"
    };

    private static string SanitiseFilePart(string? value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string((value ?? "Objection")
            .Where(character => !invalid.Contains(character))
            .ToArray());
    }

    private sealed class DearJohnnyReadDbContext : DbContext
    {
        private readonly string _connectionString;

        public DearJohnnyReadDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbSet<DearJohnnyEntity> DearJohnnyNotices =>
            Set<DearJohnnyEntity>();

        public DbSet<DearJohnnyObjectionEntity> Objections =>
            Set<DearJohnnyObjectionEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                _connectionString,
                sqlServer => sqlServer.CommandTimeout(60));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DearJohnnyEntity>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.ToTable("DearJohnnyTable", "dbo");
                entity.Property(x => x.Id).HasColumnName("Id");
                entity.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
                entity.Property(x => x.ObjectorName).HasColumnName("Objector_Name");
                entity.Property(x => x.ObjectorEmail).HasColumnName("Objector_Email");
                entity.Property(x => x.LetterDate).HasColumnName("Letter_Date");
                entity.Property(x => x.ValuationKey).HasColumnName("Valuation_Key");
                entity.Property(x => x.BatchName).HasColumnName("Batch_Name");
                entity.Property(x => x.BatchDate).HasColumnName("Batch_Date");
            });

            modelBuilder.Entity<DearJohnnyObjectionEntity>(entity =>
            {
                entity.HasKey(x => x.ObjectionId);
                entity.ToTable("Obj_Property_Info", "dbo");
                entity.Property(x => x.ObjectionId).HasColumnName("Objection_ID");
                entity.Property(x => x.ObjectionNo).HasColumnName("Objection_No");
                entity.Property(x => x.UserId).HasColumnName("UserID");
                entity.Property(x => x.ObjectionStatus).HasColumnName("objection_Status");
                entity.Property(x => x.ObjectorType).HasColumnName("Objector_Type");
                entity.Property(x => x.PropertyDescription).HasColumnName("Property_Desc");
            });
        }
    }

    private sealed class DearJohnnyEntity
    {
        public long Id { get; set; }
        public string? ObjectionNo { get; set; }
        public string? ObjectorName { get; set; }
        public string? ObjectorEmail { get; set; }
        public DateTime? LetterDate { get; set; }
        public string? ValuationKey { get; set; }
        public string? BatchName { get; set; }
        public DateTime? BatchDate { get; set; }
    }

    private sealed class DearJohnnyObjectionEntity
    {
        public long ObjectionId { get; set; }
        public string? ObjectionNo { get; set; }
        public string? UserId { get; set; }
        public string? ObjectionStatus { get; set; }
        public string? ObjectorType { get; set; }
        public string? PropertyDescription { get; set; }
    }

    private sealed class DearJohnnyRow
    {
        public long Id { get; set; }
        public string? ObjectionNo { get; set; }
        public string? ObjectorType { get; set; }
        public string? PropertyDescription { get; set; }
        public string? ObjectorName { get; set; }
        public string? ObjectorSurname { get; set; }
        public string? ObjectorAddress { get; set; }
        public string? ObjectorEmail { get; set; }
        public DateTime? LetterDate { get; set; }
        public string? ValuationKey { get; set; }
        public string? BatchName { get; set; }
        public DateTime? BatchDate { get; set; }
        public string? ObjectionStatus { get; set; }
    }
}
