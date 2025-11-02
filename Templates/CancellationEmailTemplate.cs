using CATERINGMANAGEMENT.Models;
using System;

namespace CATERINGMANAGEMENT.Templates
{
    internal static class CancellationEmailTemplate
    {
        public static string GetHtmlBody(string adminName, Reservation reservation, string reason, string fromEmail)
        {
            var eventDate = reservation.EventDate.ToString("MMMM dd, yyyy");
            return $@"
<html>
<head>
  <meta charset='UTF-8'>
  <style>
    body {{ font-family: Arial, sans-serif; color: #333; background: #f9f9f9; padding: 20px; }}
    .container {{ background: #fff; padding: 20px; border-radius: 8px; max-width: 600px; margin: auto; box-shadow: 0 2px 8px rgba(0,0,0,0.06); }}
    h2 {{ color: #c62828; }}
    .muted {{ color: #666; font-size: 13px; }}
  </style>
</head>
<body>
  <div class='container'>
    <h2>Reservation Canceled</h2>
    <p>Hi {adminName},</p>
    <p>The following reservation has been canceled via the admin console:</p>
    <ul>
      <li><strong>Receipt #:</strong> {reservation.ReceiptNumber}</li>
      <li><strong>Client:</strong> {reservation.Profile?.FullName ?? "-"}</li>
      <li><strong>Event Date:</strong> {eventDate}</li>
      <li><strong>Venue:</strong> {reservation.Venue}</li>
    </ul>
    <p><strong>Cancellation Reason (not stored):</strong></p>
    <p style='background:#f4f4f4;padding:10px;border-radius:4px'>{(string.IsNullOrWhiteSpace(reason) ? "(none provided)" : System.Net.WebUtility.HtmlEncode(reason))}</p>

    <p class='muted'>This is an automated notification from OSHDY Event Catering Services. For questions please reply to <a href='mailto:{fromEmail}'>{fromEmail}</a>.</p>
  </div>
</body>
</html>";
        }
    }
}
