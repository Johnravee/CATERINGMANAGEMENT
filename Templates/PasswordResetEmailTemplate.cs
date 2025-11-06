using System;

namespace CATERINGMANAGEMENT.Templates
{
    public static class PasswordResetEmailTemplate
    {
        public static string GetHtmlBody(string recipientEmail, string resetLink, string brandName = "OSHDY Events Catering Services", string? supportEmail = null)
        {
            var safeBrand = string.IsNullOrWhiteSpace(brandName) ? "OSHDY Events Catering Services" : brandName.Trim();
            var contactLine = !string.IsNullOrWhiteSpace(supportEmail)
                ? $"If you did not request this, please ignore this email or contact us at {supportEmail}."
                : "If you did not request this, you can safely ignore this email.";

            const string template = @"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>%%BRAND%% Password Reset</title>
</head>
<body style=""margin:0;padding:0;background-color:#f5f6f8;font-family:Segoe UI, Arial, sans-serif;color:#333;"">
  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color:#f5f6f8;padding:20px 0;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""600"" style=""max-width:600px;background:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb;"">
          <tr>
            <td style=""background:#F3C663;color:#111827;padding:16px 24px;text-align:center;"">
              <h1 style=""margin:0;font-size:20px;line-height:1.4;"">%%BRAND%%</h1>
            </td>
          </tr>
          <tr>
            <td style=""padding:24px;"">
              <h2 style=""margin:0 0 8px 0;font-size:18px;color:#111827;"">Reset your password</h2>
              <p style=""margin:0 0 16px 0;font-size:14px;line-height:1.6;"">We received a request to reset the password for the account associated with <strong>%%RECIPIENT%%</strong>.</p>
              <p style=""margin:0 0 16px 0;font-size:14px;line-height:1.6;"">Click the button below to set a new password. This link will expire for your security.</p>
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin:20px 0;"">
                <tr>
                  <td align=""center""> 
                    <a href=""%%RESET_LINK%%"" style=""background:#111827;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:6px;display:inline-block;font-size:14px;"">Reset Password</a>
                  </td>
                </tr>
              </table>
              <p style=""margin:0 0 12px 0;font-size:12px;color:#6b7280;line-height:1.6;"">If the button doesn't work, copy and paste this link into your browser:</p>
              <p style=""margin:0 0 20px 0;word-break:break-all;font-size:12px;""><a href=""%%RESET_LINK%%"" style=""color:#2563eb;text-decoration:underline;"">%%RESET_LINK%%</a></p>
              <p style=""margin:0 0 8px 0;font-size:12px;color:#6b7280;line-height:1.6;"">%%CONTACT_LINE%%</p>
              <p style=""margin:16px 0 0 0;font-size:12px;color:#6b7280;"">Regards,<br/>%%BRAND%% Team</p>
            </td>
          </tr>
          <tr>
            <td style=""background:#f9fafb;color:#9ca3af;padding:12px 24px;text-align:center;font-size:11px;"">
              <p style=""margin:0;"">This is an automated message. Please do not reply.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            return template
                .Replace("%%BRAND%%", safeBrand)
                .Replace("%%RECIPIENT%%", recipientEmail ?? string.Empty)
                .Replace("%%RESET_LINK%%", resetLink ?? string.Empty)
                .Replace("%%CONTACT_LINE%%", contactLine);
        }
    }
}
