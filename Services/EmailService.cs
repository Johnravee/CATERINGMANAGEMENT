using System;
using System.Net;
using System.Net.Mail;
using CATERINGMANAGEMENT.Helpers;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.Services
{
    public class EmailService
    {
        private readonly string _fromEmail;
        private readonly string _appPassword;

        public EmailService()
        {
            _fromEmail = Environment.GetEnvironmentVariable("GMAIL") ?? string.Empty;
            _appPassword = Environment.GetEnvironmentVariable("GMAIL_PASSWORD") ?? string.Empty;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false, string? attachmentPath = null)
        {
            if (string.IsNullOrWhiteSpace(_fromEmail) || string.IsNullOrWhiteSpace(_appPassword))
            {
                AppLogger.Error("EmailService: GMAIL or GMAIL_PASSWORD environment variable is not configured.", showToUser: false);
                return false;
            }

            try
            {
                using (var message = new MailMessage(_fromEmail, toEmail, subject, body))
                {
                    message.IsBodyHtml = isHtml;

                    if (!string.IsNullOrEmpty(attachmentPath))
                    {
                        message.Attachments.Add(new Attachment(attachmentPath));
                    }

                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.EnableSsl = true;
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(_fromEmail, _appPassword);

                        await smtp.SendMailAsync(message);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Email send failed via SMTP (EmailService).", showToUser: false);
                return false;
            }
        }


        public string GetFromEmail() => _fromEmail;
    }
}