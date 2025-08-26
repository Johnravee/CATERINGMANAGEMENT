using System;

namespace CATERINGMANAGEMENT.Templates
{
    public static class AssignWorkersEmailTemplate
    {
        public static string GetHtmlBody(string workerName, string eventName, string workerRole, string eventDate, string eventVenue, string fromEmail)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <style>
    body {{
      font-family: Arial, sans-serif;
      background-color: #f9f9f9;
      padding: 20px;
      color: #333;
    }}
    .container {{
      background-color: #ffffff;
      border-radius: 8px;
      padding: 20px;
      max-width: 600px;
      margin: auto;
      box-shadow: 0 4px 8px rgba(0,0,0,0.1);
    }}
    h2 {{
      color: #444;
    }}
    p {{
      line-height: 1.6;
    }}
    .footer {{
      margin-top: 20px;
      font-size: 12px;
      color: #777;
    }}
  </style>
</head>
<body>
  <div class='container'>
    <h2>Work Assignment Notification</h2>
    <p>Dear {workerName},</p>

    <p>
      You have been assigned to work at the event <strong>{eventName}</strong> 
      scheduled on <strong>{eventDate}</strong>.
    </p>

    <p>
      <strong>Venue:</strong> {eventVenue}<br/>
      <strong>Role:</strong> {workerRole}
    </p>

    <p>
      Please confirm your availability and arrive on time to ensure a smooth event operation.
    </p>

    <p>Thank you,<br>
       OSHDY Event Catering Services<br>
       <a href='mailto:{fromEmail}'>{fromEmail}</a></p>

    <div class='footer'>
      <p>This notification is confidential and intended only for {workerName}. 
      If you are not the intended recipient, please disregard this message.</p>
    </div>
  </div>
</body>
</html>";
        }
    }
}
