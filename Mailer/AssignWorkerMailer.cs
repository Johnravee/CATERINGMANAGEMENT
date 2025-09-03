using System;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Templates;

namespace CATERINGMANAGEMENT.Mailer
{
    internal class AssignWorkerMailer
    {
        private readonly EmailService _emailService;

        public AssignWorkerMailer(EmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<bool> SendWorkerScheduleEmailAsync(
            string workerEmail,
            string workerName,
            string workerRole,
            string eventName,
            string eventDate,
            string eventVenue)
        {
            string subject = $"OSHDY Event Schedule: {eventName} on {eventDate}";

            string body = AssignWorkersEmailTemplate.GetHtmlBody(
                workerName,
                eventName,
                workerRole,
                eventDate,
                eventVenue,
                _emailService.GetFromEmail()
            );

            return await _emailService.SendEmailAsync(
                workerEmail,
                subject,
                body,
                isHtml: true
            );
        }
    }

}
