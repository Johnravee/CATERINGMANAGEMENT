using PdfSharp.Pdf;
using PdfSharp.Drawing;
using CATERINGMANAGEMENT.Models;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace CATERINGMANAGEMENT.DocumentsGenerator
{
    internal class UserPayslipPdfGenerator
    {
  
        public static void Generate(List<Payroll> payrolls, string workerName, DateTime startDate, DateTime endDate)
        {
            if (payrolls == null || payrolls.Count == 0)
                throw new ArgumentException("No payroll data provided.");

            var saveDialog = new SaveFileDialog
            {
                Title = "Save Payroll Contract",
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Payroll_Contract_{workerName}_{startDate:yyyyMMdd}.pdf"
            };

            if (saveDialog.ShowDialog() != true)
                return;

            using (var doc = new PdfDocument())
            {
                var page = doc.AddPage();
                page.Orientation = PdfSharp.PageOrientation.Landscape;

                var gfx = XGraphics.FromPdfPage(page);
                double margin = 40;
                double y = margin;
                double contentWidth = page.Width - 2 * margin;
                double lineHeight = 25;

                var titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
                var subTitleFont = new XFont("Arial", 14, XFontStyleEx.Bold);
                var headerFont = new XFont("Arial", 12, XFontStyleEx.Bold);
                var font = new XFont("Arial", 11);

                // --- Logo and Title Section ---
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", "oshdylogo.jpg");
                double logoWidth = 80;
                double logoHeight = 0;

                if (File.Exists(logoPath))
                {
                    var logo = XImage.FromFile(logoPath);
                    logoHeight = (logo.PixelHeight * logoWidth) / logo.PixelWidth;
                    gfx.DrawImage(logo, margin, y, logoWidth, logoHeight);
                }

                // Position the title text to the right of the logo
                double textX = margin + logoWidth + 20;
                double textWidth = page.Width - textX - margin;

                // Draw company name
                gfx.DrawString("OSHDY Event Catering Services", titleFont, XBrushes.Black,
                    new XRect(textX, y, textWidth, 30), XStringFormats.TopLeft);
                y += 30;

                // Draw payroll contract subtitle
                gfx.DrawString("Employee Payslip", subTitleFont, XBrushes.Black,
                    new XRect(textX, y, textWidth, 25), XStringFormats.TopLeft);

                y += Math.Max(logoHeight, 50); // move below logo if logo is taller

                // --- Employee Details ---
                gfx.DrawString($"Employee Name: {workerName}", font, XBrushes.Black, new XPoint(margin, y)); y += lineHeight;
                gfx.DrawString($"Date Issued: {DateTime.Now:MMMM dd, yyyy}", font, XBrushes.Black, new XPoint(margin, y)); y += lineHeight;
                gfx.DrawString($"Cutoff:({startDate:MMM d} - {endDate:MMM d, yyyy})", font, XBrushes.Black, new XPoint(margin, y)); y += 30;

                // --- Column Headers ---
                double[] colWidths = {
                    contentWidth * 0.30, // Receipt No.
                    contentWidth * 0.15, // Event Date
                    contentWidth * 0.15, // Gross Pay
                    contentWidth * 0.20, // Paid Date
                    contentWidth * 0.20  // Status
                };
                string[] headers = { "Receipt No.", "Event Date", "Gross Pay", "Paid Date", "Status" };

                void DrawTableHeader()
                {
                    double x = margin;
                    for (int i = 0; i < headers.Length; i++)
                    {
                        gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, x, y, colWidths[i], lineHeight);
                        gfx.DrawString(headers[i], headerFont, XBrushes.Black,
                            new XRect(x + 5, y + 5, colWidths[i] - 10, lineHeight), XStringFormats.TopLeft);
                        x += colWidths[i];
                    }
                    y += lineHeight;
                }

                void DrawTableRow(params string[] values)
                {
                    double x = margin;
                    for (int i = 0; i < values.Length; i++)
                    {
                        gfx.DrawRectangle(XPens.Black, x, y, colWidths[i], lineHeight);
                        gfx.DrawString(values[i], font, XBrushes.Black,
                            new XRect(x + 5, y + 5, colWidths[i] - 10, lineHeight), XStringFormats.TopLeft);
                        x += colWidths[i];
                    }
                    y += lineHeight;
                }

                // --- Table Content ---
                DrawTableHeader();

                decimal total = 0;

                foreach (var p in payrolls)
                {
                    if (y + lineHeight > page.Height - margin)
                    {
                        // Add new page
                        page = doc.AddPage();
                        page.Orientation = PdfSharp.PageOrientation.Landscape;
                        gfx = XGraphics.FromPdfPage(page);
                        y = margin;
                        DrawTableHeader();
                    }

                    string receipt = p.Reservation?.ReceiptNumber ?? "-";
                    string eventDate = p.Reservation?.EventDate.ToString("MMM dd") ?? "-";
                    string gross = (p.GrossPay ?? 0).ToString("C");
                    string paidDate = p.PaidDate?.ToString("MMM dd, yyyy") ?? "-";
                    string status = p.PaidDate != null ? "Paid" : "Unpaid";

                    DrawTableRow(receipt, eventDate, gross, paidDate, status);
                    total += p.GrossPay ?? 0;
                }

                // --- Footer: Total + Signature ---
                y += 10;
                gfx.DrawString($"Total Earnings: {total:C}", headerFont, XBrushes.Black, new XPoint(margin, y));
                y += 40;

                gfx.DrawString("Employee Signature: __________________________", font, XBrushes.Black, new XPoint(margin, y));

                // Save PDF
                doc.Save(saveDialog.FileName);
                MessageBox.Show("Payroll contract PDF generated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
    