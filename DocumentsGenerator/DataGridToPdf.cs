using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System;

namespace CATERINGMANAGEMENT.DocumentsGenerator
{
    internal static class DataGridToPdf
    {
        public static void DataGridToPDF(IEnumerable dataSource, string filename, params string[] skipProperties)
        {
            if (dataSource == null || !dataSource.Cast<object>().Any())
            {
                MessageBox.Show("No data available to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf",
                FileName = filename
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            var firstItem = dataSource.Cast<object>().FirstOrDefault();
            if (firstItem == null)
                return;

            var properties = firstItem.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !skipProperties.Contains(p.Name))
                .ToList();

            var document = new PdfDocument();
            var page = CreateLandscapePage(document);
            var gfx = XGraphics.FromPdfPage(page);

            // Fonts
            var titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
            var labelFont = new XFont("Arial", 12, XFontStyleEx.Bold);
            var dateFont = new XFont("Arial", 10, XFontStyleEx.Italic);
            var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
            var rowFont = new XFont("Arial", 10, XFontStyleEx.Regular);

            // Layout constants
            double margin = 40;
            double startX = margin;
            double startY = 140; // leave space for header section
            double rowHeight = 22;
            double pageWidth = page.Width - 2 * margin;
            double pageHeight = page.Height - 2 * margin;
            double colWidth = pageWidth / Math.Max(1, properties.Count);

            int rowIndex = 0;

            // 🟡 Header Color (darker gold)
            var headerBrush = new XSolidBrush(XColor.FromArgb(255, 184, 134, 11));

            string? tempLogo = null;
            try
            {
                // ✅ Draw Logo and Label Section (with date + dynamic filename)
                DrawHeader(gfx, titleFont, labelFont, dateFont, page, filename, out tempLogo);

                // Draw table headers
                void DrawTableHeaders()
                {
                    for (int i = 0; i < properties.Count; i++)
                    {
                        gfx.DrawRectangle(headerBrush,
                            startX + i * colWidth,
                            startY + rowIndex * rowHeight,
                            colWidth,
                            rowHeight);

                        gfx.DrawString(properties[i].Name.ToUpper(),
                            headerFont,
                            XBrushes.White,
                            new XRect(startX + i * colWidth, startY + rowIndex * rowHeight, colWidth, rowHeight),
                            XStringFormats.Center);
                    }
                    rowIndex++;
                }

                void AddNewPage()
                {
                    page = CreateLandscapePage(document);
                    gfx = XGraphics.FromPdfPage(page);
                    DrawHeader(gfx, titleFont, labelFont, dateFont, page, filename, out tempLogo);
                    rowIndex = 0;
                    DrawTableHeaders();
                }

                // Draw Header Row
                DrawTableHeaders();

                // Draw Data Rows
                foreach (var item in dataSource)
                {
                    if ((startY + (rowIndex + 2) * rowHeight) > pageHeight)
                        AddNewPage();

                    for (int i = 0; i < properties.Count; i++)
                    {
                        string value = properties[i].GetValue(item)?.ToString() ?? "";
                        value = TruncateText(value, 50);

                        gfx.DrawRectangle(XBrushes.White,
                            startX + i * colWidth,
                            startY + rowIndex * rowHeight,
                            colWidth,
                            rowHeight);

                        gfx.DrawRectangle(XPens.LightGray,
                            startX + i * colWidth,
                            startY + rowIndex * rowHeight,
                            colWidth,
                            rowHeight);

                        gfx.DrawString(value,
                            rowFont,
                            XBrushes.Black,
                            new XRect(startX + i * colWidth, startY + rowIndex * rowHeight, colWidth, rowHeight),
                            XStringFormats.Center);
                    }

                    rowIndex++;
                }

                // Save and open
                document.Save(saveFileDialog.FileName);
                Process.Start(new ProcessStartInfo
                {
                    FileName = saveFileDialog.FileName,
                    UseShellExecute = true
                });
            }
            finally
            {
                // Cleanup extracted temp image if any
                try
                {
                    if (!string.IsNullOrWhiteSpace(tempLogo) && File.Exists(tempLogo))
                        File.Delete(tempLogo);
                }
                catch { /* ignore cleanup errors */ }
            }
        }

        // ✅ Draw Header with Logo, Title, and Date
        private static void DrawHeader(XGraphics gfx, XFont titleFont, XFont labelFont, XFont dateFont, PdfPage page, string filename, out string? tempExtractedImage)
        {
            tempExtractedImage = null;
            double logoSize = 70;
            double margin = 40;

            string logo = DocumentResourceHelper.GetLogoPath(out tempExtractedImage) ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(logo) && File.Exists(logo))
            {
                try
                {
                    XImage img = XImage.FromFile(logo);
                    double logoHeight = img.PixelHeight * (logoSize / img.PixelWidth);
                    gfx.DrawImage(img, margin, 30, logoSize, logoHeight);
                }
                catch
                {
                    // ignore
                }
            }

            // Draw Label beside logo
            double textStartX = margin + logoSize + 15;
            gfx.DrawString("Catering Management", titleFont, XBrushes.Black,
                new XRect(textStartX, 40, page.Width - textStartX, 30),
                XStringFormats.TopLeft);

            // 🧾 Dynamic filename label (without extension)
            string fileTitle = Path.GetFileNameWithoutExtension(filename).Replace("_", " ");
            gfx.DrawString(fileTitle, labelFont, XBrushes.Gray,
                new XRect(textStartX, 65, page.Width - textStartX, 30),
                XStringFormats.TopLeft);

            // 📅 Date Generated (right side)
            string dateGenerated = "Date Generated: " + DateTime.Now.ToString("MMMM dd, yyyy");
            gfx.DrawString(dateGenerated, dateFont, XBrushes.Gray,
                new XRect(0, 100, page.Width - margin, 20),
                XStringFormats.TopRight);

            // Divider line
            gfx.DrawLine(XPens.DarkGray, margin, 115, page.Width - margin, 115);
        }

        // ✅ Helper for consistent A4 landscape page creation
        private static PdfPage CreateLandscapePage(PdfDocument doc)
        {
            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            return page;
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}
