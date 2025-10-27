using DotNetEnv;
using System.Windows;
using PdfSharp.Fonts;
using System.IO;
using System;
using System.Diagnostics;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.View;


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

            // Register custom URI protocol for password reset deep links (e.g., oshdy://reset-password)
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                UriProtocolRegistrar.EnsureRegistered("oshdy", exePath);
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool openedFromDeepLink = false;

            if (e.Args != null && e.Args.Length > 0)
            {
                try
                {
                    var arg = e.Args[0];
                    if (Uri.TryCreate(arg, UriKind.Absolute, out var uri) && uri.Scheme == "oshdy")
                    {
                        openedFromDeepLink = DeepLinkHandler.Handle(uri);
                    }
                }
                catch { }
            }

            if (!openedFromDeepLink)
            {
                // Fallback to normal startup
                if (Services.SessionService.IsLoggedIn)
                {
                    var dash = new View.Windows.Dashboard();
                    MainWindow = dash;
                    dash.Show();
                }
                else
                {
                    var login = new LoginView();
                    MainWindow = login;
                    login.Show();
                }
            }
        }
    }
}
