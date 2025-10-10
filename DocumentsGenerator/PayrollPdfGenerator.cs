using CATERINGMANAGEMENT.Models;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                // === Fonts ===
                var titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
                var subTitleFont = new XFont("Arial", 14, XFontStyleEx.Bold);
                var headerFont = new XFont("Arial", 12, XFontStyleEx.Bold);
                var font = new XFont("Arial", 11);

                // === Colors ===
                var headerColor = XColor.FromArgb(204, 163, 0); // golden yellow
                var darkHeaderColor = XColor.FromArgb(153, 122, 0);
                var headerBrush = new XSolidBrush(darkHeaderColor);

                // === Logo and Company Info ===
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", "oshdylogo.jpg");
                double logoWidth = 70;
                double logoHeight = 0;

                if (File.Exists(logoPath))
                {
                    var logo = XImage.FromFile(logoPath);
                    logoHeight = (logo.PixelHeight * logoWidth) / logo.PixelWidth;
                    gfx.DrawImage(logo, margin, y, logoWidth, logoHeight);
                }

                double textX = margin + logoWidth + 15;
                double textWidth = page.Width - textX - margin;

                gfx.DrawString("OSHDY Event Catering Services", titleFont, XBrushes.Black,
                    new XRect(textX, y, textWidth, 30), XStringFormats.TopLeft);
                y += 30;

                gfx.DrawString("Payroll Report", subTitleFont, XBrushes.Black,
                    new XRect(textX, y, textWidth, 25), XStringFormats.TopLeft);

                y += Math.Max(logoHeight, 50) + 10;

                // === Payroll Info ===
                gfx.DrawString($"Reservation: {reservationReceipt}", font, XBrushes.Black, new XPoint(margin, y));
                y += lineHeight;
                gfx.DrawString($"Event Date: {eventDate:MMMM dd, yyyy}", font, XBrushes.Black, new XPoint(margin, y));
                y += 30;

                // === Table Headers ===
                string[] headers = { "Worker Name", "Role", "Salary" };
                double[] colWidths =
                {
                    contentWidth * 0.45,
                    contentWidth * 0.35,
                    contentWidth * 0.20
                };

                void DrawTableHeader()
                {
                    double x = margin;
                    for (int i = 0; i < headers.Length; i++)
                    {
                        gfx.DrawRectangle(XPens.Black, headerBrush, x, y, colWidths[i], lineHeight);
                        gfx.DrawString(headers[i], headerFont, XBrushes.White,
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

                // === Draw Table ===
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

                    string workerName = payroll.Worker?.Name ?? "-";
                    string role = payroll.Worker?.Role ?? "-";
                    string gross = $"₱{(payroll.GrossPay ?? 0):N2}";

                    DrawTableRow(workerName, role, gross);
                    total += payroll.GrossPay ?? 0;
                }

                // === Total ===
                y += 40;
                gfx.DrawString($"Total Payroll Cost: ₱{total:N2}", headerFont, XBrushes.Black,
                    new XRect(margin, y, contentWidth, lineHeight), XStringFormats.TopLeft);

                // === Date Generated ===
                y += 30;
                gfx.DrawString($"Date Generated: {DateTime.Now:MMMM dd, yyyy - h:mm tt}", font, XBrushes.Gray,
                    new XRect(margin, y, contentWidth, lineHeight), XStringFormats.TopLeft);

                // === Save and Open ===
                doc.Save(saveDialog.FileName);
                Process.Start(new ProcessStartInfo
                {
                    FileName = saveDialog.FileName,
                    UseShellExecute = true
                });

                MessageBox.Show("Payroll report PDF generated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
