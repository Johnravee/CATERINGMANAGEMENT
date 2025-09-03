using PdfSharp.Pdf;
using PdfSharp.Drawing;
using CATERINGMANAGEMENT.Models;

using Microsoft.Win32; 

namespace CATERINGMANAGEMENT.DocumentsGenerator
{
    internal class EquipmentPdfGenerator
    {
        [Obsolete]
        public static void Generate(List<Equipment> equipments)
        {
            if (equipments == null || equipments.Count == 0)
                throw new ArgumentException("No equipment provided to export.");

            // Open SaveFileDialog
            var saveDialog = new SaveFileDialog
            {
                Title = "Save Equipment PDF",
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Equipment_{DateTime.Now:yyyyMMddHHmmss}.pdf"
            };

            if (saveDialog.ShowDialog() != true)
                return; // user cancelled

            using (var document = new PdfDocument())
            {
                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);

                // Title
                var titleFont = new XFont("Arial", 16);
                gfx.DrawString("Selected Equipment Report", titleFont, XBrushes.Black,
                    new XRect(0, 20, page.Width, 30), XStringFormats.TopCenter);

                var font = new XFont("Arial", 12);
                double yPoint = 70;

                // Table headers
                gfx.DrawString("Item Name", font, XBrushes.Black, new XPoint(40, yPoint));
                gfx.DrawString("Quantity", font, XBrushes.Black, new XPoint(250, yPoint));
                gfx.DrawString("Condition", font, XBrushes.Black, new XPoint(400, yPoint));
                yPoint += 25;

                // Rows
                foreach (var eq in equipments)
                {
                    gfx.DrawString(eq.ItemName ?? "-", font, XBrushes.Black, new XPoint(40, yPoint));
                    gfx.DrawString(eq.Quantity?.ToString() ?? "-", font, XBrushes.Black, new XPoint(250, yPoint));
                    gfx.DrawString(eq.Condition ?? "-", font, XBrushes.Black, new XPoint(400, yPoint));
                    yPoint += 20;

                    if (yPoint > page.Height - 50)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        yPoint = 40;
                    }
                }

                // Save document to chosen path
                document.Save(saveDialog.FileName);
            }
        }
    }
}
