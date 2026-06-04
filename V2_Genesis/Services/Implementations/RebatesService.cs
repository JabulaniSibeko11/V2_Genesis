using Dapper;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data.SqlClient;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Rebates;
using V2_Genesis.Models.Results.Rebates;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class RebatesService : IRebatesService
    {
        private readonly RebateDBContext _db;
        private readonly IConfiguration _config;
        private readonly string _connStr;
        private readonly IEmailService _email;
       private readonly IWebHostEnvironment _env;
        private readonly ILogger<RebatesService> _logger;
        public RebatesService(RebateDBContext db, IConfiguration config, IEmailService email, IWebHostEnvironment env, ILogger<RebatesService> logger) { 
        _db= db;
            _config= config;
            _email= email;
            _env= env;
            _logger= logger;
            _connStr = config.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("DefaultConnection missing.");
        }

        public async Task<RebatesSubmitResult> SubmitAsync(
            string rebateType, string userId, string userEmail,
            Rebate_Info info,
            Rebate_Section1_PersonalDetails s1,
            Rebate_Section2_Addresses s2,
            Rebate_Section3_ContactDetails s3,
            Rebate_Section4_Ownership s4,
            Rebate_Section5_Declaration s5,
            Rebate_Section6_FI s6,
            Rebate_Section7_MinorOccupants s7,
            Rebate_Section8_ACS s8,
            Rebate_Section9_HeritageDetails s9,
            Rebate_Section10_Organisation s10,
            Rebate_Section11_SummaryIES s11,
            Rebates_Files rebateFiles,
            List<IFormFile> evidenceFiles,
            List<IFormFile> attachedFiles)
        {
            // ── 1. Status determination (mirrors V1 logic exactly) ───
            info.Status = DetermineStatus(rebateType, s1, s10);
            info.UserID = userId;
            info.Rebate_Type = rebateType;

            _db.Rebate_Infos.Add(info);
            await _db.SaveChangesAsync();

            long id = info.Rebate_ID;

            // ── 2. Save sections (same order as V1) ──────────────────
            s1.Ref = id; _db.Rebate_Section1_PersonalDetails.Add(s1); await _db.SaveChangesAsync();
            s2.Ref = id; _db.Rebate_Section2_Addresses.Add(s2); await _db.SaveChangesAsync();
            s3.Ref = id; _db.Rebate_Section3_ContactDetails.Add(s3); await _db.SaveChangesAsync();
            s4.Ref = id; _db.Rebate_Section4_Ownerships.Add(s4); await _db.SaveChangesAsync();
            s6.Ref = id; _db.Rebate_Section6_FIs.Add(s6); await _db.SaveChangesAsync();
            s7.Ref = id; _db.Rebate_Section7_MinorOccupants.Add(s7); await _db.SaveChangesAsync();
            s8.Ref = id; _db.Rebate_Section8_ACSs.Add(s8); await _db.SaveChangesAsync();
            s9.Ref = id; _db.Rebate_Section9_HeritageDetails.Add(s9); await _db.SaveChangesAsync();
            s10.Ref = id; _db.Rebate_Section10_Organisations.Add(s10); await _db.SaveChangesAsync();
            s11.Ref = id; _db.Rebate_Section11_Summaries.Add(s11); await _db.SaveChangesAsync();

            // Section 5 (declaration) saved last — needs timestamp
            s5.DateOfSubmission = DateTime.Now;
            s5.Ref = id;
            _db.Rebate_Section5_Declarations.Add(s5);
            await _db.SaveChangesAsync();

            // ── 3. File upload ───────────────────────────────────────
            string rebateNo = info.Rebate_No ?? id.ToString();

            string uploadRoot = _config["ObjectionRolls:Rebates:RebateRooTPath"]
                     ?? throw new InvalidOperationException("RebateRooTPath missing.");
            string folder = Path.Combine(uploadRoot, rebateNo);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Attached (rep letter)
            foreach (var file in attachedFiles)
            {
                var sub = Path.Combine(folder, "AttachedFile");
                if (!Directory.Exists(sub)) Directory.CreateDirectory(sub);
                var name = Path.GetFileName(file.FileName);
                rebateFiles.Rep_letter = name;
                await using var stream = File.Create(Path.Combine(sub, name));
                await file.CopyToAsync(stream);
            }

            // Evidence files
            int count = 0;
            foreach (var file in evidenceFiles)
            {
                count++;
                var name = Path.GetFileName(file.FileName);
                SetFileSlot(rebateFiles, count, name);
                await using var stream = File.Create(Path.Combine(folder, name));
                await file.CopyToAsync(stream);
            }

            rebateFiles.Ref = id;
            rebateFiles.Evidence_count = count;
            _db.Rebates_Files.Add(rebateFiles);
            await _db.SaveChangesAsync();

            var result = new RebatesSubmitResult
            {
                RebateNo = rebateNo,
                RebateId = Convert.ToInt32(id),
                status = info.Status,
                FileCount = count,
                SubmittedAt = s5.DateOfSubmission?.ToString("yyyy-MM-dd HH:mm"),
                files = new[]
                {
                rebateFiles.Files1, rebateFiles.Files2, rebateFiles.Files3,
                rebateFiles.Files4, rebateFiles.Files5, rebateFiles.Files6,
                rebateFiles.Files7, rebateFiles.Files8, rebateFiles.Files9,
                rebateFiles.Files10
            }
            };

            // ── 4. Acknowledgement PDF to disk ───────────────────────

            // ── 4. Acknowledgement PDF to disk ───────────────────────
            WriteAcknowledgement(result);

            // ── 5. Email — attach PDF for Acknowledged, plain for Reject ─
            var (subject, htmlBody) = BuildRebateEmail(
                rebateType, result, userEmail, info.Status == "Auto Reject");

            if (info.Status != "Auto Reject")
            {
                // Read the PDF we just wrote and attach it
                var pdfPath = Path.Combine(
                    _config["ObjectionRolls:Rebates:RebateRooTPath"] ?? "",
                    result.RebateNo,
                    $"{result.RebateNo}_Acknowledgement.pdf");

                if (File.Exists(pdfPath))
                {
                    var pdfBytes = await File.ReadAllBytesAsync(pdfPath);
                    await _email.SendEmailWithAttachmentAsync(
                        userEmail, subject, htmlBody,
                        pdfBytes,
                        $"{result.RebateNo}_Acknowledgement.pdf");
                }
                else
                {
                    // PDF write failed — send without attachment rather than crash
                    _logger.LogWarning(
                        "[Rebates] Acknowledgement PDF not found for {RebateNo} — " +
                        "sending email without attachment.", result.RebateNo);
                    await _email.SendEmailAsync(userEmail, subject, htmlBody);
                }
            }
            else
            {
                // Auto-rejected — no PDF attachment
                await _email.SendEmailAsync(userEmail, subject, htmlBody);
            }

            return result;

        }


        // ── Build rebate email subject + HTML body ────────────────────
        private static (string subject, string htmlBody) BuildRebateEmail(
            string rebateType, RebatesSubmitResult result,
            string userEmail, bool isRejected)
        {
            var statusLabel = isRejected ? "Unsuccessful" : "Received — Acknowledgement";
            var statusColor = isRejected ? "#bb4722" : "#36626d";
            var statusBg = isRejected ? "#fdf0eb" : "#e8f3f4";
            var subject = isRejected
                ? $"City of Johannesburg — Rebate Application Unsuccessful: {result.RebateNo}"
                : $"City of Johannesburg — Rebate Application Received: {result.RebateNo}";

            var bodyMessage = isRejected
                ? @"<p>Unfortunately your rebate application did not meet the qualifying criteria
                   at this stage and has been <strong>automatically declined</strong>.</p>
                <p>If you believe this is incorrect, or would like to enquire further,
                   please contact the Valuation Services Department using the details below.</p>"
                : @"<p>Thank you for submitting your rebate application through the
                   City of Johannesburg Valuation Portal.</p>
                <p>Your application has been <strong>successfully received</strong> and
                   will be reviewed by the Valuation Services Department.
                   You will be contacted regarding the outcome.</p>";

            var htmlBody = $@"
<!DOCTYPE html><html lang='en'>
<head><meta charset='UTF-8'/></head>
<body style='margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 0;background:#f5f5f5;'>
<tr><td align='center'>
<table width='600' cellpadding='0' cellspacing='0'
       style='background:#fff;border-radius:10px;overflow:hidden;
              box-shadow:0 4px 20px rgba(0,0,0,.10);'>
 
  <!-- Header -->
  <tr><td style='background:#e6b000;padding:28px 32px;text-align:center;'>
    <span style='font-size:18px;font-weight:800;color:#1a1a1a;letter-spacing:1px;'>
      City of Johannesburg</span><br/>
    <span style='font-size:11px;color:rgba(0,0,0,.6);'>
      Valuation Services Department — Rates Rebates</span>
  </td></tr>
 
  <!-- Status badge -->
  <tr><td style='padding:20px 32px 0;'>
    <div style='background:{statusBg};border-left:4px solid {statusColor};
                border-radius:6px;padding:12px 16px;font-size:13px;color:{statusColor};
                font-weight:700;'>
      Application Status: {statusLabel}
    </div>
  </td></tr>
 
  <!-- Body -->
  <tr><td style='padding:24px 32px;font-size:13.5px;color:#333;line-height:1.7;'>
    <p style='margin:0 0 12px;'>Dear Applicant,</p>
    {bodyMessage}
 
    <!-- Reference details -->
    <table style='width:100%;border-collapse:collapse;margin:16px 0;
                  background:#f7f7f7;border-radius:6px;'>
      <tr>
        <td style='padding:8px 14px;font-weight:700;color:#555;
                   font-size:12px;border-bottom:1px solid #eee;width:200px;'>
          Application Number:</td>
        <td style='padding:8px 14px;font-size:12px;border-bottom:1px solid #eee;'>
          <strong>{result.RebateNo}</strong></td>
      </tr>
      <tr>
        <td style='padding:8px 14px;font-weight:700;color:#555;font-size:12px;
                   border-bottom:1px solid #eee;'>Rebate Type:</td>
        <td style='padding:8px 14px;font-size:12px;border-bottom:1px solid #eee;'>
          {rebateType}</td>
      </tr>
      <tr>
        <td style='padding:8px 14px;font-weight:700;color:#555;font-size:12px;'>
          Submission Date:</td>
        <td style='padding:8px 14px;font-size:12px;'>{result.SubmittedAt}</td>
      </tr>
    </table>
 
    <p style='font-size:13px;color:#444;margin-top:14px;'>
      For enquiries contact us:<br/>
      <strong>Tel:</strong> 011 407-6622 / 011 407-6597<br/>
      <strong>Email:</strong>
      <a href='mailto:valuationenquiries@joburg.org.za'
         style='color:#36626d;'>valuationenquiries@joburg.org.za</a>
    </p>
  </td></tr>
 
  <!-- Footer -->
  <tr><td style='background:#1a1a1a;padding:18px 32px;text-align:center;
                 font-size:11px;color:rgba(255,255,255,.5);'>
    City of Johannesburg Valuation Services Department<br/>
    This is an automated email — please do not reply directly.<br/>
    &copy; {DateTime.Now.Year} City of Johannesburg. All rights reserved.
  </td></tr>
 
</table>
</td></tr>
</table>
</body></html>";

            return (subject, htmlBody);
        }


        // ── Status — mirrors V1 per-type logic exactly ───────────────
        private static string DetermineStatus(
            string rebateType,
            Rebate_Section1_PersonalDetails s1,
            Rebate_Section10_Organisation s10)
        {
            bool occupies = s1.OccupyMentionedProperty != "No";
            bool sarsOk = s10.RegisteredWithSARS != "No";

            switch (rebateType)
            {
                case RebateType.Pensioner70:
                    int age70 = ParseAge(s1.IDNumber);
                    return (s1.IDNumber != null)
                        ? (age70 < 70 || !occupies || !sarsOk ? "Auto Reject" : "Acknowledge")
                        : (!occupies || !sarsOk ? "Auto Reject" : "Acknowledge");

                case RebateType.Pensioner60:
                    int age60 = ParseAge(s1.IDNumber);
                    return (s1.IDNumber != null)
                        ? (!(age60 >= 60 && age60 <= 69) || !occupies || !sarsOk ? "Auto Reject" : "Acknowledge")
                        : (!occupies || !sarsOk ? "Auto Reject" : "Acknowledge");

                case RebateType.SportsClub:
                case RebateType.ProtectionAnimal:
                case RebateType.Education:
                    // Only SARS check
                    return sarsOk ? "Acknowledge" : "Auto Reject";

                default:
                    // ChildHeaded, PBO, Disaster, LifeRights, Heritage, HighDensity
                    return (!occupies || !sarsOk) ? "Auto Reject" : "Acknowledge";
            }
        }

        private static int ParseAge(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length < 6) return 0;
            try
            {
                int yr = int.Parse(id.Substring(0, 2));
                int mo = int.Parse(id.Substring(2, 2));
                int dy = int.Parse(id.Substring(4, 2));
                int cen = (yr >= 0 && yr <= 30) ? 2000 : 1900;
                return DateTime.Today.Year - new DateTime(cen + yr, mo, dy).Year;
            }
            catch { return 0; }
        }

        private static void SetFileSlot(Rebates_Files f, int n, string name)
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

        // ── Dashboard (SP: UserNameRebates) ──────────────────────────
        public async Task<List<Rebate_View_Model>> GetDashboardAsync(string userId)
        {
            await using var conn = new SqlConnection(_connStr);
            var rows = await conn.QueryAsync<Rebate_View_Model>(
                "EXEC [Objection].[dbo].[UserNameRebates] @UserName",
                new { UserName = userId });
            return rows.ToList();
        }

        // ── View rebate detail (SP: ViewRebateForm) ───────────────────
        // One SP call returns ALL columns — Dapper maps everything.
        // Partial views just use what they need from the full model.
        public async Task<List<Rebate_View_Model>> GetRebateDataAsync(string rebateNo)
        {
            await using var conn = new SqlConnection(_connStr);
            var rows = await conn.QueryAsync<Rebate_View_Model>(
                "EXEC [Objection].[dbo].[ViewRebateForm] @RebateNo",
                new { RebateNo = rebateNo });
            return rows.ToList();
        }

        // ── Write acknowledgement PDF to disk (QuestPDF) ─────────────
        public void WriteAcknowledgement(RebatesSubmitResult result)
        {
           // var uploadRoot = _config["AppSettings:RebateRooTPath"] ?? "";
            var uploadRoot = _config["ObjectionRolls:Rebates:RebateRooTPath"] 
                    ?? throw new InvalidOperationException("RebateRooTPath missing.");
            var folder = Path.Combine(uploadRoot, result.RebateNo);

            if (!Directory.Exists(folder)) return;

            var pdfPath = Path.Combine(folder, $"{result.RebateNo}_Acknowledgement.pdf");
            // ── Header image — resolved from wwwroot ─────────────────────
            var imgPath = Path.Combine(_env.WebRootPath, "Images", "Rebate_Header.PNG");
            // Uploaded file list — only non-empty slots
            var uploadedFiles = result.files
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();

            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(1.5f, Unit.Centimetre);
                    page.MarginBottom(1.5f, Unit.Centimetre);
                    page.MarginHorizontal(2f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(11));

                    page.Content().Column(col =>
                    {
                        col.Spacing(12);

                        // ── Header image ──────────────────────────────
                        if (File.Exists(imgPath))
                        {
                            col.Item()
                               .Height(90)
                               .Image(imgPath, ImageScaling.FitArea);
                        }

                        // ── Divider ───────────────────────────────────
                        col.Item()
                           .BorderBottom(1)
                           .BorderColor("#e6b000")
                           .PaddingBottom(6)
                           .Text("City of Johannesburg — Rates Rebates")
                           .FontSize(9)
                           .FontColor("#645a50")
                           .Italic();

                        // ── Thank you block ───────────────────────────
                        col.Item()
                           .Background("#f7f5f2")
                           .Border(1)
                           .BorderColor("#e8e3dc").CornerRadius(6)
                           .Padding(16)
                           .Column(inner =>
                           {
                               inner.Item()
                                    .Text("Thank you for applying.")
                                    .FontSize(14)
                                    .FontColor("#36626d")
                                    .Bold();

                               inner.Item().Height(8);

                               inner.Item().Row(row =>
                               {
                                   row.RelativeItem()
                                      .Text(t =>
                                      {
                                          t.Span("Application Number: ").Bold();
                                          t.Span(result.RebateNo);
                                      });
                                   row.RelativeItem()
                                      .Text(t =>
                                      {
                                          t.Span("Date: ").Bold();
                                          t.Span(result.SubmittedAt ?? "");
                                      });
                               });

                               inner.Item().Height(4);

                               inner.Item().Text(t =>
                               {
                                   t.Span("Status: ").Bold();
                                   t.Span(result.status ?? "Acknowledge")
                                    .FontColor(result.status == "Auto Reject"
                                        ? "#bb4722" : "#36626d")
                                    .Bold();
                               });
                           });

                        // ── Documents uploaded ────────────────────────
                        col.Item()
                           .Text($"You have uploaded {result.FileCount} document(s).")
                           .FontSize(11)
                           .Bold();

                        if (uploadedFiles.Any())
                        {
                            col.Item()
                               .Border(1)
                               .BorderColor("#e8e3dc").
                               CornerRadius(6)
                               .Padding(12)
                               .Column(inner =>
                               {
                                   inner.Item()
                                        .Text("Uploaded Documents")
                                        .FontSize(10)
                                        .FontColor("#645a50")
                                        .Bold();

                                   inner.Item().Height(6);

                                   foreach (var fileName in uploadedFiles)
                                   {
                                       inner.Item().Row(row =>
                                       {
                                           row.ConstantItem(14)
                                              .Text("•")
                                              .FontColor("#e6b000")
                                              .Bold();
                                           row.RelativeItem()
                                              .Text(fileName)
                                              .FontSize(10);
                                       });
                                   }
                               });
                        }

                        // ── Footer note ───────────────────────────────
                        col.Item()
                           .PaddingTop(8)
                           .Text("Please keep this acknowledgement for your records. " +
                                 "You will be contacted regarding the outcome of your application.")
                           .FontSize(9)
                           .FontColor("#645a50")
                           .Italic();
                    });

                    // ── Page footer ───────────────────────────────────
                    page.Footer()
                        .AlignCenter()
                        .Text(t =>
                        {
                            t.Span("City of Johannesburg Valuation Portal  |  ")
                             .FontSize(8).FontColor("#999");
                            t.Span("Ref: ").FontSize(8).FontColor("#999");
                            t.Span(result.RebateNo).FontSize(8).FontColor("#36626d");
                        });
                });
            })
            .GeneratePdf(pdfPath);
        }
    }
}

