using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using V2_Genesis.Models.Emails;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _cfg;
        private readonly AppSettings _app;
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _config;

        public EmailService(
            IOptions<EmailSettings> emailOpts,
            IOptions<AppSettings> appOpts,
            ILogger<EmailService> logger,
            IConfiguration config)
        {
            _cfg = emailOpts.Value;
            _app = appOpts.Value;
            _logger = logger;
            _config = config;
        }
        private static readonly Dictionary<string, string> RollConnections = new()
        {
            ["Objection"] = "DefaultConnection",
            ["Objection_Supp1"] = "Sup1Connection",
            ["Objection_Supp2"] = "Sup2Connection",
            ["Objection_Supp3"] = "Sup3Connection",
        };
        private static readonly Dictionary<string, string> RollTitles = new()
        {
            ["Objection"] = "General Valuation Roll (GV23)",
            ["Objection_Supp1"] = "Supplementary Roll 1",
            ["Objection_Supp2"] = "Supplementary Roll 2",
            ["Objection_Supp3"] = "Supplementary Roll 3",
        };

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                using var smtp = BuildClient();
                using var msg = new MailMessage
                {
                    From = new MailAddress(_cfg.Username, _cfg.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                msg.To.Add(toEmail);
                await smtp.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw;
            }
        }

        public Task SendConfirmationEmailAsync(string toEmail, string displayName, string confirmationLink)
        {
            var subject = $"{_app.PortalSubtitle} – Please confirm your email address";
            var body = EmailTemplate(
                heading: "Confirm Your Email",
                body: $@"<p>Hi <strong>{displayName}</strong>,</p>
                           <p>Thank you for registering on the <strong>{_app.PortalSubtitle}</strong>.</p>
                           <p>Please click the button below to confirm your email address and activate your account.</p>",
                btnLabel: "Confirm Email Address",
                btnLink: confirmationLink,
                footer: "If you did not create an account, please ignore this email.");

            return SendEmailAsync(toEmail, subject, body);
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string displayName, string resetLink)
        {
            var subject = $"{_app.PortalSubtitle} – Password Reset Request";
            var body = EmailTemplate(
                heading: "Reset Your Password",
                body: $@"<p>Hi <strong>{displayName}</strong>,</p>
                          <p>We received a request to reset the password for your <strong>{_app.PortalSubtitle}</strong> account.</p>
                          <p>Click the button below to set a new password. This link expires in 24 hours.</p>",
                btnLabel: "Reset My Password",
                btnLink: resetLink,
                footer: "If you did not request a password reset, please ignore this email and your password will remain unchanged.");

            return SendEmailAsync(toEmail, subject, body);
        }

        // ── Private helpers ────────────────────────────────────────────────────
        private SmtpClient BuildClient() => new SmtpClient
        {
            Host = _cfg.Host,
            Port = _cfg.Port,
            EnableSsl = _cfg.EnableSsl,
            UseDefaultCredentials = _cfg.UseDefaultCredentials,
            Credentials = new NetworkCredential(_cfg.Username, _cfg.Password)
        };

        private string EmailTemplate(string heading, string body, string btnLabel, string btnLink, string footer) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f5f5;padding:40px 0;'>
    <tr><td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08);'>
        <!-- Header -->
        <tr><td style='background:#e6b000;padding:28px 40px;text-align:center;'>
          <span style='font-size:28px;font-weight:700;color:#1a1a1a;letter-spacing:2px;'>City of Johannesburg Valuation Portal</span><br/>
          //<span style='font-size:12px;color:#1a1a1a;opacity:.8;'>City of Johannesburg Valuation Portal</span>
        </td></tr>
        <!-- Body -->
        <tr><td style='padding:40px;color:#1a1a1a;font-size:15px;line-height:1.6;'>
          <h2 style='margin:0 0 20px;font-size:22px;color:#1a1a1a;'>{heading}</h2>
          {body}
          <div style='text-align:center;margin:32px 0;'>
            <a href='{btnLink}' style='display:inline-block;background:#e6b000;color:#1a1a1a;text-decoration:none;padding:14px 32px;border-radius:6px;font-weight:700;font-size:15px;'>{btnLabel}</a>
          </div>
          <p style='color:#888;font-size:13px;'>{footer}</p>
        </td></tr>
        <!-- Footer -->
        <tr><td style='background:#f9f9f9;padding:20px 40px;text-align:center;border-top:1px solid #eee;'>
          <p style='margin:0;font-size:12px;color:#aaa;'>
            City of Johannesburg &bull; Property Branch Data Section<br/>
            {_app.SupportPhone} &bull; {_app.SupportEmail}
          </p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";


        public async Task SendObjectionAcknowledgementAsync(
     string objectionRef,
     string rollSource,
     bool isAppeal,
     byte[] acknowledgementPdf,
     string folderPath,
     List<EmailAttachment>? extraAttachments = null)
        {
            try
            {
                // 1. Resolve recipients from Obj_Section1
                var recipients = await ResolveRecipientsAsync(objectionRef, rollSource);
                if (!recipients.Any())
                {
                    _logger.LogWarning(
                        "[Email] No valid email addresses found for {ObjRef} — skipping.",
                        objectionRef);
                    return;
                }

                var rollTitle = RollTitles.GetValueOrDefault(rollSource, rollSource);
                var actionWord = isAppeal ? "Appeal" : "Objection";
                var subject = $"City of Johannesburg — {actionWord} Acknowledgement: {objectionRef}";

                // 2. Build HTML body
                var htmlBody = BuildHtmlBody(
                    objectionRef,
                    rollTitle,
                    isAppeal,
                    recipients);

                // 3. Build PDF copy of email notification
                var emailRecordPdf = BuildEmailRecordPdf(
                    objectionRef,
                    rollTitle,
                    isAppeal,
                    recipients);

                // 4. Save EML copy to folder
                _ = SaveEmailCopyAsync(
                    folderPath,
                    objectionRef,
                    actionWord,
                    htmlBody,
                    acknowledgementPdf,
                    recipients);

                // 5. Send email to each recipient with acknowledgement + extra PDFs
                foreach (var recipient in recipients)
                {
                    await SendMailAsync(
                        recipient,
                        subject,
                        htmlBody,
                        acknowledgementPdf,
                        objectionRef,
                        isAppeal,
                        extraAttachments);
                }

                _logger.LogInformation(
                    "[Email] Sent {Count} acknowledgement email(s) for {ObjRef}",
                    recipients.Count,
                    objectionRef);
            }
            catch (Exception ex)
            {
                // Never crash the caller — email is best-effort
                _logger.LogError(ex,
                    "[Email] Failed sending acknowledgement for {ObjRef}", objectionRef);
            }
        }

        public async Task SendSection78AcknowledgementAsync(
      string queryRef,
      bool isReview,
      byte[] acknowledgementPdf,
      string folderPath,
      List<EmailAttachment>? extraAttachments = null)
        {
            try
            {
                // 1. Resolve recipients from Obj_Section1 in Objection_Query DB
                var recipients = await ResolveSection78RecipientsAsync(queryRef);
                if (!recipients.Any())
                {
                    _logger.LogWarning(
                        "[S78 Email] No valid addresses found for {Ref} — skipping.",
                        queryRef);
                    return;
                }

                var actionWord = isReview ? "Review" : "Query";
                var subject = $"City of Johannesburg — Section 78 {actionWord} " +
                              $"Acknowledgement: {queryRef}";

                // 2. Build HTML body
                var htmlBody = BuildSection78HtmlBody(
                    queryRef,
                    isReview,
                    recipients);

                // 3. Save .eml copy to folder
                _ = SaveEmailCopyAsync(
                    folderPath,
                    queryRef,
                    $"S78_{actionWord}",
                    htmlBody,
                    acknowledgementPdf,
                    recipients);

                // 4. Send to each recipient
                foreach (var recipient in recipients)
                {
                    try
                    {
                        using var msg = new MailMessage();

                        msg.From = new MailAddress(_cfg.FromAddress, _cfg.FromName);
                        msg.To.Add(new MailAddress(recipient.Address, recipient.Name));
                        msg.CC.Add(new MailAddress(
                            _cfg.FromAddress,
                            "Valuation Services (Copy)"));

                        msg.Subject = subject;
                        msg.IsBodyHtml = true;
                        msg.Body = htmlBody;

                        // Attach acknowledgement PDF
                        var pdfStream = new MemoryStream(acknowledgementPdf);
                        var attachment = new Attachment(
                            pdfStream,
                            $"S78_{actionWord}_Acknowledgement_{queryRef}.pdf",
                            MediaTypeNames.Application.Pdf);

                        msg.Attachments.Add(attachment);

                        // Attach submitted form PDF or any extra PDFs
                        AddExtraAttachments(msg, extraAttachments);

                        using var smtp = BuildClient();
                        await smtp.SendMailAsync(msg);

                        _logger.LogInformation(
                            "[S78 Email] Sent {Action} acknowledgement to {Addr} for {Ref}",
                            actionWord,
                            recipient.Address,
                            queryRef);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "[S78 Email] Failed sending to {Addr} for {Ref}",
                            recipient.Address,
                            queryRef);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[S78 Email] Failed acknowledgement for {Ref}", queryRef);
            }
        }

        // ── Helper: resolve recipients from Objection_Query DB ──────────────
        private async Task<List<EmailRecipient>> ResolveSection78RecipientsAsync(
            string queryRef)
        {
            var connStr = _config.GetConnectionString("QueryConnection")!;
            try
            {
                await using var conn = new SqlConnection(connStr);
                var section1 = await conn.QueryFirstOrDefaultAsync(
                    @"SELECT TOP 1
                Owner_Name,       Owner_Email,
                Objector_Name,    Objector_Email, Objector_Status,
                Representative_name, Rep_Email
              FROM dbo.Obj_Section1
              WHERE Objection_Ref_S1 = @Ref",
                    new { Ref = queryRef.Trim() });

                if (section1 is null) return new();

                var list = new List<EmailRecipient>();
                var status = section1.Objector_Status?.ToString()?.Trim() ?? string.Empty;

                if (status.Equals("Representative", StringComparison.OrdinalIgnoreCase))
                {
                    TryAdd(list,
                        section1.Owner_Name?.ToString(),
                        section1.Owner_Email?.ToString());
                    TryAdd(list,
                        section1.Representative_name?.ToString(),
                        section1.Rep_Email?.ToString());
                }
                else if (status.Equals("Owner", StringComparison.OrdinalIgnoreCase))
                {
                    TryAdd(list,
                        section1.Owner_Name?.ToString(),
                        section1.Owner_Email?.ToString());
                }
                else
                {
                    TryAdd(list,
                        section1.Owner_Name?.ToString(),
                        section1.Owner_Email?.ToString());
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[S78 Email] Error reading Obj_Section1 for {Ref}", queryRef);
                return new();
            }
        }

        // ── HTML body for S78 ────────────────────────────────────────────────
        private string BuildSection78HtmlBody(
            string queryRef,
            bool isReview,
            List<EmailRecipient> recipients)
        {
            var actionWord = isReview ? "Review" : "Query";
            var recipientName = recipients.FirstOrDefault()?.Name ?? "Applicant";
            var date = DateTime.Now.ToString("dd MMMM yyyy HH:mm");

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/>
<style>
  body  {{ margin:0; padding:0; background:#f5f5f5;
           font-family:Arial,sans-serif; }}
  .wrap {{ max-width:640px; margin:32px auto; background:#fff;
           border-radius:8px; overflow:hidden;
           box-shadow:0 2px 8px rgba(0,0,0,.08); }}
  .hdr  {{ background:#e6b000; padding:28px 32px; text-align:center; }}
  .hdr h1 {{ margin:0; font-size:20px; color:#1a1a1a; }}
  .hdr p  {{ margin:4px 0 0; font-size:13px; color:#3a3a3a; }}
  .body {{ padding:28px 32px; }}
  .ref  {{ background:#f7f7f7; border-radius:8px;
           border-left:4px solid #e6b000;
           padding:16px 20px; margin:20px 0; }}
  .ref span {{ font-size:22px; font-weight:700;
               color:#1a1a1a; letter-spacing:1px; }}
  .notice {{ background:#fffbeb; border:1px solid #f59e0b;
             border-radius:6px; padding:14px 18px;
             font-size:13px; color:#78350f; margin:16px 0; }}
  .ftr  {{ background:#1a1a1a; padding:20px 32px;
           text-align:center; color:#aaa; font-size:12px; }}
  .ftr a {{ color:#e6b000; text-decoration:none; }}
</style>
</head>
<body>
<div class='wrap'>
  <div class='hdr'>
    <h1>City of Johannesburg</h1>
    <p>Valuation Services — Section 78 {actionWord} Acknowledgement</p>
  </div>
  <div class='body'>
    <p>Dear <strong>{recipientName}</strong>,</p>
    <p>Your Section 78 <strong>{actionWord.ToLower()}</strong> has been
       successfully received by the City of Johannesburg Valuation Services
       Department.</p>
 
    <div class='ref'>
      <div style='font-size:12px;color:#666;margin-bottom:4px;
                  text-transform:uppercase;letter-spacing:.5px;'>
        Reference Number
      </div>
      <span>{queryRef}</span>
    </div>
 
    <table style='width:100%;border-collapse:collapse;
                  font-size:13px;margin:16px 0;'>
      <tr>
        <td style='padding:8px 0;color:#666;width:40%;'>
          Submission Type
        </td>
        <td style='padding:8px 0;font-weight:600;'>
          Section 78 {actionWord}
        </td>
      </tr>
      <tr>
        <td style='padding:8px 0;color:#666;'>Date Submitted</td>
        <td style='padding:8px 0;font-weight:600;'>{date}</td>
      </tr>
      <tr>
        <td style='padding:8px 0;color:#666;'>Status</td>
        <td style='padding:8px 0;'>
          <span style='background:#fef3c7;color:#92400e;
                       padding:3px 10px;border-radius:12px;
                       font-size:12px;font-weight:600;'>
            {actionWord}-Lodging
          </span>
        </td>
      </tr>
    </table>
 
    <div class='notice'>
      <strong>Please keep your reference number</strong> ({queryRef})
      for all future correspondence regarding this {actionWord.ToLower()}.
      Your official acknowledgement document is attached to this email.
    </div>
 
    <p>If you have any queries, please contact:</p>
    <ul style='font-size:13px;color:#333;'>
      <li>Email:
        <a href='mailto:valuationenquiries@joburg.org.za'
           style='color:#e6b000;'>
          valuationenquiries@joburg.org.za
        </a>
      </li>
      <li>Tel: 011 084 9823</li>
    </ul>
  </div>
  <div class='ftr'>
    <p>City of Johannesburg — Valuation Services Department</p>
    <p>This is an automated acknowledgement. Please do not reply directly.</p>
  </div>
</div>
</body>
</html>";
        }


        // ════════════════════════════════════════════════════════════
        //  RESOLVE RECIPIENTS from Obj_Section1
        // ════════════════════════════════════════════════════════════
        private async Task<List<EmailRecipient>> ResolveRecipientsAsync(
            string objectionRef, string rollSource)
        {
            var connKey = RollConnections.GetValueOrDefault(
                rollSource, "DefaultConnection");
            var connStr = _config.GetConnectionString(connKey)!;

            try
            {
                await using var conn = new SqlConnection(connStr);

                var section1 = await conn.QueryFirstOrDefaultAsync(
                    @"SELECT TOP 1
                    Owner_Name, Owner_Email,
                    Objector_Name, Objector_Email, Objector_Status,
                    Representative_name, Rep_Email
                  FROM dbo.Obj_Section1
                  WHERE Objection_Ref_S1 = @Ref",
                    new { Ref = objectionRef.Trim() });

                if (section1 is null)
                {
                    _logger.LogWarning(
                        "[Email] Obj_Section1 not found for {ObjRef}", objectionRef);
                    return new();
                }

                var status = section1.Objector_Status?.ToString()?.Trim() ?? string.Empty;
                var list = new List<EmailRecipient>();

                // ── Owner ─────────────────────────────────────────────
                if (status.Equals("Owner", StringComparison.OrdinalIgnoreCase))
                {
                    TryAdd(list,
                        section1.Owner_Name?.ToString(),
                        section1.Owner_Email?.ToString());
                }

                // ── Third Party ────────────────────────────────────────
                else if (status.Equals("Third_Party", StringComparison.OrdinalIgnoreCase))
                {
                    TryAdd(list,
                        section1.Objector_Name?.ToString(),
                        section1.Objector_Email?.ToString());
                }

                // ── Representative ─────────────────────────────────────
                // → send to owner AND representative
                else if (status.Equals("Representative", StringComparison.OrdinalIgnoreCase))
                {
                    TryAdd(list,
                        section1.Owner_Name?.ToString(),
                        section1.Owner_Email?.ToString());

                    TryAdd(list,
                        section1.Representative_name?.ToString(),
                        section1.Rep_Email?.ToString());
                }

                // ── Fallback (if Objector_Status is not set) ──────────
                else
                {
                    TryAdd(list,
                        section1.Owner_Name?.ToString(),
                        section1.Owner_Email?.ToString());
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Email] Error reading Obj_Section1 for {ObjRef}", objectionRef);
                return new();
            }
        }

        private static void TryAdd(List<EmailRecipient> list, string? name, string? email)
        {
            if (!string.IsNullOrWhiteSpace(email) &&
                email.Contains('@'))  // basic guard
            {
                list.Add(new EmailRecipient(
                    name?.Trim() ?? email.Trim(),
                    email.Trim()));
            }
        }

        // ════════════════════════════════════════════════════════════
        //  HTML EMAIL BODY
        // ════════════════════════════════════════════════════════════
        private static string BuildHtmlBody(
            string objectionRef,
            string rollTitle,
            bool isAppeal,
            List<EmailRecipient> recipients)
        {
            var actionWord = isAppeal ? "appeal" : "objection";
            var ActionWord = isAppeal ? "Appeal" : "Objection";
            var ActorWord = isAppeal ? "Appellant" : "Objector";
            var toName = recipients.FirstOrDefault()?.Name ?? "Valued Ratepayer";
            var now = DateTime.Now.ToString("dd MMMM yyyy HH:mm");

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0' />
<title>{ActionWord} Acknowledgement</title>
<style>
  body      {{ font-family: Arial, sans-serif; margin: 0; padding: 0;
               background: #f5f5f5; color: #1a1a1a; }}
  .wrapper  {{ max-width: 640px; margin: 32px auto; background: #fff;
               border-radius: 10px; overflow: hidden;
               box-shadow: 0 4px 20px rgba(0,0,0,.10); }}
  .header   {{ background: #e6b000; padding: 28px 32px;
               text-align: center; }}
  .header h1{{ margin: 0; font-size: 18px; color: #1a1a1a;
               font-weight: 800; letter-spacing: 1px; }}
  .header p {{ margin: 6px 0 0; font-size: 12px; color: rgba(0,0,0,.6); }}
  .body     {{ padding: 28px 32px; }}
  .greeting {{ font-size: 15px; font-weight: 700; margin-bottom: 10px; }}
  .intro    {{ font-size: 13.5px; color: #444; line-height: 1.7;
               margin-bottom: 20px; }}
  .ref-box  {{ background: #f7f7f7; border-radius: 8px;
               padding: 16px 20px; margin-bottom: 20px;
               border-left: 4px solid #e6b000; }}
  .ref-box table{{ width: 100%; border-collapse: collapse; }}
  .ref-box td   {{ padding: 5px 0; font-size: 13px; }}
  .ref-box td:first-child {{ font-weight: 700; width: 180px; color: #555; }}
  .notice   {{ background: #fffbeb; border: 1px solid #f59e0b;
               border-radius: 8px; padding: 14px 18px; font-size: 13px;
               color: #92400e; margin-bottom: 20px; line-height: 1.6; }}
  .footer   {{ background: #1a1a1a; padding: 20px 32px; text-align: center;
               color: rgba(255,255,255,.5); font-size: 11.5px; }}
  .footer a {{ color: #e6b000; text-decoration: none; }}
  .divider  {{ border: none; border-top: 1px solid #eeeeee; margin: 20px 0; }}
</style>
</head>
<body>
<div class='wrapper'>
 
  <!-- Header -->
  <div class='header'>
    <h1>City of Johannesburg</h1>
    <p>Valuation Services Department — {ActionWord} Acknowledgement</p>
  </div>
 
  <!-- Body -->
  <div class='body'>
    <p class='greeting'>Dear {toName},</p>
    <p class='intro'>
      Thank you for submitting your property {actionWord} through the
      City of Johannesburg Valuation Portal. This email confirms that
      your {actionWord} has been successfully received and recorded.
    </p>
 
    <!-- Reference details -->
    <div class='ref-box'>
      <table>
        <tr>
          <td>{ActionWord} Reference:</td>
          <td><strong>{objectionRef}</strong></td>
        </tr>
        <tr>
          <td>Valuation Roll:</td>
          <td>{rollTitle}</td>
        </tr>
        <tr>
          <td>Submission Date:</td>
          <td>{now}</td>
        </tr>
        <tr>
          <td>{ActorWord} Type:</td>
          <td>{(recipients.Count > 1 ? "Representative" : "See attached")}</td>
        </tr>
      </table>
    </div>
 
    <hr class='divider' />
 
    <!-- Important notice -->
    <div class='notice'>
      <strong>⏰ Important:</strong> You have <strong>48 hours</strong>
      from your submission time to upload any additional supporting evidence.
      Log into the portal and use the <em>Add Evidence</em> function.
    </div>
 
    <p style='font-size:13px;color:#444;line-height:1.7;'>
      Please find your official acknowledgement document attached to this email.
      Keep it for your records as proof of submission.
    </p>
 
    <p style='font-size:13px;color:#444;margin-top:16px;'>
      For enquiries please contact us:<br />
      <strong>Tel:</strong> 011 407-6622 / 011 407-6597<br />
      <strong>Email:</strong>
      <a href='mailto:valuationenquiries@joburg.org.za'>
        valuationenquiries@joburg.org.za
      </a>
    </p>
  </div>
 
  <!-- Footer -->
  <div class='footer'>
    City of Johannesburg Valuation Services Department<br />
    This is an automated email — please do not reply directly.<br />
    &copy; {DateTime.Now.Year} City of Johannesburg. All rights reserved.
  </div>
 
</div>
</body>
</html>";
        }

        // ════════════════════════════════════════════════════════════
        //  SEND INDIVIDUAL EMAIL via System.Net.Mail
        // ════════════════════════════════════════════════════════════
        private async Task SendMailAsync(
        EmailRecipient recipient,
        string subject,
        string htmlBody,
        byte[] pdfAttachment,
        string objectionRef,
        bool isAppeal,
        List<EmailAttachment>? extraAttachments = null)
        {
            using var msg = new MailMessage();

            msg.From = new MailAddress(_cfg.FromAddress, _cfg.FromName);
            msg.To.Add(new MailAddress(recipient.Address, recipient.Name));
            msg.Subject = subject;
            msg.IsBodyHtml = true;
            msg.Body = htmlBody;

            // Attach the acknowledgement PDF
            var pdfName = $"{(isAppeal ? "Appeal" : "Objection")}_Acknowledgement_{objectionRef}.pdf";

            var pdfStream = new MemoryStream(pdfAttachment);
            var attachment = new Attachment(
                pdfStream,
                pdfName,
                MediaTypeNames.Application.Pdf);

            msg.Attachments.Add(attachment);

            // Attach submitted form PDF or any extra PDFs
            AddExtraAttachments(msg, extraAttachments);

            using var client = new SmtpClient(_cfg.Host, _cfg.Port)
            {
                EnableSsl = _cfg.EnableSsl,
                Credentials = new NetworkCredential(_cfg.SmtpUser, _cfg.Password)
            };

            await client.SendMailAsync(msg);

            _logger.LogInformation(
                "[Email] Sent {Action} acknowledgement to {Addr} for {Ref}",
                isAppeal ? "Appeal" : "Objection",
                recipient.Address,
                objectionRef);
        }

        private static void AddExtraAttachments(
    MailMessage msg,
    List<EmailAttachment>? extraAttachments)
        {
            if (extraAttachments == null || !extraAttachments.Any())
                return;

            foreach (var item in extraAttachments)
            {
                if (item == null)
                    continue;

                if (item.FileBytes == null || item.FileBytes.Length == 0)
                    continue;

                if (string.IsNullOrWhiteSpace(item.FileName))
                    continue;

                var stream = new MemoryStream(item.FileBytes);

                var attachment = new Attachment(
                    stream,
                    item.FileName,
                    string.IsNullOrWhiteSpace(item.ContentType)
                        ? MediaTypeNames.Application.Pdf
                        : item.ContentType);

                msg.Attachments.Add(attachment);
            }
        }

        // ════════════════════════════════════════════════════════════
        //  BUILD EMAIL RECORD PDF (QuestPDF) — saved to folder
        // ════════════════════════════════════════════════════════════
        private byte[] BuildEmailRecordPdf(
            string objectionRef,
            string rollTitle,
            bool isAppeal,
            List<EmailRecipient> recipients)
        {
            var actionWord = isAppeal ? "Appeal" : "Objection";

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9));

                    page.Content().Column(col =>
                    {
                        // Header bar
                        col.Item().Background("#e6b000").Padding(16).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("CITY OF JOHANNESBURG")
                                    .FontSize(14).Bold().FontColor("#1a1a1a");
                                c.Item().Text("Valuation Services — Email Notification Record")
                                    .FontSize(9).FontColor(Colors.Black);
                            });
                        });

                        col.Item().Height(12);

                        // Title
                        col.Item().AlignCenter()
                            .Text($"{actionWord.ToUpper()} ACKNOWLEDGEMENT EMAIL RECORD")
                            .FontSize(11).Bold();

                        col.Item().Height(6);
                        col.Item().BorderBottom(1).BorderColor("#cccccc");
                        col.Item().Height(10);

                        // Email metadata box
                        col.Item().Background("#f7f7f7").BorderLeft(4).BorderColor("#e6b000")
                            .Padding(10).Column(meta =>
                            {
                                void Row(string label, string value)
                                {
                                    meta.Item().Row(r =>
                                    {
                                        r.ConstantItem(150)
                                        .Text(label).Bold().FontSize(9);
                                        r.RelativeItem()
                                        .Text(value).FontSize(9);
                                    });
                                    meta.Item().Height(3);
                                }

                                Row($"{actionWord} Reference:", objectionRef);
                                Row("Valuation Roll:", rollTitle);
                                Row("Sent Date/Time:", DateTime.Now.ToString("dd MMMM yyyy HH:mm"));
                                Row("Sent From:", _cfg.FromAddress);
                            });

                        col.Item().Height(12);

                        // Recipients table
                        col.Item().Text("EMAIL RECIPIENTS").Bold().FontSize(9);
                        col.Item().Height(6);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(30);
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                            });

                            static IContainer TH(IContainer c) =>
                                c.Background("#1a1a1a").Padding(6);

                            table.Cell().Element(TH)
                                .Text("#").FontColor(Colors.White).Bold().FontSize(8);
                            table.Cell().Element(TH)
                                .Text("Name").FontColor(Colors.White).Bold().FontSize(8);
                            table.Cell().Element(TH)
                                .Text("Email Address").FontColor(Colors.White).Bold().FontSize(8);

                            bool alt = false;
                            for (int i = 0; i < recipients.Count; i++)
                            {
                                var bg = alt ? Colors.Grey.Lighten5 : Colors.White;
                                alt = !alt;
                                IContainer TD(IContainer c) =>
                                    c.Background(bg).BorderBottom(0.5f)
                                     .BorderColor("#eeeeee").Padding(5);

                                table.Cell().Element(TD).Text((i + 1).ToString()).FontSize(8);
                                table.Cell().Element(TD).Text(recipients[i].Name).FontSize(8);
                                table.Cell().Element(TD).Text(recipients[i].Address).FontSize(8);
                            }
                        });

                        col.Item().Height(12);

                        // Note
                        col.Item().Background("#fffbeb").Border(1)
                            .BorderColor("#f59e0b").Padding(8)
                           
                          .DefaultTextStyle(x => x.FontSize(8))
                            .Text(t =>
                            {
                                t.Span("Note: ").Bold();
                                t.Span("The official acknowledgement PDF was attached to each " +
                                       "email listed above. This document serves as a record " +
                                       "that the notification was dispatched.");
                            });

                        col.Item().Height(14);

                        // Footer
                        col.Item().BorderTop(1).BorderColor("#cccccc").PaddingTop(6)
                            .AlignCenter()
                            .Text($"Generated by Genesis V2 — City of Johannesburg " +
                                  $"Valuation Services — {DateTime.Now:dd MMMM yyyy HH:mm}")
                            .FontSize(7).FontColor("#888888");
                    });
                });
            }).GeneratePdf();
        }

        // ════════════════════════════════════════════════════════════
        //  SAVE PDF COPY TO FOLDER
        // ════════════════════════════════════════════════════════════
        private async Task SaveEmailCopyAsync(
       string folderPath,
       string reference,
       string actionWord,
       string htmlBody,
       byte[] ackPdf,
       List<EmailRecipient> recipients)
        {
            try
            {
                Directory.CreateDirectory(folderPath);

                var safe = string.Join("_",
                    reference.Split(Path.GetInvalidFileNameChars()));
                var fileName = $"{safe}_{actionWord}_EmailNotification.eml";
                var fullPath = Path.Combine(folderPath, fileName);

                // Build the MailMessage
                using var msg = new MailMessage();
                msg.From = new MailAddress(_cfg.FromAddress, _cfg.FromName);
                msg.Subject = $"City of Johannesburg — {actionWord} Acknowledgement: {reference}";
                msg.IsBodyHtml = true;
                msg.Body = htmlBody;

                foreach (var r in recipients)
                    msg.To.Add(new MailAddress(r.Address, r.Name));

                // CC admin inbox
                msg.CC.Add(new MailAddress(
                    _cfg.FromAddress, "Valuation Services (Copy)"));

                // Attach the acknowledgement PDF
                var pdfStream = new MemoryStream(ackPdf);
                var attachment = new Attachment(
                    pdfStream,
                    $"{actionWord}_Acknowledgement_{reference}.pdf",
                    MediaTypeNames.Application.Pdf);
                msg.Attachments.Add(attachment);

                // Use SmtpClient pickup-directory trick to write a real .eml file
                var tmpDir = Path.Combine(
                    Path.GetTempPath(), "eml_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmpDir);

                using (var pickup = new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                    PickupDirectoryLocation = tmpDir
                })
                {
                    pickup.Send(msg);   // synchronous — writes .eml to tmpDir
                }

                var generated = Directory.GetFiles(tmpDir).FirstOrDefault();
                if (generated is not null)
                    File.Move(generated, fullPath, overwrite: true);

                Directory.Delete(tmpDir, recursive: true);

                _logger.LogInformation(
                    "[Email] EML copy saved → {Path}", fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Email] Failed saving EML copy for {Ref}", reference);
            }
        }
        public async Task SendEmailWithAttachmentsAsync(
        string toEmail,
        string subject,
        string body,
        List<EmailAttachment> attachments,
        bool isHtml = true)
        {
            try
            {
                using var smtp = BuildClient();

                using var msg = new MailMessage
                {
                    From = new MailAddress(_cfg.Username, _cfg.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                msg.To.Add(toEmail);

                if (attachments != null && attachments.Any())
                {
                    foreach (var item in attachments)
                    {
                        if (item == null)
                            continue;

                        if (item.FileBytes == null || item.FileBytes.Length == 0)
                            continue;

                        if (string.IsNullOrWhiteSpace(item.FileName))
                            continue;

                        var stream = new MemoryStream(item.FileBytes);

                        var attachment = new Attachment(
                            stream,
                            item.FileName,
                            string.IsNullOrWhiteSpace(item.ContentType)
                                ? MediaTypeNames.Application.Pdf
                                : item.ContentType);

                        msg.Attachments.Add(attachment);
                    }
                }

                await smtp.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send email with attachments to {Email}", toEmail);

                throw;
            }
        }
        public async Task SendEmailWithAttachmentAsync(
    string toEmail,
    string subject,
    string htmlBody,
    byte[] attachmentBytes,
    string attachmentFileName)
        {
            var attachments = new List<EmailAttachment>
    {
        new EmailAttachment
        {
            FileName = attachmentFileName,
            FileBytes = attachmentBytes,
            ContentType = MediaTypeNames.Application.Pdf
        }
    };

            await SendEmailWithAttachmentsAsync(
                toEmail,
                subject,
                htmlBody,
                attachments,
                true);
        }
    }

}


