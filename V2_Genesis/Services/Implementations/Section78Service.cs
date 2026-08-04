using Dapper;
using QuestPDF.Fluent;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mime;
using System.Security.Cryptography;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Emails;
using V2_Genesis.Models.Results.Acknowledgement;
using V2_Genesis.Models.Results.Section78;
using V2_Genesis.Models.ViewModels.Section78;
using V2_Genesis.Services.Interfaces;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace V2_Genesis.Services.Implementations
{
    public class Section78Service : ISection78Service
    {
        private readonly IConfiguration _config;
        private readonly QueryDbContext _qdb;
        private readonly string _queryConn;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;
        private readonly ISubmittedFormPdfService _submittedFormPdfService;
        private readonly ILogger<Section78Service> _logger;
        private const string SP_DETAIL = "IndexObjection";
        private const string SP_LINKED = "DashboardLinkedQ";
        private const string SP_SUBMITTED = "DashboardObjectionQ";

        public Section78Service(IConfiguration config, QueryDbContext qdb, IWebHostEnvironment env, IEmailService emailService, ISubmittedFormPdfService submittedFormPdfService, ILogger<Section78Service> logger)
        {
            _config = config;
            _qdb = qdb;
            _env = env;
            _emailService = emailService;
            _submittedFormPdfService = submittedFormPdfService;
            _logger = logger;
            _queryConn = config.GetConnectionString("QueryConnection")
                ?? throw new InvalidOperationException(
                    "QueryConnection missing from appsettings.");
        }

        // ── Property detail ──────────────────────────────────────────────
        public async Task<Section78PropertyDetail?> GetPropertyDetailAsync(
            string unitKey, string? valuationKey)
        {
            try
            {
                await using var conn = new SqlConnection(_queryConn);
                return await conn.QueryFirstOrDefaultAsync<Section78PropertyDetail>(
                    SP_DETAIL,
                    new { UnitKey = unitKey, ValuationKey = valuationKey ?? "" },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[Section78] GetPropertyDetail failed: {ex.Message}");
                return null;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  SUBMIT — mirrors the V1 POST action logic, now in service layer
        // ════════════════════════════════════════════════════════════════
        public async Task<Section78SubmitResult> SubmitQueryAsync(
            // ── Query header ─────────────────────────────────────────
            Que_Property_InfoModel que,
            // ── Shared sections ──────────────────────────────────────
            Obj_Section1Model obj1,
            Obj_Section2Model obj2,
            Obj_Section2QueryModel que1,
            Obj_Section3ResModel objR3,
            Obj_Section3BusModel objB3,
            Obj_Section3AgriModel objA3,
            Obj_Section4BusModel objB4,
            Obj_Section4ResModel objR4,
            Obj_Section5Model obj5,
            Obj_Section6Model obj6,
            Obj_Section7Model obj7,
            Obj_Files obj_file,
            // ── File upload ──────────────────────────────────────────
            List<IFormFile> files,
            List<IFormFile> fileR,
            // ── Config ───────────────────────────────────────────────
            string reviewStat,
            string uploadRootPath,
            string propertyType,
            string userId)
        {
            bool isReview = reviewStat == "R";

            // ── 1. Query header ────────────────────────────────────────
            que.UserID = userId;
            que.Query_Status = "Que-Lodging";
            que.Sub_typ = isReview ? 1 : 0;

            _qdb.Que_Property_Info.Add(que);
            await _qdb.SaveChangesAsync();

            // ── Helper: build reference string ────────────────────────
            string Ref(long id) => isReview
       ? $"Que-GV23-{id}-R"
       : $"Que-GV23-{id}";

            string queryRef = Ref(que.Query_ID);

            // ── 2. Section 1 ───────────────────────────────────────────
            obj1.Ref = que.Query_ID;
            obj1.Objection_Ref_S1 = que.Query_No ?? queryRef;
            _qdb.Obj_Section1.Add(obj1);
            await _qdb.SaveChangesAsync();

            // ── 3. Section 2 ───────────────────────────────────────────
            obj2.Ref = que.Query_ID;
            obj2.Objection_Ref_S2 = queryRef;
            _qdb.Obj_Section2.Add(obj2);
            await _qdb.SaveChangesAsync();

            // ── 4. Section 2 Query (S78 checkboxes A-H) ───────────────
            que1.Ref = que.Query_ID;
            que1.Objection_Ref_SQ = queryRef;
            _qdb.Obj_Section2Query.Add(que1);
            await _qdb.SaveChangesAsync();

            // ── 5. Section 3 (all three property types) ───────────────
            objA3.Ref = que.Query_ID;
            objA3.Objection_Ref_SA3 = queryRef;
            _qdb.Obj_Section3Agri.Add(objA3);
            await _qdb.SaveChangesAsync();

            objB3.Ref = que.Query_ID;
            objB3.Objection_Ref_SB3 = queryRef;
            _qdb.Obj_Section3Bus.Add(objB3);
            await _qdb.SaveChangesAsync();

            objR3.Ref = que.Query_ID;
            objR3.Objection_Ref_SR3 = queryRef;
            _qdb.Obj_Section3Res.Add(objR3);
            await _qdb.SaveChangesAsync();

            // ── 6. Section 4 ───────────────────────────────────────────
            objB4.Ref = que.Query_ID;
            objB4.Objection_Ref_SB4 = queryRef;
            _qdb.Obj_Section4Bus.Add(objB4);
            await _qdb.SaveChangesAsync();

            objR4.Ref = que.Query_ID;
            objR4.Objection_Ref_SR4 = queryRef;
            _qdb.Obj_Section4Res.Add(objR4);
            await _qdb.SaveChangesAsync();

            // ── 7. Section 5 ───────────────────────────────────────────
            obj5.Ref = que.Query_ID;
            obj5.Objection_Ref_S5 = queryRef;
            _qdb.Obj_Section5.Add(obj5);
            await _qdb.SaveChangesAsync();

            // ── 8. Section 6 ───────────────────────────────────────────
            obj6.Ref = que.Query_ID;
            obj6.Objection_Ref_S6 = queryRef;
            _qdb.Obj_Section6.Add(obj6);
            await _qdb.SaveChangesAsync();

            // ── 9. Section 7 (declaration + signature) ─────────────────
            obj7.Ref = que.Query_ID;
            obj7.Objection_Ref_S7 = queryRef;
            obj7.RandomPin = GeneratePin();
            obj7.Section51Pin = GeneratePin();
            _qdb.Obj_Section7.Add(obj7);
            await _qdb.SaveChangesAsync();

            // ── 10. Files ──────────────────────────────────────────────
            string folder = Path.Combine(uploadRootPath, queryRef);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Rep letter
            foreach (var file in fileR)
            {
                var repFolder = Path.Combine(folder, "Rep_Letter");
                if (!Directory.Exists(repFolder))
                    Directory.CreateDirectory(repFolder);

                var name = Path.GetFileName(file.FileName);
                obj_file.Rep_letter = name;
                var path = Path.Combine(repFolder, name);
                await using var s = File.Create(path);
                await file.CopyToAsync(s);
            }

            // Evidence files
            int count = 0;
            foreach (var file in files)
            {
                count++;
                var name = Path.GetFileName(file.FileName);
                var path = Path.Combine(folder, name);

                // Assign to Files1..Files10
                SetFileSlot(obj_file, count, name);

                await using var s = File.Create(path);
                await file.CopyToAsync(s);
            }

            obj_file.Ref = que.Query_ID;
            obj_file.Objection_Ref_files = queryRef;
            obj_file.Evidence_count = count;
            _qdb.Obj_Files.Add(obj_file);
            await _qdb.SaveChangesAsync();

            // ── 11. Build result ───────────────────────────────────────────
            var result = new Section78SubmitResult
            {
                QueryRef = queryRef,
                QueryId = que.Query_ID,
                RandomPin = obj7.RandomPin,
                IsReview = isReview,
                IsMulti = propertyType == "Multi",
                ValuationKey= que.Valuation_Key,
                FileCount = count,
                Files = new[]
                {
                obj_file.Files1, obj_file.Files2, obj_file.Files3,
                obj_file.Files4, obj_file.Files5, obj_file.Files6,
                obj_file.Files7, obj_file.Files8, obj_file.Files9,
                obj_file.Files10
            },
                Section6 = obj6
            };

            // ── 12. Write acknowledgement PDF to disk ─────────────────────
            WriteAcknowledgement(result, uploadRootPath);

            // ── 13. Send acknowledgement email ────────────────────────────
            // ── 13. Generate submitted form PDF + send acknowledgement email ─────
            try
            {
                var folderPath = Path.Combine(uploadRootPath, result.QueryRef);

                var pdfPath = Path.Combine(
                    folderPath,
                    $"{result.QueryRef}_Acknowledgement.pdf");

                if (!File.Exists(pdfPath))
                {
                    _logger.LogWarning(
                        "[S78] Ack PDF not found at {Path} — email skipped.",
                        pdfPath);

                    return result;
                }

                var ackPdfBytes = await File.ReadAllBytesAsync(pdfPath);

                // Generate the Section 78 Query/Review form PDF and save it in the same folder
                var submittedFormPdf = await _submittedFormPdfService.GenerateSection78FormAsync(
                    result.IsReview,
                    folderPath,
                    que,
                    obj1,
                    obj2,
                    que1,
                    objR3,
                    objB3,
                    objA3,
                    objB4,
                    objR4,
                    obj5,
                    obj6,
                    obj7,
                    DateTime.Now);

                var extraAttachments = new List<EmailAttachment>
    {
        new EmailAttachment
        {
            FileName = submittedFormPdf.FileName,
            FileBytes = submittedFormPdf.PdfBytes,
            ContentType = MediaTypeNames.Application.Pdf
        }
    };

                await _emailService.SendSection78AcknowledgementAsync(
                    result.QueryRef,
                    result.IsReview,
                    ackPdfBytes,
                    folderPath,
                    extraAttachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[S78] Failed to generate submitted form PDF/send ack email for {Ref}",
                    result.QueryRef);
            }

            return result;
        }



        private void WriteAcknowledgement(
     Section78SubmitResult result,
     string uploadRootPath)
        {
            try
            {
                var folder = Path.Combine(uploadRootPath, result.QueryRef);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = $"{result.QueryRef}_Acknowledgement.pdf";
                var fullPath = Path.Combine(folder, fileName);

                var imgPath = !string.IsNullOrEmpty(_config["AppSettings:QueryHeaderImage"])
                    ? _config["AppSettings:QueryHeaderImage"]!
                    : Path.Combine(_env.WebRootPath, "Images", "Obj_Header.PNG");

                var hasImg = File.Exists(imgPath);

                var s6 = result.Section6;
                var typeWord = result.IsReview ? "REVIEW" : "QUERY";
                var typeWordTitle = result.IsReview ? "REVIEW" : "QUERY";
                var typeWordLower = result.IsReview ? "review" : "query";

                var now = DateTime.Now;
                var letterDate = now.ToString("dd MMMM yyyy");
                var generatedDate = now.ToString("dd MMMM yyyy HH:mm");

                QuestPDF.Settings.License = LicenseType.Community;

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(36);
                        page.DefaultTextStyle(x =>
                            x.FontFamily("Arial").FontSize(9));

                        page.Content().Column(col =>
                        {
                            col.Spacing(8);

                            // HEADER IMAGE
                            if (hasImg)
                            {
                                col.Item()
                                    .Height(80)
                                    .Image(imgPath, ImageScaling.FitArea);
                            }

                            // DATE
                            col.Item()
                                .AlignRight()
                                .Text(letterDate)
                                .FontSize(10)
                                .SemiBold();

                            // TITLE
                            col.Item()
                                .AlignCenter()
                                .Text("CITY OF JOHANNESBURG")
                                .FontSize(13)
                                .Bold();

                            col.Item()
                                .AlignCenter()
                                .Text($"SECTION 78 {typeWordTitle} ACKNOWLEDGEMENT")
                                .FontSize(12)
                                .Bold();

                            col.Item()
                                .BorderBottom(1)
                                .BorderColor("#555555");

                            // INTRO
                            col.Item()
                                .Text(
                                    $"This is to acknowledge that your Section 78 {typeWordLower} has been successfully received and logged. Details below for your records.")
                                .FontSize(9);

                            col.Item()
                                .Text("IMPORTANT NOTICE: You have 48 hours from submission to upload outstanding evidence.")
                                .FontSize(9)
                                .Bold();

                            // REFERENCE DETAILS
                            col.Item()
                                .AlignCenter()
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
                                    RefRow(refBox, "Property Description:", s6?.Old_Property_Description);
                                    RefRow(refBox, $"{typeWordTitle} Reference:", result.QueryRef);
                                    RefRow(refBox, "PIN:", result.RandomPin);
                                    RefRow(refBox, "Date Captured:", generatedDate);

                                    if (!string.IsNullOrWhiteSpace(result.ValuationKey))
                                        RefRow(refBox, "Valuation Key:", result.ValuationKey);
                                });

                            col.Item()
                                .BorderBottom(1)
                                .BorderColor("#555555");

                            // PROPERTY DETAILS AS LISTED
                            col.Item()
                                .AlignCenter()
                                .Text("PROPERTY DETAILS AS LISTED IN THE VALUATION ROLL")
                                .FontSize(10)
                                .Bold();

                            Section78PropertyTable(
                                col,
                                s6?.Old_Property_Description,
                                s6?.Old_Category,
                                s6?.Old_Address,
                                FormatMV(s6?.Old_Market_Value),
                                s6?.Old_Extent,
                                s6?.Old_Owner,
                                result.IsMulti,
                                s6?.Old2_Category,
                                FormatMV(s6?.Old2_Market_Value),
                                s6?.Old2_Extent,
                                s6?.Old3_Category,
                                FormatMV(s6?.Old3_Market_Value),
                                s6?.Old3_Extent);

                            col.Item()
                                .BorderBottom(1)
                                .BorderColor("#555555");

                            // PROPERTY DETAILS AS PER QUERY / REVIEW
                            col.Item()
                                .AlignCenter()
                                .Text($"PROPERTY DETAILS AS PER YOUR {typeWordTitle}")
                                .FontSize(10)
                                .Bold();

                            Section78PropertyTable(
                                col,
                                s6?.New_Property_Description,
                                s6?.New_Category,
                                s6?.New_Address,
                                FormatMV(s6?.New_Market_Value),
                                s6?.New_Extent,
                                s6?.New_Owner,
                                result.IsMulti,
                                s6?.New2_Category,
                                FormatMV(s6?.New2_Market_Value),
                                s6?.New2_Extent,
                                s6?.New3_Category,
                                FormatMV(s6?.New3_Market_Value),
                                s6?.New3_Extent);

                            // REASONS
                            col.Item()
                                .BorderBottom(1)
                                .BorderColor("#555555");

                            col.Item()
                                .AlignCenter()
                                .Text($"REASONS FOR {typeWordTitle}")
                                .FontSize(10)
                                .Bold();

                            col.Item()
                                .Background("#eaf4fb")
                                .Border(1)
                                .BorderColor("#444444")
                                .Padding(8)
                                .Text(string.IsNullOrWhiteSpace(s6?.Objection_Reasons)
                                    ? "No reasons provided."
                                    : s6.Objection_Reasons)
                                .FontSize(8);

                            // REQUIRED DOCUMENTATION
                            col.Item()
                                .BorderBottom(1)
                                .BorderColor("#555555");

                            col.Item()
                                .AlignCenter()
                                .Text($"REQUIRED DOCUMENTATION FOR {typeWordTitle}")
                                .FontSize(10)
                                .Bold();

                            col.Item()
                                .Text($"You have uploaded {result.FileCount} Document(s)")
                                .FontSize(9);

                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });

                                t.Cell()
                                    .Background("#eaf4fb")
                                    .Border(1)
                                    .BorderColor("#444444")
                                    .Padding(8)
                                    .Column(left =>
                                    {
                                        left.Item()
                                            .Text("Uploaded Documents (1–5):")
                                            .FontSize(8)
                                            .Bold();

                                        for (int i = 0; i < 5; i++)
                                        {
                                            var file = i < result.Files.Length ? result.Files[i] : null;

                                            if (!string.IsNullOrWhiteSpace(file))
                                                left.Item().Text($"• {file}").FontSize(7.5f);
                                        }
                                    });

                                t.Cell()
                                    .Background("#eaf4fb")
                                    .Border(1)
                                    .BorderColor("#444444")
                                    .Padding(8)
                                    .Column(right =>
                                    {
                                        right.Item()
                                            .Text("Uploaded Documents (6–10):")
                                            .FontSize(8)
                                            .Bold();

                                        for (int i = 5; i < 10; i++)
                                        {
                                            var file = i < result.Files.Length ? result.Files[i] : null;

                                            if (!string.IsNullOrWhiteSpace(file))
                                                right.Item().Text($"• {file}").FontSize(7.5f);
                                        }
                                    });
                            });
                        });

                        // FOOTER — same style as Objection/Appeal acknowledgement
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

                                if (!string.IsNullOrWhiteSpace(result.ValuationKey))
                                {
                                    f.Item()
                                        .Text(result.ValuationKey)
                                        .FontSize(8)
                                        .SemiBold()
                                        .FontColor("#cc0000");
                                }
                            });
                    });
                })
                .GeneratePdf(fullPath);

                _logger.LogInformation(
                    "[S78] Acknowledgement PDF saved → {Path}", fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[S78] Failed writing acknowledgement PDF for {Ref}", result.QueryRef);
            }

            static void RefRow(ColumnDescriptor col, string label, string? value)
            {
                col.Item().Text(t =>
                {
                    t.Span(label + " ").Bold();
                    t.Span(string.IsNullOrWhiteSpace(value) ? "—" : value);
                });
            }

            static void Section78PropertyTable(
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

        // ── Market value formatter ────────────────────────────────────────────
        private static string FormatMV(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "—";
            var clean = raw.Replace("R", "").Replace(",", "").Trim();
            if (decimal.TryParse(clean,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var num) && num > 0)
            {
                return "R " + num.ToString("N0",
                    new System.Globalization.CultureInfo("en-ZA"));
            }
            return raw;
        }
        // ── Pin generator ─────────────────────────────────────────────
        private static string GeneratePin()
        {
            const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digits = "23456789";
            var pin = new[]
            {
                letters[RandomNumberGenerator.GetInt32(letters.Length)],
                letters[RandomNumberGenerator.GetInt32(letters.Length)],
                letters[RandomNumberGenerator.GetInt32(letters.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)]
            };

            for (var index = pin.Length - 1; index > 0; index--)
            {
                var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
                (pin[index], pin[swapIndex]) = (pin[swapIndex], pin[index]);
            }

            return new string(pin);
        }

        private static void SetFileSlot(Obj_Files f, int n, string name)
        {
            switch (n)
            {
                case 1: f.Files1 = name; break;
                case 2: f.Files2 = name; break;
                case 3: f.Files3 = name; break;
                case 4: f.Files4 = name; break;
                case 5: f.Files5 = name; break;
                case 6: f.Files6 = name; break;
                case 7: f.Files7 = name; break;
                case 8: f.Files8 = name; break;
                case 9: f.Files9 = name; break;
                case 10: f.Files10 = name; break;
            }
        }

        // ── Dashboard reads (unchanged) ───────────────────────────────
        public async Task<List<Section78LinkedResult>> GetLinkedAsync(string userId)
        {
            try
            {
                await using var conn = new SqlConnection(_queryConn);
                var rows = await conn.QueryAsync<Section78LinkedResult>(
                    SP_LINKED,
                    new { userName = userId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60);
                return rows.ToList();
            }
            catch { return new(); }
        }

        public async Task<List<Section78SubmittedResult>> GetSubmittedAsync(string userId)
        {
            try
            {
                await using var conn = new SqlConnection(_queryConn);
                var rows = await conn.QueryAsync<Section78SubmittedResult>(
                    SP_SUBMITTED,
                    new { userName = userId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60);
                return rows.ToList();
            }
            catch { return new(); }
        }
        public async Task<GeneratedAcknowledgementResult>
    GenerateAcknowledgementFromDatabaseAsync(
        string queryReference,
        string userId,
        CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queryReference))
            {
                throw new ArgumentException(
                    "Section 78 reference is required.",
                    nameof(queryReference));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "The current user could not be identified.");
            }

            var reference = queryReference.Trim();

            var query = await _qdb.Que_Property_Info
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Query_No != null
                    && q.Query_No.Trim() == reference
                    && q.UserID == userId, cancellationToken);

            Section78AcknowledgementDbRow? row = null;
            if (query is not null)
            {
                var section6 = await _qdb.Obj_Section6.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Objection_Ref_S6 != null
                        && s.Objection_Ref_S6.Trim() == reference, cancellationToken);
                var section7 = await _qdb.Obj_Section7.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Objection_Ref_S7 != null
                        && s.Objection_Ref_S7.Trim() == reference, cancellationToken);
                var fil = await _qdb.Obj_Files.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Objection_Ref_files != null
                        && f.Objection_Ref_files.Trim() == reference, cancellationToken);

                row = BuildAcknowledgementRow(query, section6, section7, fil);
            }

            if (row is null)
            {
                throw new KeyNotFoundException(
                    $"Section 78 submission '{reference}' was not found for the current user.");
            }

            var isReview =
                row.Sub_typ == 1
                ||
                reference.EndsWith(
                    "-R",
                    StringComparison.OrdinalIgnoreCase);

            var files =
                new[]
                {
            row.Files1,
            row.Files2,
            row.Files3,
            row.Files4,
            row.Files5,
            row.Files6,
            row.Files7,
            row.Files8,
            row.Files9,
            row.Files10
                };

            var actualFileCount =
                files.Count(x =>
                    !string.IsNullOrWhiteSpace(x));

            var result =
                new Section78SubmitResult
                {
                    QueryRef =
                        reference,

                    QueryId =
                        row.Query_ID,

                    RandomPin =
                        row.RandomPin,

                    IsReview =
                        isReview,

                    IsMulti =
                        string.Equals(
                            row.Property_Type,
                            "Multi",
                            StringComparison.OrdinalIgnoreCase),

                    ValuationKey =
                        row.Valuation_Key,

                    FileCount =
                        row.Evidence_count > 0
                            ? row.Evidence_count
                            : actualFileCount,

                    Files =
                        files,

                    Section6 =
                        new Obj_Section6Model
                        {
                            Old_Property_Description =
                                FirstNotEmpty(
                                    row.Old_Property_Description,
                                    row.Property_Desc),

                            Old_Category =
                                row.Old_Category,

                            Old_Address =
                                row.Old_Address,

                            Old_Extent =
                                row.Old_Extent,

                            Old_Market_Value =
                                row.Old_Market_Value,

                            Old_Owner =
                                row.Old_Owner,

                            Old2_Category =
                                row.Old2_Category,

                            Old2_Extent =
                                row.Old2_Extent,

                            Old2_Market_Value =
                                row.Old2_Market_Value,

                            Old3_Category =
                                row.Old3_Category,

                            Old3_Extent =
                                row.Old3_Extent,

                            Old3_Market_Value =
                                row.Old3_Market_Value,

                            New_Property_Description =
                                row.New_Property_Description,

                            New_Category =
                                row.New_Category,

                            New_Address =
                                row.New_Address,

                            New_Extent =
                                row.New_Extent,

                            New_Market_Value =
                                row.New_Market_Value,

                            New_Owner =
                                row.New_Owner,

                            New2_Category =
                                row.New2_Category,

                            New2_Extent =
                                row.New2_Extent,

                            New2_Market_Value =
                                row.New2_Market_Value,

                            New3_Category =
                                row.New3_Category,

                            New3_Extent =
                                row.New3_Extent,

                            New3_Market_Value =
                                row.New3_Market_Value,

                            Objection_Reasons =
                                row.Objection_Reasons
                        }
                };

            var pdfBytes =
                GenerateAcknowledgementPdf(
                    result,
                    row.Objection_Date);

            if (pdfBytes is null || pdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Acknowledgement PDF generation returned no data for '{reference}'.");
            }

            var submissionType =
                isReview
                    ? "Section78Review"
                    : "Section78Query";

            var fileName =
                $"{SanitiseFileName(reference)}_" +
                $"{submissionType}_Acknowledgement_" +
                $"{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            _logger.LogInformation(
                "[S78 Acknowledgement Download] Generated {SubmissionType} acknowledgement from database for {Reference}. User={UserId}",
                submissionType,
                reference,
                userId);

            return new GeneratedAcknowledgementResult
            {
                ReferenceNumber =
                    reference,

                FileName =
                    fileName,

                PdfBytes =
                    pdfBytes,

                SubmissionType =
                    submissionType
            };
        }

        private byte[] GenerateAcknowledgementPdf(
    Section78SubmitResult result,
    DateTime? submittedAt = null)
        {
            var headerPath =
                !string.IsNullOrWhiteSpace(
                    _config["AppSettings:QueryHeaderImage"])
                    ? _config["AppSettings:QueryHeaderImage"]!
                    : Path.Combine(
                        _env.WebRootPath,
                        "Images",
                        "Obj_Header.PNG");

            var hasHeader =
                File.Exists(headerPath);

            var section6 =
                result.Section6;

            var processTitle =
                result.IsReview
                    ? "REVIEW"
                    : "QUERY";

            var processLower =
                result.IsReview
                    ? "review"
                    : "query";

            var capturedAt =
                submittedAt ?? DateTime.Now;

            var generatedAt =
                DateTime.Now;

            var validFiles =
                result.Files?
                    .Where(file =>
                        !string.IsNullOrWhiteSpace(file))
                    .Select(file =>
                        file!.Trim())
                    .Take(10)
                    .ToList()
                ?? new List<string>();

            var fileCount =
                result.FileCount > 0
                    ? result.FileCount
                    : validFiles.Count;

            QuestPDF.Settings.License =
                LicenseType.Community;

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);

                    page.Margin(36);

                    page.DefaultTextStyle(style =>
                        style
                            .FontFamily("Arial")
                            .FontSize(9));

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);

                        if (hasHeader)
                        {
                            column.Item()
                                .AlignCenter()
                                .Width(500)
                                .Height(90)
                                .Image(
                                    headerPath,
                                    ImageScaling.FitArea);
                        }

                        column.Item()
                            .AlignRight()
                            .Text(
                                capturedAt.ToString(
                                    "dd MMMM yyyy"))
                            .FontSize(10)
                            .SemiBold();

                        column.Item()
                            .AlignCenter()
                            .Text(
                                "CITY OF JOHANNESBURG")
                            .FontSize(13)
                            .Bold();

                        column.Item()
                            .AlignCenter()
                            .Text(
                                $"SECTION 78 {processTitle} ACKNOWLEDGEMENT")
                            .FontSize(12)
                            .Bold();

                        column.Item()
                            .BorderBottom(1)
                            .BorderColor("#555555");

                        column.Item()
                            .Text(
                                $"This is to acknowledge that your Section 78 {processLower} has been successfully received and logged. The details below are provided for your records.")
                            .FontSize(9);

                        column.Item()
                            .Text(
                                "IMPORTANT NOTICE: You have 48 hours from submission to upload any outstanding supporting documents.")
                            .FontSize(9)
                            .Bold();

                        column.Item()
                            .AlignCenter()
                            .Text("REFERENCE DETAILS")
                            .FontSize(10)
                            .Bold();

                        column.Item()
                            .Background("#eeeeee")
                            .Border(1)
                            .BorderColor("#444444")
                            .Padding(10)
                            .Column(referenceBox =>
                            {
                                ReferenceRow(
                                    referenceBox,
                                    "Property Description:",
                                    FirstNotEmpty(
                                        section6?
                                            .Old_Property_Description,
                                        section6?
                                            .New_Property_Description));

                                ReferenceRow(
                                    referenceBox,
                                    $"{processTitle} Reference:",
                                    result.QueryRef);

                                ReferenceRow(
                                    referenceBox,
                                    "PIN:",
                                    result.RandomPin);

                                ReferenceRow(
                                    referenceBox,
                                    "Date Captured:",
                                    capturedAt.ToString(
                                        "dd MMMM yyyy HH:mm"));

                                if (!string.IsNullOrWhiteSpace(
                                        result.ValuationKey))
                                {
                                    ReferenceRow(
                                        referenceBox,
                                        "Valuation Key:",
                                        result.ValuationKey);
                                }
                            });

                        column.Item()
                            .BorderBottom(1)
                            .BorderColor("#555555");

                        column.Item()
                            .AlignCenter()
                            .Text(
                                "PROPERTY DETAILS AS LISTED IN THE VALUATION ROLL")
                            .FontSize(10)
                            .Bold();

                        Section78PropertyTable(
                            column,
                            section6?
                                .Old_Property_Description,
                            section6?
                                .Old_Category,
                            section6?
                                .Old_Address,
                            FormatMV(
                                section6?
                                    .Old_Market_Value),
                            section6?
                                .Old_Extent,
                            section6?
                                .Old_Owner,
                            result.IsMulti,
                            section6?
                                .Old2_Category,
                            FormatMV(
                                section6?
                                    .Old2_Market_Value),
                            section6?
                                .Old2_Extent,
                            section6?
                                .Old3_Category,
                            FormatMV(
                                section6?
                                    .Old3_Market_Value),
                            section6?
                                .Old3_Extent);

                        column.Item()
                            .BorderBottom(1)
                            .BorderColor("#555555");

                        column.Item()
                            .AlignCenter()
                            .Text(
                                $"PROPERTY DETAILS AS PER YOUR {processTitle}")
                            .FontSize(10)
                            .Bold();

                        Section78PropertyTable(
                            column,
                            section6?
                                .New_Property_Description,
                            section6?
                                .New_Category,
                            section6?
                                .New_Address,
                            FormatMV(
                                section6?
                                    .New_Market_Value),
                            section6?
                                .New_Extent,
                            section6?
                                .New_Owner,
                            result.IsMulti,
                            section6?
                                .New2_Category,
                            FormatMV(
                                section6?
                                    .New2_Market_Value),
                            section6?
                                .New2_Extent,
                            section6?
                                .New3_Category,
                            FormatMV(
                                section6?
                                    .New3_Market_Value),
                            section6?
                                .New3_Extent);

                        column.Item()
                            .BorderBottom(1)
                            .BorderColor("#555555");

                        column.Item()
                            .AlignCenter()
                            .Text(
                                $"REASONS FOR {processTitle}")
                            .FontSize(10)
                            .Bold();

                        column.Item()
                            .Background("#eaf4fb")
                            .Border(1)
                            .BorderColor("#444444")
                            .Padding(8)
                            .Text(
                                string.IsNullOrWhiteSpace(
                                    section6?
                                        .Objection_Reasons)
                                    ? "No reasons provided."
                                    : section6
                                        .Objection_Reasons)
                            .FontSize(8);

                        column.Item()
                            .BorderBottom(1)
                            .BorderColor("#555555");

                        column.Item()
                            .AlignCenter()
                            .Text(
                                $"REQUIRED DOCUMENTATION FOR {processTitle}")
                            .FontSize(10)
                            .Bold();

                        column.Item()
                            .Text(
                                $"You have uploaded {fileCount} document(s).")
                            .FontSize(9);

                        column.Item()
                            .Element(container =>
                                SupportingDocumentsTable(
                                    container,
                                    validFiles));
                    });

                    page.Footer()
                        .PaddingTop(5)
                        .AlignCenter()
                        .Column(footer =>
                        {
                            footer.Item()
                                .Text(
                                    "This is an official document generated by the City of Johannesburg")
                                .FontSize(7)
                                .FontColor("#666666");

                            footer.Item()
                                .Text(
                                    $"Generated on: {generatedAt:dd MMMM yyyy HH:mm}")
                                .FontSize(7)
                                .FontColor("#666666");

                            if (!string.IsNullOrWhiteSpace(
                                    result.ValuationKey))
                            {
                                footer.Item()
                                    .Text(
                                        result.ValuationKey)
                                    .FontSize(8)
                                    .SemiBold()
                                    .FontColor("#cc0000");
                            }
                        });
                });
            }).GeneratePdf();

            static void ReferenceRow(
                ColumnDescriptor column,
                string label,
                string? value)
            {
                column.Item().Text(text =>
                {
                    text.Span(label + " ")
                        .Bold();

                    text.Span(
                        string.IsNullOrWhiteSpace(value)
                            ? "—"
                            : value);
                });
            }

            static void Section78PropertyTable(
                ColumnDescriptor column,
                string? propertyDescription,
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
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.6f);
                        columns.RelativeColumn(1.7f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(1.1f);
                    });

                    HeaderCell(
                        table,
                        "Property Description");

                    HeaderCell(
                        table,
                        "Category");

                    HeaderCell(
                        table,
                        "Physical Address");

                    HeaderCell(
                        table,
                        "Market Value");

                    HeaderCell(
                        table,
                        "Extent");

                    HeaderCell(
                        table,
                        "Owner");

                    DataCell(
                        table,
                        propertyDescription);

                    DataCell(
                        table,
                        category);

                    DataCell(
                        table,
                        physicalAddress);

                    DataCell(
                        table,
                        marketValue);

                    DataCell(
                        table,
                        extent);

                    DataCell(
                        table,
                        owner);

                    if (isMulti &&
                        HasAny(
                            category2,
                            marketValue2,
                            extent2))
                    {
                        DataCell(table, "");
                        DataCell(table, category2);
                        DataCell(table, "");
                        DataCell(table, marketValue2);
                        DataCell(table, extent2);
                        DataCell(table, "");
                    }

                    if (isMulti &&
                        HasAny(
                            category3,
                            marketValue3,
                            extent3))
                    {
                        DataCell(table, "");
                        DataCell(table, category3);
                        DataCell(table, "");
                        DataCell(table, marketValue3);
                        DataCell(table, extent3);
                        DataCell(table, "");
                    }
                });
            }

            static void SupportingDocumentsTable(
                IContainer container,
                IReadOnlyList<string> files)
            {
                var leftFiles =
                    files
                        .Take(5)
                        .ToList();

                var rightFiles =
                    files
                        .Skip(5)
                        .Take(5)
                        .ToList();

                container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Cell()
                        .Background("#eaf4fb")
                        .Border(1)
                        .BorderColor("#444444")
                        .Padding(8)
                        .Column(left =>
                        {
                            left.Item()
                                .Text(
                                    "Uploaded Documents (1–5):")
                                .FontSize(8)
                                .Bold();

                            if (leftFiles.Count == 0)
                            {
                                left.Item()
                                    .Text(
                                        "No documents uploaded.")
                                    .FontSize(8)
                                    .Italic()
                                    .FontColor("#666666");
                            }
                            else
                            {
                                foreach (var file in leftFiles)
                                {
                                    left.Item()
                                        .Text($"• {file}")
                                        .FontSize(7.5f);
                                }
                            }
                        });

                    table.Cell()
                        .Background("#eaf4fb")
                        .Border(1)
                        .BorderColor("#444444")
                        .Padding(8)
                        .Column(right =>
                        {
                            right.Item()
                                .Text(
                                    "Uploaded Documents (6–10):")
                                .FontSize(8)
                                .Bold();

                            foreach (var file in rightFiles)
                            {
                                right.Item()
                                    .Text($"• {file}")
                                    .FontSize(7.5f);
                            }
                        });
                });
            }

            static bool HasAny(
                params string?[] values)
            {
                return values.Any(value =>
                    !string.IsNullOrWhiteSpace(value));
            }

            static void HeaderCell(
                TableDescriptor table,
                string value)
            {
                table.Cell()
                    .Background("#3f7fb5")
                    .Border(1)
                    .BorderColor("#222222")
                    .Padding(5)
                    .AlignCenter()
                    .Text(value)
                    .FontSize(8)
                    .FontColor(Colors.White)
                    .Bold();
            }

            static void DataCell(
                TableDescriptor table,
                string? value)
            {
                table.Cell()
                    .Border(1)
                    .BorderColor("#222222")
                    .Padding(5)
                    .Text(
                        string.IsNullOrWhiteSpace(value)
                            ? ""
                            : value)
                    .FontSize(8);
            }
        }
        private static string FirstNotEmpty(
    params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        private static string SanitiseFileName(
            string value)
        {
            var cleaned =
                string.Concat(
                    value.Split(
                        Path.GetInvalidFileNameChars()));

            cleaned =
                cleaned
                    .Replace(" ", "_")
                    .Replace("/", "_")
                    .Replace("\\", "_")
                    .Replace(":", "_")
                    .Trim();

            while (cleaned.Contains("__"))
            {
                cleaned =
                    cleaned.Replace("__", "_");
            }

            return string.IsNullOrWhiteSpace(cleaned)
                ? "Section78"
                : cleaned;
        }
  
        private static Section78AcknowledgementDbRow BuildAcknowledgementRow(
            Que_Property_InfoModel query,
            Obj_Section6Model? section6,
            Obj_Section7Model? section7,
            Obj_Files? files)
        {
            return new Section78AcknowledgementDbRow
            {
                Query_ID = query.Query_ID,
                QUERY_No = query.Query_No,
                QUERY_Status = query.Query_Status,
                Sub_typ = query.Sub_typ,
                Property_Type = query.Property_Type,
                Property_Desc = query.Property_Desc,
                Valuation_Key = query.Valuation_Key,
                UserID = query.UserID,
                Old_Property_Description = section6?.Old_Property_Description,
                Old_Category = section6?.Old_Category,
                Old_Address = section6?.Old_Address,
                Old_Extent = section6?.Old_Extent,
                Old_Market_Value = section6?.Old_Market_Value,
                Old_Owner = section6?.Old_Owner,
                Old2_Category = section6?.Old2_Category,
                Old2_Extent = section6?.Old2_Extent,
                Old2_Market_Value = section6?.Old2_Market_Value,
                Old3_Category = section6?.Old3_Category,
                Old3_Extent = section6?.Old3_Extent,
                Old3_Market_Value = section6?.Old3_Market_Value,
                New_Property_Description = section6?.New_Property_Description,
                New_Category = section6?.New_Category,
                New_Address = section6?.New_Address,
                New_Extent = section6?.New_Extent,
                New_Market_Value = section6?.New_Market_Value,
                New_Owner = section6?.New_Owner,
                New2_Category = section6?.New2_Category,
                New2_Extent = section6?.New2_Extent,
                New2_Market_Value = section6?.New2_Market_Value,
                New3_Category = section6?.New3_Category,
                New3_Extent = section6?.New3_Extent,
                New3_Market_Value = section6?.New3_Market_Value,
                Objection_Reasons = section6?.Objection_Reasons,
                RandomPin = section7?.RandomPin,
                Objection_Date = section7?.Objection_Date,
                Evidence_count = (int)(files?.Evidence_count ?? 0),
                Files1 = files?.Files1,
                Files2 = files?.Files2,
                Files3 = files?.Files3,
                Files4 = files?.Files4,
                Files5 = files?.Files5,
                Files6 = files?.Files6,
                Files7 = files?.Files7,
                Files8 = files?.Files8,
                Files9 = files?.Files9,
                Files10 = files?.Files10
            };
        }

        private sealed class Section78AcknowledgementDbRow
        {
            public long Query_ID { get; set; }

            public string? QUERY_No { get; set; }

            public string? QUERY_Status { get; set; }

            public int Sub_typ { get; set; }

            public string? Property_Type { get; set; }

            public string? Property_Desc { get; set; }

            public string? Valuation_Key { get; set; }

            public string? UserID { get; set; }

            public string? Old_Property_Description { get; set; }

            public string? Old_Category { get; set; }

            public string? Old_Address { get; set; }

            public string? Old_Extent { get; set; }

            public string? Old_Market_Value { get; set; }

            public string? Old_Owner { get; set; }

            public string? Old2_Category { get; set; }

            public string? Old2_Extent { get; set; }

            public string? Old2_Market_Value { get; set; }

            public string? Old3_Category { get; set; }

            public string? Old3_Extent { get; set; }

            public string? Old3_Market_Value { get; set; }

            public string? New_Property_Description { get; set; }

            public string? New_Category { get; set; }

            public string? New_Address { get; set; }

            public string? New_Extent { get; set; }

            public string? New_Market_Value { get; set; }

            public string? New_Owner { get; set; }

            public string? New2_Category { get; set; }

            public string? New2_Extent { get; set; }

            public string? New2_Market_Value { get; set; }

            public string? New3_Category { get; set; }

            public string? New3_Extent { get; set; }

            public string? New3_Market_Value { get; set; }

            public string? Objection_Reasons { get; set; }

            public string? RandomPin { get; set; }

            public DateTime? Objection_Date { get; set; }

            public int Evidence_count { get; set; }

            public string? Files1 { get; set; }

            public string? Files2 { get; set; }

            public string? Files3 { get; set; }

            public string? Files4 { get; set; }

            public string? Files5 { get; set; }

            public string? Files6 { get; set; }

            public string? Files7 { get; set; }

            public string? Files8 { get; set; }

            public string? Files9 { get; set; }

            public string? Files10 { get; set; }
        }
    }
}
