using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Collections;
using System.Reflection;
using System.Windows;

namespace CATERINGMANAGEMENT.Services.DocumentsGenerator
{
    internal static class DataGridToPdf
    {
        [Obsolete]
        public static void DataGridToPDF(IEnumerable dataSource, params string[] skipProperties)
        {
            if (dataSource == null)
                return;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf",
                FileName = "Equipments.pdf"
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
            page.Orientation = PdfSharp.PageOrientation.Portrait;
            var gfx = XGraphics.FromPdfPage(page);

            // Fonts
            var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
            var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
            var rowFont = new XFont("Arial", 10, XFontStyleEx.Regular);


            // Draw Title
            gfx.DrawString("Equipment Inventory", titleFont, XBrushes.Black,
                new XRect(0, 20, page.Width, 30), XStringFormats.TopCenter);

            // Table starting position
            double startX = 40;
            double startY = 60;
            double rowHeight = 20;
            double colWidth = (page.Width - 80) / properties.Count; // auto width

            // Draw Header Row
            for (int i = 0; i < properties.Count; i++)
            {
                gfx.DrawRectangle(XBrushes.LightGray, startX + i * colWidth, startY, colWidth, rowHeight);
                gfx.DrawString(properties[i].Name.ToUpper(), headerFont, XBrushes.Black,
                    new XRect(startX + i * colWidth, startY, colWidth, rowHeight), XStringFormats.Center);
            }

            // Draw Data Rows
            int rowIndex = 1;
            foreach (var item in dataSource)
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    string value = properties[i].GetValue(item)?.ToString() ?? "";
                    gfx.DrawRectangle(XBrushes.White, startX + i * colWidth, startY + rowIndex * rowHeight, colWidth, rowHeight);
                    gfx.DrawString(value, rowFont, XBrushes.Black,
                        new XRect(startX + i * colWidth, startY + rowIndex * rowHeight, colWidth, rowHeight), XStringFormats.Center);
                }
                rowIndex++;

                // If table exceeds page height → add new page
                if (startY + (rowIndex + 1) * rowHeight > page.Height - 40)
                {
                    page = document.AddPage();
                    page.Orientation = PdfSharp.PageOrientation.Portrait;
                    gfx = XGraphics.FromPdfPage(page);
                    rowIndex = 0; // reset for new page
                }
            }

            // Save PDF
            document.Save(saveFileDialog.FileName);
            MessageBox.Show("PDF file has been saved successfully!", "Export Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
