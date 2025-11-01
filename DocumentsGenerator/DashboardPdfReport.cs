using CATERINGMANAGEMENT.Models;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CATERINGMANAGEMENT.DocumentsGenerator
{
    public static class DashboardPdfReport
    {
        private const string CompanyLogoPath = "Assets/images/documentlogo.png";
        public static void Generate(
            DashboardCounters counters,
            IEnumerable<MonthlyReservationSummary> monthlySummaries,
            IEnumerable<Reservation> upcomingReservations,
            Dictionary<string, int> eventTypeDistribution)
        {
            var document = new PdfDocument();
            document.Info.Title = "Dashboard Report";

            var page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);

            // ====== FONTS & COLORS ======
            var fontTitle = new XFont("Arial", 18, XFontStyleEx.Bold);
            var fontSubTitle = new XFont("Arial", 14, XFontStyleEx.Bold);
            var fontText = new XFont("Arial", 11, XFontStyleEx.Regular);
            var fontSmall = new XFont("Arial", 10, XFontStyleEx.Italic);
            var penBorder = new XPen(XColors.Gray, 0.5);
            var penDivider = new XPen(XColors.LightGray, 1);

            // Golden yellow tone
            var headerBackground = XColor.FromArgb(0xD4, 0xAF, 0x37);
            var headerBrush = new XSolidBrush(headerBackground);

            double marginLeft = 50;
            double marginRight = 50;
            double pageWidth = page.Width - marginLeft - marginRight;
            double y = 50;

            void NextPage()
            {
                page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                y = 50;
            }

            // ===== HEADER: LOGO + TITLE (LEFT ALIGNED) =====
            double logoWidth = 70;
            double logoHeight = 0;
            double textStartX = marginLeft;

            string? tempLogo = null;
            try
            {
                var logoPath = DocumentResourceHelper.GetLogoPath(out tempLogo);
                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                {
                    XImage logo = XImage.FromFile(logoPath);
                    logoHeight = logoWidth * (logo.PixelHeight / (double)logo.PixelWidth);
                    gfx.DrawImage(logo, marginLeft, y, logoWidth, logoHeight);
                    textStartX = marginLeft + logoWidth + 15;
                }

                gfx.DrawString("Catering Dashboard Report", fontTitle, XBrushes.Black,
                    new XRect(textStartX, y + 10, page.Width - textStartX - marginRight, 25),
                    XStringFormats.TopLeft);

                y += Math.Max(logoHeight, 40) + 10;

                gfx.DrawString($"Generated on: {DateTime.Now:MMMM dd, yyyy hh:mm tt}",
                    fontSmall, XBrushes.Gray, marginLeft, y);
                y += 25;

                gfx.DrawLine(penDivider, marginLeft, y, page.Width - marginRight, y);
                y += 20;

                // ===== SUMMARY SECTION =====
                gfx.DrawString("Summary Overview", fontSubTitle, XBrushes.Black, marginLeft, y);
                y += 25;

                var summary = new[]
                {
                    ("Total Equipments", counters.TotalEquipments),
                    ("Damaged Equipments", counters.DamagedEquipment),
                    ("Total Workers", counters.TotalWorkers),
                    ("Active Workers", counters.ActiveWorkers),
                    ("Kitchen Items", counters.TotalKitchenItems),
                    ("Low Kitchen Stock", counters.KitchenLowStock),
                    ("Total Reservations", counters.TotalReservations),
                    ("Pending Reservations", counters.PendingReservations)
                };

                double labelX = marginLeft + 10;
                double valueX = marginLeft + 230;

                foreach (var (label, val) in summary)
                {
                    if (y > page.Height - 60) NextPage();
                    gfx.DrawString($"{label}:", fontText, XBrushes.Black, labelX, y);
                    gfx.DrawString(val.ToString(), fontText, XBrushes.DarkSlateGray, valueX, y);
                    y += 18;
                }

                y += 30;
                gfx.DrawLine(penDivider, marginLeft, y, page.Width - marginRight, y);
                y += 20;

                // ===== TABLE DRAW FUNCTION =====
                void DrawTable(string title, string[] headers, IEnumerable<string[]> rows)
                {
                    if (y > page.Height - 100) NextPage();

                    gfx.DrawString(title, fontSubTitle, XBrushes.Black, marginLeft, y);
                    y += 25;

                    int cols = headers.Length;
                    double rowHeight = 22;
                    double[] colWidths = Enumerable.Repeat(pageWidth / cols, cols).ToArray();

                    // Header row (golden yellow background)
                    double x = marginLeft;
                    gfx.DrawRectangle(headerBrush, x, y, pageWidth, rowHeight);
                    for (int i = 0; i < cols; i++)
                    {
                        gfx.DrawRectangle(penBorder, x, y, colWidths[i], rowHeight);
                        gfx.DrawString(headers[i], fontText, XBrushes.Black, // black for better contrast
                            new XRect(x + 5, y + 4, colWidths[i], rowHeight), XStringFormats.TopLeft);
                        x += colWidths[i];
                    }
                    y += rowHeight;

                    // Data rows
                    foreach (var row in rows)
                    {
                        if (y > page.Height - 60)
                        {
                            NextPage();
                            gfx.DrawString(title + " (cont.)", fontSubTitle, XBrushes.Black, marginLeft, y);
                            y += 25;

                            // Redraw header
                            x = marginLeft;
                            gfx.DrawRectangle(headerBrush, x, y, pageWidth, rowHeight);
                            for (int i = 0; i < cols; i++)
                            {
                                gfx.DrawRectangle(penBorder, x, y, colWidths[i], rowHeight);
                                gfx.DrawString(headers[i], fontText, XBrushes.Black,
                                    new XRect(x + 5, y + 4, colWidths[i], rowHeight), XStringFormats.TopLeft);
                                x += colWidths[i];
                            }
                            y += rowHeight;
                        }

                        x = marginLeft;
                        for (int i = 0; i < cols; i++)
                        {
                            gfx.DrawRectangle(penBorder, x, y, colWidths[i], rowHeight);
                            gfx.DrawString(row[i], fontText, XBrushes.Black,
                                new XRect(x + 5, y + 4, colWidths[i], rowHeight), XStringFormats.TopLeft);
                            x += colWidths[i];
                        }

                        y += rowHeight;
                    }

                    y += 30;
                }

                // ===== TABLE SECTIONS =====

                if (monthlySummaries.Any())
                {
                    int year = monthlySummaries.First().ReservationYear;
                    DrawTable(
                        $"Monthly Reservations - {year}",
                        new[] { "Month", "Total" },
                        monthlySummaries.OrderBy(r => r.ReservationMonth)
                            .Select(r => new[]
                            {
                                CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(r.ReservationMonth),
                                r.TotalReservations.ToString()
                            })
                    );
                }

                if (eventTypeDistribution.Any())
                {
                    DrawTable(
                        "Event Type Distribution",
                        new[] { "Event Type", "Count" },
                        eventTypeDistribution
                            .OrderByDescending(kv => kv.Value)
                            .Select(kv => new[] { kv.Key, kv.Value.ToString() })
                    );
                }

                if (upcomingReservations.Any())
                {
                    DrawTable(
                        "Completed Reservations",
                        new[] { "Receipt #", "Venue", "Event Date" },
                        upcomingReservations
                            .OrderBy(r => r.EventDate)
                            .Select(r => new[]
                            {
                                r.ReceiptNumber ?? "-",
                                r.Venue ?? "-",
                                r.EventDate.ToString("MMM dd, yyyy")
                            })
                    );
                }

                // ===== FOOTER =====
                gfx.DrawLine(penDivider, marginLeft, page.Height - 60, page.Width - marginRight, page.Height - 60);
                gfx.DrawString("Prepared by OSHDY Event Catering Services", fontSmall, XBrushes.Gray,
                    new XRect(marginLeft, page.Height - 50, page.Width - marginRight, 20), XStringFormats.BottomLeft);

                // ===== SAVE PDF =====
                var sfd = new SaveFileDialog
                {
                    Title = "Save Dashboard PDF Report",
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"Dashboard_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (sfd.ShowDialog() == true)
                {
                    document.Save(sfd.FileName);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = sfd.FileName,
                        UseShellExecute = true
                    });
                }
            }
            finally
            {
                try { if (!string.IsNullOrWhiteSpace(tempLogo) && File.Exists(tempLogo)) File.Delete(tempLogo); } catch { }
            }
        }
    }
}
