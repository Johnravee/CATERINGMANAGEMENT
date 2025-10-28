using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Helpers;
using System.Diagnostics;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class SplashScreenViewModel : INotifyPropertyChanged
    {
        private string _appName = Environment.GetEnvironmentVariable("APP_NAME")?.Trim()
            ?? Environment.GetEnvironmentVariable("BRAND_NAME")?.Trim()
            ?? "CaterMate Management";
        public string AppName { get => _appName; set { _appName = value; OnPropertyChanged(); } }

        private string _statusMessage = "Starting...";
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        private int _progress;
        public int Progress { get => _progress; set { _progress = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Run real startup work and allow an optional extra initializer
        public async Task RunAsync(Func<Task> initialize)
        {
            try
            {
                // 1) Load/validate configuration
                Progress = 10; StatusMessage = "Loading configuration...";
                await Task.Yield();
                var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_API_KEY");
                if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseKey))
                {
                    StatusMessage = "Missing Supabase configuration (SUPABASE_URL/API_KEY).";
                }

                // 2) Initialize services (Supabase client)
                Progress = 35; StatusMessage = "Initializing services...";
                var client = await SupabaseService.GetClientAsync();

                // 3) Connectivity check / warmup
                Progress = 60; StatusMessage = "Checking connectivity...";
                try
                {
                    if (client != null)
                    {
                        // Lightweight count query as a ping (any table will do). Using feedbacks as example
                        _ = await client.From<CATERINGMANAGEMENT.Models.Feedback>().Count(CountType.Exact);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Splash] Warmup check failed: {ex.Message}");
                }

                // 4) Register deep link protocol (idempotent)
                Progress = 80; StatusMessage = "Registering deep link...";
                try
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                    UriProtocolRegistrar.EnsureRegistered("cater", exePath);
                }
                catch { }

                // 5) Allow caller to run extra initialization
                if (initialize != null)
                {
                    StatusMessage = "Finalizing...";
                    await initialize();
                }

                Progress = 100; StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initialization failed: {ex.Message}";
            }
        }
    }
}
