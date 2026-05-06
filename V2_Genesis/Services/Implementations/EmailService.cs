using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using V2_Genesis.Models.Emails;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _cfg;
        private readonly AppSettings _app;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> emailOpts,
            IOptions<AppSettings> appOpts,
            ILogger<EmailService> logger)
        {
            _cfg = emailOpts.Value;
            _app = appOpts.Value;
            _logger = logger;
        }

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
    }

}
