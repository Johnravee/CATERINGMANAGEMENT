using System;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Templates;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.Mailer
{
    internal class ContractMailer
    {
        private readonly EmailService _emailService;

        public ContractMailer(EmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<bool> SendContractEmailAsync(string recipientEmail, string recipientName, string eventDate, string attachmentPath)
        {
            string subject = "OSHDY Event Catering Services Contract Agreement";

            string body = ContractEmailTemplate.GetHtmlBody(recipientName, eventDate, _emailService.GetFromEmail());

            return await _emailService.SendEmailAsync(recipientEmail, subject, body, isHtml: true, attachmentPath: attachmentPath);
        }
    }
}
