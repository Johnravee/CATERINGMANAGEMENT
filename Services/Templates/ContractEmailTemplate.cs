using System;

namespace CATERINGMANAGEMENT.Services.Templates
{
    internal static class ContractEmailTemplate
    {
        /// <summary>
        /// Generates the HTML email body for a catering contract.
        /// </summary>
        public static string GetHtmlBody(string recipientName, string eventDate, string senderEmail)
        {
            return $@"
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <style>
                            body {{
                                font-family: 'Segoe UI', Arial, sans-serif;
                                font-size: 15px;
                                color: #333333;
                                background-color: #f4f4f4;
                                margin: 0;
                                padding: 0;
                            }}
                            .container {{
                                max-width: 600px;
                                margin: 30px auto;
                                background: #ffffff;
                                border-radius: 8px;
                                overflow: hidden;
                                box-shadow: 0 2px 8px rgba(0,0,0,0.05);
                            }}
                            .header {{
                                background-color: #F3C663;
                                color: #ffffff;
                                padding: 20px;
                                text-align: center;
                            }}
                            .header h1 {{
                                margin: 0;
                                font-size: 22px;
                            }}
                            .content {{
                                padding: 25px;
                            }}
                            .content h2 {{
                                color: #2C3E50;
                                font-size: 18px;
                                margin-bottom: 15px;
                            }}
                            .content p {{
                                line-height: 1.6;
                                margin: 10px 0;
                            }}
                            .content b {{
                                color: #000000;
                            }}
                            .footer {{
                                background-color: #f0f0f0;
                                color: #777777;
                                font-size: 12px;
                                text-align: center;
                                padding: 15px;
                            }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>OSHDY Event Catering Services Contract Agreement</h1>
                            </div>
                            <div class='content'>
                                <p>Hello <b>{recipientName}</b>,</p>
                                <p>
                                    Please find attached the contract for your upcoming event scheduled on 
                                    <b>{eventDate:MMMM dd, yyyy}</b>.
                                </p>
                                <p>
                                    If you have any questions, feel free to contact us.
                                </p>
                                <p>Best regards,<br>{senderEmail}</p>
                            </div>
                            <div class='footer'>
                                This is an automated message from Your Company. Please do not reply directly to this email.
                            </div>
                        </div>
                    </body>
                    </html>";
            }
    }
}
