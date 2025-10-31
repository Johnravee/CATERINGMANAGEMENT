using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Diagnostics;
using System.IO;

namespace CATERINGMANAGEMENT.DocumentsGenerator
{
    /// <summary>
    /// Generates a PDF-only Reservation Equipment Checklist with:
    /// - Reservation summary
    /// - Selected equipment list with quantities
    /// - Menu items for the reservation
    /// - Assigned workers
    /// - Optional customer design image attachment (embedded in PDF)
    /// No DB writes are performed.
    /// </summary>
    public static class ReservationChecklistPdfGenerator
    {
        public static void Generate(
            Reservation reservation,
            IEnumerable<SelectedEquipmentItem> equipments,
            IEnumerable<MenuOption> menuItems,
            IEnumerable<Worker> assignedWorkers,
            string? designImagePath = null,
            string? callTime = null,
            string? suggestedFilename = null)
        {
            using var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);

            // Layout
            double margin = 40;
            double y = margin;
            var h1 = new XFont("Arial", 18, XFontStyleEx.Bold);
            var h2 = new XFont("Arial", 12, XFontStyleEx.Bold);
            var text = new XFont("Arial", 10, XFontStyleEx.Regular);
            var muted = new XSolidBrush(XColors.DimGray);

            // Header
            string logoPath = "Assets/images/oshdylogo.jpg";
            if (File.Exists(logoPath))
            {
                var img = XImage.FromFile(logoPath);
                gfx.DrawImage(img, margin, y, 60, 60);
            }

            // Title
            gfx.DrawString("Reservation Equipment Checklist", h1, XBrushes.Black, new XRect(margin + 70, y, page.Width - margin * 2 - 70, 25), XStringFormats.TopLeft);
            gfx.DrawString($"Generated: {DateTime.Now:MMMM dd, yyyy}", text, muted, new XRect(margin + 70, y + 24, page.Width - margin * 2 - 70, 20), XStringFormats.TopLeft);

            // Call Time: render prominently at top-right if provided
            if (!string.IsNullOrWhiteSpace(callTime))
            {
                var callFont = new XFont("Arial", 26, XFontStyleEx.Bold);
                var callBrush = new XSolidBrush(XColor.FromArgb(255, 33, 37, 41)); // dark
                // Allocate a rect in the header area on the right
                var callRect = new XRect(margin, y, page.Width - margin * 2, 48);
                gfx.DrawString($"@{callTime}", callFont, callBrush, callRect, XStringFormats.TopRight);
            }

            y += 70;

            // Reservation Summary
            gfx.DrawString("Reservation Details", h2, XBrushes.Black, new XRect(margin, y, page.Width - margin * 2, 20), XStringFormats.TopLeft); y += 18;
            DrawKeyValue(gfx, text, margin, ref y, "Receipt #", reservation.ReceiptNumber);
            DrawKeyValue(gfx, text, margin, ref y, "Client", reservation.Profile?.FullName ?? "");
            DrawKeyValue(gfx, text, margin, ref y, "Contact", reservation.Profile?.ContactNumber ?? "");
            DrawKeyValue(gfx, text, margin, ref y, "Event", $"{reservation.EventDate:MMMM dd, yyyy}  {reservation.EventTime:hh\\:mm}");
            DrawKeyValue(gfx, text, margin, ref y, "Venue", reservation.Venue);
            DrawKeyValue(gfx, text, margin, ref y, "Location", reservation.Location);
            DrawKeyValue(gfx, text, margin, ref y, "Package", reservation.Package?.Name ?? "");
            DrawKeyValue(gfx, text, margin, ref y, "Theme", reservation.ThemeMotif?.Name ?? "");
            DrawKeyValue(gfx, text, margin, ref y, "Guests", $"Adults: {reservation.AdultsQty} | Kids: {reservation.KidsQty}");
            y += 10;

            // Equipment list
            gfx.DrawString("Selected Equipments", h2, XBrushes.Black, new XRect(margin, y, page.Width - margin * 2, 20), XStringFormats.TopLeft); y += 18;
            DrawTable(gfx, text, margin, ref y, new[] { "Item", "Qty", "Notes" },
                equipments.Select(e => new[] { e.ItemName, e.Quantity.ToString(), e.Notes ?? string.Empty }),
                page);
            y += 10;

            // Menu list
            gfx.DrawString("Menu Items", h2, XBrushes.Black, new XRect(margin, y, page.Width - margin * 2, 20), XStringFormats.TopLeft); y += 18;
            var menuRows = menuItems?.Select(m => new[] { m.Name ?? "", m.Category ?? "" }) ?? Enumerable.Empty<string[]>();
            DrawTable(gfx, text, margin, ref y, new[] { "Name", "Category" }, menuRows, page);
            y += 10;

            // Workers
            gfx.DrawString("Assigned Workers", h2, XBrushes.Black, new XRect(margin, y, page.Width - margin * 2, 20), XStringFormats.TopLeft); y += 18;
            var workerRows = assignedWorkers?.Select(w => new[] { w.Name ?? "", w.Role ?? "", w.Contact ?? "" }) ?? Enumerable.Empty<string[]>();
            DrawTable(gfx, text, margin, ref y, new[] { "Name", "Role", "Contact" }, workerRows, page);
            y += 12;

            // Optional design image - larger size, better scaling
            if (!string.IsNullOrWhiteSpace(designImagePath) && File.Exists(designImagePath))
            {
                gfx.DrawString("Customer Design Reference", h2, XBrushes.Black, new XRect(margin, y, page.Width - margin * 2, 20), XStringFormats.TopLeft); y += 18;
                var dimg = XImage.FromFile(designImagePath);
                // target larger display area
                double maxW = page.Width - margin * 2;
                double maxH = (page.Height - y - margin) * 0.9; // leave slight space bottom
                double wPt = dimg.PixelWidth * 72.0 / dimg.HorizontalResolution;
                double hPt = dimg.PixelHeight * 72.0 / dimg.VerticalResolution;
                double scale = Math.Min(maxW / wPt, maxH / hPt);
                if (scale <= 0 || double.IsInfinity(scale)) scale = 1;
                double w = wPt * scale;
                double h = hPt * scale;
                gfx.DrawImage(dimg, margin, y, w, h);
                y += h + 8;
            }

            // Save dialog
            string safeName = suggestedFilename ?? $"Checklist_{reservation.ReceiptNumber}_{reservation.EventDate:yyyyMMdd}";
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf",
                FileName = safeName
            };
            if (dlg.ShowDialog() == true)
            {
                doc.Save(dlg.FileName);
                Process.Start(new ProcessStartInfo { FileName = dlg.FileName, UseShellExecute = true });
                AppLogger.Success("Checklist PDF generated");
            }
        }

        private static void DrawKeyValue(XGraphics gfx, XFont font, double x, ref double y, string key, string? val)
        {
            double labelW = 110;
            gfx.DrawString(key + ":", font, XBrushes.Black, new XRect(x, y, labelW, 16), XStringFormats.TopLeft);
            gfx.DrawString(val ?? string.Empty, font, XBrushes.Black, new XRect(x + labelW + 6, y, 420, 16), XStringFormats.TopLeft);
            y += 16;
        }

        private static void DrawTable(XGraphics gfx, XFont font, double x, ref double y, string[] headers, IEnumerable<string[]> rows, PdfPage page)
        {
            double width = gfx.PageSize.Width - 2 * x;
            double rowH = 20; // tighter but readable
            int cols = headers.Length;
            double colW = width / cols;

            // header bg
            var headerBrush = new XSolidBrush(XColor.FromArgb(255, 184, 134, 11));

            // Header
            for (int i = 0; i < cols; i++)
            {
                gfx.DrawRectangle(headerBrush, x + i * colW, y, colW, rowH);
                gfx.DrawRectangle(XPens.LightGray, x + i * colW, y, colW, rowH);
                gfx.DrawString(headers[i], font, XBrushes.White, new XRect(x + i * colW, y + 2, colW, rowH), XStringFormats.TopCenter);
            }
            y += rowH;

            foreach (var r in rows)
            {
                // page break if needed
                if (y + rowH > page.Height - 50)
                {
                    // not implementing multipage for now; shrink a bit
                }

                for (int i = 0; i < cols; i++)
                {
                    string cell = i < r.Length ? r[i] ?? string.Empty : string.Empty;
                    gfx.DrawRectangle(XBrushes.White, x + i * colW, y, colW, rowH);
                    gfx.DrawRectangle(XPens.LightGray, x + i * colW, y, colW, rowH);
                    gfx.DrawString(cell, font, XBrushes.Black, new XRect(x + i * colW + 3, y + 2, colW - 6, rowH), XStringFormats.TopLeft);
                }
                y += rowH;
            }
        }
    }
}
