using CATERINGMANAGEMENT.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Diagnostics;
using System.IO;

namespace CATERINGMANAGEMENT.DocumentsGenerator
{
    internal class ContractPdfGenerator
    {
        public static void Generate(Reservation reservation, string savePath)
        {
            string imagePath = "Assets/images/contract.png";

            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Contract template image not found.", imagePath);

            using (var document = new PdfDocument())
            {
                var page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                var gfx = XGraphics.FromPdfPage(page);
                var bgImage = XImage.FromFile(imagePath);

                page.Width = bgImage.PixelWidth * 72 / bgImage.HorizontalResolution;
                page.Height = bgImage.PixelHeight * 72 / bgImage.VerticalResolution;

                gfx.DrawImage(bgImage, 0, 0, page.Width, page.Height);

                // Font and brush
                var font = new XFont("Arial", 10);
                var brush = XBrushes.Black;

                // Draw values on correct coordinates
                gfx.DrawString(DateTime.Now.ToString("MMMM dd, yyyy"), font, brush, new XPoint(364.85, 124.35)); 
                gfx.DrawString(reservation.Profile?.FullName ?? string.Empty, font, brush, new XPoint(121.49, 146.85));
                gfx.DrawString(reservation.Celebrant, font, brush, new XPoint(385.77, 146.85));

                gfx.DrawString(reservation.Profile?.Address ?? string.Empty, font, brush, new XPoint(130.70, 168.17));
                gfx.DrawString(reservation.Profile?.ContactNumber ?? string.Empty, font, brush, new XPoint(148.86, 191.07));
                gfx.DrawString(reservation.Profile?.Email ?? string.Empty, font, brush, new XPoint(361.29, 192.25));

                gfx.DrawString(reservation.Package?.Name ?? string.Empty, font, brush, new XPoint(130, 215));
                gfx.DrawString(reservation.Package?.Name ?? string.Empty, font, brush, new XPoint(396.04, 213.96)); 

                gfx.DrawString(reservation.ThemeMotif?.Name ?? string.Empty, font, brush, new XPoint(153.99, 235.28));
                gfx.DrawString(reservation.Venue, font, brush, new XPoint(118.46, 258.97));
                gfx.DrawString(reservation.Location, font, brush, new XPoint(377.48, 257.58));

                gfx.DrawString(reservation.EventDate.ToString("MMMM dd, yyyy"), font, brush, new XPoint(167, 281.47));
                gfx.DrawString(reservation.EventTime.ToString(@"hh\:mm"), font, brush, new XPoint(431.86, 281.47));

                gfx.DrawString(reservation.AdultsQty.ToString(), font, brush, new XPoint(227.94, 304.76));
                gfx.DrawString(reservation.KidsQty.ToString(), font, brush, new XPoint(414.99, 304.76));

                gfx.DrawString("None", font, brush, new XPoint(134.65, 325.58)); // Additional field

                // Save the document
                document.Save(savePath);
                Process.Start(new ProcessStartInfo
                {
                    FileName = savePath,
                    UseShellExecute = true
                });
            }
        }
    }
}
