using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.Services
{
    public class EmailService
    {
        private readonly string _fromEmail;
        private readonly string _appPassword;

        // Tunables (can be moved to config/env if you prefer)
        private const int SmtpTimeoutMs = 15000;     // 15s per attempt
        private const int MaxRetries = 2;            // total attempts = MaxRetries + 1
        private const int InitialBackoffMs = 1000;   // 1s, doubles each retry, capped

        public EmailService()
        {
            _fromEmail = Environment.GetEnvironmentVariable("GMAIL") ?? string.Empty;
            _appPassword = Environment.GetEnvironmentVariable("GMAIL_PASSWORD") ?? string.Empty;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false, string? attachmentPath = null)
        {
            var sw = Stopwatch.StartNew();
            int attempt = 0;
            int backoff = InitialBackoffMs;
            Exception? lastException = null;

            try
            {
                do
                {
                    try
                    {
                        using (var message = new MailMessage(_fromEmail, toEmail, subject, body))
                        {
                            message.IsBodyHtml = isHtml;

                            if (!string.IsNullOrEmpty(attachmentPath))
                                message.Attachments.Add(new Attachment(attachmentPath));

                            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                            {
                                smtp.EnableSsl = true;
                                smtp.Credentials = new NetworkCredential(_fromEmail, _appPassword);
                                smtp.Timeout = SmtpTimeoutMs;

                                await smtp.SendMailAsync(message).ConfigureAwait(false);
                            }
                        }

                        return true; // success
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        attempt++;

                        // If we've exceeded retries, break
                        if (attempt > MaxRetries)
                            break;

                        // Backoff before next attempt (exponential with cap)
                        try
                        {
                            await Task.Delay(backoff).ConfigureAwait(false);
                        }
                        catch { /* ignore delay cancellation */ }

                        backoff = Math.Min(backoff * 2, 10000); // cap at 10s
                    }
                } while (attempt <= MaxRetries);

                // All attempts failed
                Console.WriteLine($"Email send failed after {attempt} attempts: {lastException?.Message}");
                return false;
            }
            finally
            {
                sw.Stop();
                Console.WriteLine($"Email send duration (all attempts): {sw.Elapsed.TotalSeconds:F2}s to {toEmail}");
            }
        }

        public string GetFromEmail() => _fromEmail;
    }
}