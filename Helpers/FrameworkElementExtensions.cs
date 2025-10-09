using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public static class FrameworkElementExtensions
{
    public static RenderTargetBitmap RenderToBitmap(this FrameworkElement element, double dpiX = 96, double dpiY = 96)
    {
        if (element == null)
            return null;

        // Force layout update if needed
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        element.Arrange(new Rect(element.DesiredSize));
        element.UpdateLayout();

        int width = (int)element.ActualWidth;
        int height = (int)element.ActualHeight;

        if (width == 0 || height == 0)
            return null;

        var rtb = new RenderTargetBitmap(width, height, dpiX, dpiY, PixelFormats.Pbgra32);
        rtb.Render(element);

        return rtb;
    }
}
