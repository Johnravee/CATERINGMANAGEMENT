using DotNetEnv;
using System.Windows;
using PdfSharp.Fonts;
using System.IO;


namespace CATERINGMANAGEMENT
{
    public partial class App : Application
    {
        public App()
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            Env.Load(envPath);
            InitializeComponent();
        }
    }
}
