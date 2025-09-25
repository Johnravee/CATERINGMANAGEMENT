using PdfSharp.Pdf;
using PdfSharp.Drawing;
using CATERINGMANAGEMENT.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace CATERINGMANAGEMENT.DocumentsGenerator
{
    internal class PayrollPdfGenerator
    {
        public static void Generate(List<Payroll> payrolls, string reservationReceipt, DateTime eventDate)
        {
            if (payrolls == null || payrolls.Count == 0)
                throw new ArgumentException("No payroll data provided.");

            var saveDialog = new SaveFileDialog
            {
                Title = "Save Payroll Report",
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Payroll_{reservationReceipt}_{DateTime.Now:yyyyMMdd}.pdf"
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

                // Draw payroll report subtitle
                gfx.DrawString("Payroll Report", subTitleFont, XBrushes.Black,
                    new XRect(textX, y, textWidth, 25), XStringFormats.TopLeft);

                y += Math.Max(logoHeight, 50); // move below logo if logo is taller

                // --- Payroll Details ---
                gfx.DrawString($"Reservation: {reservationReceipt}", font, XBrushes.Black, new XPoint(margin, y)); y += lineHeight;
                gfx.DrawString($"Event Date: {eventDate:MMMM dd, yyyy}", font, XBrushes.Black, new XPoint(margin, y)); y += 30;

                // --- Table Headers ---
                string[] headers = { "Worker Name", "Role", "Salary" };
                double[] colWidths = {
                    contentWidth * 0.45,
                    contentWidth * 0.35,
                    contentWidth * 0.20
                };

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

                void DrawTableRow(string workerName, string role, string grossPay)
                {
                    double x = margin;
                    gfx.DrawRectangle(XPens.Black, x, y, colWidths[0], lineHeight);
                    gfx.DrawString(workerName, font, XBrushes.Black,
                        new XRect(x + 5, y + 5, colWidths[0] - 10, lineHeight), XStringFormats.TopLeft);
                    x += colWidths[0];

                    gfx.DrawRectangle(XPens.Black, x, y, colWidths[1], lineHeight);
                    gfx.DrawString(role, font, XBrushes.Black,
                        new XRect(x + 5, y + 5, colWidths[1] - 10, lineHeight), XStringFormats.TopLeft);
                    x += colWidths[1];

                    gfx.DrawRectangle(XPens.Black, x, y, colWidths[2], lineHeight);
                    gfx.DrawString(grossPay, font, XBrushes.Black,
                        new XRect(x + 5, y + 5, colWidths[2] - 10, lineHeight), XStringFormats.TopLeft);
                    y += lineHeight;
                }

                // --- Table Content ---
                DrawTableHeader();

                decimal total = 0;

                foreach (var payroll in payrolls)
                {
                    if (y + lineHeight > page.Height - margin)
                    {
                        page = doc.AddPage();
                        page.Orientation = PdfSharp.PageOrientation.Landscape;
                        gfx = XGraphics.FromPdfPage(page);
                        y = margin;
                        DrawTableHeader();
                    }

                    string workerName = payroll.Worker.Name;
                    string role = payroll.Worker.Role ?? "-";
                    string gross = $"₱{(payroll.GrossPay ?? 0):N2}";

                    DrawTableRow(workerName, role, gross);
                    total += payroll.GrossPay ?? 0;
                }

                // --- Footer: Total + Signature ---
                y += 50;

                double totalX = page.Width - margin - gfx.MeasureString($"Total Payroll Cost: ₱{total:N2}", headerFont).Width;
                gfx.DrawString($"Total Cost: ₱{total:N2}", headerFont, XBrushes.Black, new XPoint(totalX, y));


                // Save PDF
                doc.Save(saveDialog.FileName);
                MessageBox.Show("Payroll report PDF generated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
