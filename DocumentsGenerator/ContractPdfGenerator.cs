using CATERINGMANAGEMENT.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Diagnostics;
using System.IO;

namespace CATERINGMANAGEMENT.DocumentsGenerator
{
    internal class ContractPdfGenerator
    {
        // Updated to support optional custom template image path.
        public static void Generate(Reservation reservation, string savePath, string? templateImagePath = null)
        {
            // Determine image path (custom or default)
            var defaultRelative = Path.Combine("Assets", "images", "contract.png");
            var defaultFromBase = Path.Combine(AppContext.BaseDirectory, "Assets", "images", "contract.png");

            string imagePath = !string.IsNullOrWhiteSpace(templateImagePath) ? templateImagePath! : defaultRelative;
            if (!File.Exists(imagePath))
            {
                // fallback to base directory copy if available
                imagePath = defaultFromBase;
            }

            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Contract template image not found.", imagePath);

            using (var document = new PdfDocument())
            {
                var page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                var gfx = XGraphics.FromPdfPage(page);

                // Draw embedded logo (small) if available
                string? tempLogo = null;
                try
                {
                    string? logoPath = DocumentResourceHelper.GetLogoPath(out tempLogo);
                    if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                    {
                        var logoImg = XImage.FromFile(logoPath);
                        gfx.DrawImage(logoImg, 20, 20, 40, 40);
                    }

                    var bgImage = XImage.FromFile(imagePath);

                    page.Width = bgImage.PixelWidth * 72 / bgImage.HorizontalResolution;
                    page.Height = bgImage.PixelHeight * 72 / bgImage.VerticalResolution;

                    gfx.DrawImage(bgImage, 0, 0, page.Width, page.Height);

                    // Font and brush (bold + uppercase for values except email)
                    var font = new XFont("Arial", 10, XFontStyleEx.Bold);
                    var brush = XBrushes.Black;
                    static string U(string? s) => (s ?? string.Empty).ToUpperInvariant();

                    // Draw values on correct coordinates (uppercase, bold). Email stays as-is (not uppercased).
                    gfx.DrawString(U(DateTime.Now.ToString("MMMM dd, yyyy")), font, brush, new XPoint(364.85, 124.35));
                    gfx.DrawString(U(reservation.Profile?.FullName), font, brush, new XPoint(121.49, 146.85));
                    gfx.DrawString(U(reservation.Celebrant), font, brush, new XPoint(385.77, 146.85));

                    gfx.DrawString(U(reservation.Profile?.Address), font, brush, new XPoint(130.70, 168.17));
                    gfx.DrawString(U(reservation.Profile?.ContactNumber), font, brush, new XPoint(148.86, 191.07));
                    gfx.DrawString(reservation.Profile?.Email ?? string.Empty, font, brush, new XPoint(361.29, 192.25));

                    gfx.DrawString(U(reservation.Package?.Name), font, brush, new XPoint(130, 215));
                    gfx.DrawString(U(reservation.Package?.Name), font, brush, new XPoint(396.04, 213.96));

                    gfx.DrawString(U(reservation.ThemeMotif?.Name), font, brush, new XPoint(153.99, 235.28));
                    gfx.DrawString(U(reservation.Venue), font, brush, new XPoint(118.46, 258.97));
                    gfx.DrawString(U(reservation.Location), font, brush, new XPoint(377.48, 257.58));

                    gfx.DrawString(U(reservation.EventDate.ToString("MMMM dd, yyyy")), font, brush, new XPoint(167, 281.47));
                    gfx.DrawString(U(reservation.EventTime.ToString(@"hh\:mm")), font, brush, new XPoint(431.86, 281.47));

                    gfx.DrawString(U(reservation.AdultsQty.ToString()), font, brush, new XPoint(227.94, 304.76));
                    gfx.DrawString(U(reservation.KidsQty.ToString()), font, brush, new XPoint(414.99, 304.76));

                    // Additional line for optional items: render Grazing if it has value
                    if (!string.IsNullOrWhiteSpace(reservation.Grazing?.Name))
                    {
                        gfx.DrawString(U(reservation.Grazing?.Name), font, brush, new XPoint(134.65, 325.58));
                    }

                    // Save the document
                    document.Save(savePath);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = savePath,
                        UseShellExecute = true
                    });
                }
                finally
                {
                    try { if (!string.IsNullOrWhiteSpace(tempLogo) && File.Exists(tempLogo)) File.Delete(tempLogo); } catch { }
                }
            }
        }
    }
}
