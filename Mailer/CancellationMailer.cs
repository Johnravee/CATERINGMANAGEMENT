using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Templates;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.Mailer
{
    internal class CancellationMailer
    {
        private readonly EmailService _emailService;

        public CancellationMailer(EmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<bool> SendCancellationEmailAsync(string recipientEmail, string recipientName, Reservation reservation, string reason)
        {
            string subject = $"Reservation Canceled: {reservation.ReceiptNumber}";
            string body = CancellationEmailTemplate.GetHtmlBody(recipientName, reservation, reason, _emailService.GetFromEmail());
            return await _emailService.SendEmailAsync(recipientEmail, subject, body, isHtml: true);
        }
    }
}
