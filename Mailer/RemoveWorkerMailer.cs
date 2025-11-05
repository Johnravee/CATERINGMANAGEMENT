using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Templates;
using CATERINGMANAGEMENT.Models;
using System.Collections.Generic;
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

        public async Task NotifyWorkersRemovalAsync(IEnumerable<Worker> workers, string eventName, string eventDate, string eventVenue)
        {
            foreach (var w in workers)
            {
                if (string.IsNullOrWhiteSpace(w.Email))
                    continue;
                await SendWorkerRemovalEmailAsync(
                    w.Email,
                    w.Name ?? "Staff",
                    w.Role ?? "Staff",
                    eventName,
                    eventDate,
                    eventVenue
                );
            }
        }
    }
}