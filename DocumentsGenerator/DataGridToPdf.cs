using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Collections;
using System.Reflection;
using System.Windows;

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

            // Create PDF document
            var document = new PdfDocument();
            var page = document.AddPage();
            page.Orientation = PdfSharp.PageOrientation.Landscape; // Allow wider tables
            var gfx = XGraphics.FromPdfPage(page);

            // Fonts
            var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
            var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
            var rowFont = new XFont("Arial", 10, XFontStyleEx.Regular);

            // Layout constants
            double margin = 40;
            double startX = margin;
            double startY = margin;
            double rowHeight = 20;
            double pageWidth = page.Width - 2 * margin;
            double pageHeight = page.Height - 2 * margin;
            double colWidth = pageWidth / properties.Count;

            int rowIndex = 0;

            void DrawTableHeaders()
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    gfx.DrawRectangle(XBrushes.LightGray, startX + i * colWidth, startY + rowIndex * rowHeight, colWidth, rowHeight);
                    gfx.DrawString(properties[i].Name.ToUpper(), headerFont, XBrushes.Black,
                        new XRect(startX + i * colWidth, startY + rowIndex * rowHeight, colWidth, rowHeight), XStringFormats.Center);
                }
                rowIndex++;
            }

            void AddNewPage()
            {
                page = document.AddPage();
                page.Orientation = PdfSharp.PageOrientation.Landscape;
                gfx = XGraphics.FromPdfPage(page);
                rowIndex = 0;
                DrawTableHeaders();
            }

            // Draw Title
            gfx.DrawString(filename.Replace(".pdf", "").ToUpper(), titleFont, XBrushes.Black,
                new XRect(0, 20, page.Width, 30), XStringFormats.TopCenter);

            rowIndex += 2; // Offset title

            // Draw Header
            DrawTableHeaders();

            // Draw Rows
            foreach (var item in dataSource)
            {
                // Check for page overflow
                if ((startY + (rowIndex + 1) * rowHeight) > pageHeight)
                    AddNewPage();

                for (int i = 0; i < properties.Count; i++)
                {
                    string value = properties[i].GetValue(item)?.ToString() ?? "";
                    value = TruncateText(value, 50); // limit very long strings

                    gfx.DrawRectangle(XBrushes.White, startX + i * colWidth, startY + rowIndex * rowHeight, colWidth, rowHeight);
                    gfx.DrawString(value, rowFont, XBrushes.Black,
                        new XRect(startX + i * colWidth, startY + rowIndex * rowHeight, colWidth, rowHeight), XStringFormats.Center);
                }

                rowIndex++;
            }

            // Save PDF
            document.Save(saveFileDialog.FileName);
            MessageBox.Show("PDF file has been saved successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}
