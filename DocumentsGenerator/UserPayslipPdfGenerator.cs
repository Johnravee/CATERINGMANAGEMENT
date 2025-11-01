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
    internal class UserPayslipPdfGenerator
    {
        public static void Generate(List<Payroll> payrolls, string workerName, DateTime startDate, DateTime endDate)
        {
            if (payrolls == null || payrolls.Count == 0)
                throw new ArgumentException("No payroll data provided.");

            var saveDialog = new SaveFileDialog
            {
                Title = "Save Payslip Contract",
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Payslip_{workerName}_{startDate:yyyyMMdd}.pdf"
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
                var darkGold = XColor.FromArgb(153, 122, 0);
                var headerBrush = new XSolidBrush(darkGold);

                // === Logo and Company Info ===
                double logoWidth = 70;
                double logoHeight = 0;

                string? tempLogo = null;
                try
                {
                    var logoPath = DocumentResourceHelper.GetLogoPath(out tempLogo);
                    if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
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

                    gfx.DrawString("Employee Payslip", subTitleFont, XBrushes.Black,
                        new XRect(textX, y, textWidth, 25), XStringFormats.TopLeft);

                    y += Math.Max(logoHeight, 50) + 10;

                    // === Employee Details ===
                    gfx.DrawString($"Employee Name: {workerName}", font, XBrushes.Black, new XPoint(margin, y)); y += lineHeight;
                    gfx.DrawString($"Date Issued: {DateTime.Now:MMMM dd, yyyy}", font, XBrushes.Black, new XPoint(margin, y)); y += lineHeight;
                    gfx.DrawString($"Cutoff: {startDate:MMM d} - {endDate:MMM d, yyyy}", font, XBrushes.Black, new XPoint(margin, y)); y += 30;

                    // === Column Setup ===
                    string[] headers = { "Receipt No.", "Event Date", "Gross Pay", "Paid Date", "Status" };
                    double[] colWidths =
                    {
                        contentWidth * 0.30,
                        contentWidth * 0.15,
                        contentWidth * 0.15,
                        contentWidth * 0.20,
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

                    // === Table Content ===
                    DrawTableHeader();
                    decimal total = 0;

                    foreach (var p in payrolls)
                    {
                        if (y + lineHeight > page.Height - margin)
                        {
                            page = doc.AddPage();
                            page.Orientation = PdfSharp.PageOrientation.Landscape;
                            gfx = XGraphics.FromPdfPage(page);
                            y = margin;
                            DrawTableHeader();
                        }

                        string receipt = p.Reservation?.ReceiptNumber ?? "-";
                        string eventDate = p.Reservation?.EventDate.ToString("MMM dd, yyyy") ?? "-";
                        string gross = $"₱{(p.GrossPay ?? 0):N2}";
                        string paidDate = p.PaidDate?.ToString("MMM dd, yyyy") ?? "-";
                        string status = p.PaidDate != null ? "Paid" : "Unpaid";

                        DrawTableRow(receipt, eventDate, gross, paidDate, status);
                        total += p.GrossPay ?? 0;
                    }

                    // === Total ===
                    y += 40;
                    gfx.DrawString($"Total Payroll Amount: ₱{total:N2}", headerFont, XBrushes.Black,
                        new XRect(margin, y, contentWidth, lineHeight), XStringFormats.TopLeft);

                    // === Date Generated ===
                    y += 25;
                    gfx.DrawString($"Date Generated: {DateTime.Now:MMMM dd, yyyy - h:mm tt}", font, XBrushes.Gray,
                        new XRect(margin, y, contentWidth, lineHeight), XStringFormats.TopLeft);

                    // === Save and Open ===
                    doc.Save(saveDialog.FileName);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
                finally
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(tempLogo) && File.Exists(tempLogo))
                            File.Delete(tempLogo);
                    }
                    catch { /* ignore cleanup errors */ }
                }
            }
        }
    }
}
