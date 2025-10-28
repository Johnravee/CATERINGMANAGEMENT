using DotNetEnv;
using System.Windows;
using PdfSharp.Fonts;
using System.IO;
using System;
using System.Diagnostics;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.View;
using CATERINGMANAGEMENT.View.Windows;


namespace CATERINGMANAGEMENT
{
    public partial class App : Application
    {
        public App()
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            // Only load .env if it exists; otherwise rely on OS environment variables
            try
            {
                if (File.Exists(envPath))
                {
                    Env.Load(envPath);
                }
            }
            catch { /* swallow env load issues to avoid crashing on startup */ }

            InitializeComponent();

            // Register custom URI protocol for password reset deep links (e.g., cater://reset-password)
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                UriProtocolRegistrar.EnsureRegistered("cater", exePath);
            }
            catch { }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool openedFromDeepLink = false;

            if (e.Args != null && e.Args.Length > 0)
            {
                try
                {
                    var arg = e.Args[0];
                    if (Uri.TryCreate(arg, UriKind.Absolute, out var uri) && uri.Scheme == "cater")
                    {
                        openedFromDeepLink = DeepLinkHandler.Handle(uri);
                    }
                }
                catch { }
            }

            if (!openedFromDeepLink)
            {
                // Show splash and run initialization
                var splash = new SplashScreenWindow();
                MainWindow = splash;
                splash.Show();

                await splash.RunAsync(async () =>
                {
                    // Place any blocking init tasks here if needed
                    await System.Threading.Tasks.Task.Delay(200);
                });

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

                splash.Close();
            }
        }
    }
}
