using System.Configuration;
using System.Data;
using System.Windows;
using PdfSharp.Fonts; // ✅ Required for GlobalFontSettings

namespace CATERINGMANAGEMENT
{
    public partial class App : Application
    {
        public App()
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            // Initialize WPF application
            InitializeComponent();
        }
    }
}
