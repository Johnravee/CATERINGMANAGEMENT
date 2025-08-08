using System;
using System.Net;
using System.Net.Mail;

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

        public bool SendEmail(string toEmail, string subject, string body, bool isHtml = false, string? attachmentPath = null)
        {
            try
            {
                using (MailMessage message = new MailMessage(_fromEmail, toEmail, subject, body))
                {
                    message.IsBodyHtml = isHtml;

                    if (!string.IsNullOrEmpty(attachmentPath))
                    {
                        message.Attachments.Add(new Attachment(attachmentPath));
                    }

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.EnableSsl = true;
                        smtp.Credentials = new NetworkCredential(_fromEmail, _appPassword);
                        smtp.Send(message);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email send failed: " + ex.Message);
                return false;
            }
        }

        public string GetFromEmail() => _fromEmail;
    }
}
