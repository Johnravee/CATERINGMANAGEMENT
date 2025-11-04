using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Templates;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.Mailer
{
    internal class RemoveWorkerMailer
    {
        private readonly EmailService _emailService;

        public RemoveWorkerMailer(EmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<bool> SendWorkerRemovalEmailAsync(
            string workerEmail,
            string workerName,
            string workerRole,
            string eventName,
            string eventDate,
            string eventVenue)
        {
            string subject = $"OSHDY Notification: Removed from {eventName} on {eventDate}";

            string body = RemoveWorkerEmailTemplate.GetHtmlBody(
                workerName,
                eventName,
                workerRole,
                eventDate,
                eventVenue,
                _emailService.GetFromEmail()
            );

            return await _emailService.SendEmailAsync(workerEmail, subject, body, isHtml: true);
        }
    }
}