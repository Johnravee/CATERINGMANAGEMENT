using Supabase;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace CATERINGMANAGEMENT.Services
{
    internal static class SupabaseService
    {
        private static Client? _client;

        public static async Task<Client?> GetClientAsync()
        {
            if (_client != null)
                return _client;

            try
            {
                var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
                var key = Environment.GetEnvironmentVariable("SUPABASE_API_KEY");

                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
                {
                    Log("Supabase URL or API Key is missing.");
                    MessageBox.Show("Supabase credentials are not set in environment variables.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                var options = new SupabaseOptions
                {
                    AutoConnectRealtime = true
                };

                _client = new Client(url, key, options);

                Log("Initializing Supabase client...");
                await _client.InitializeAsync();
                Log("Supabase client initialized successfully.");
                return _client;
            }
            catch (Exception ex)
            {
                Log($"Error initializing Supabase: {ex.Message}");
                MessageBox.Show($"Failed to connect to Supabase:\n{ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private static void Log(string message)
        {
            Debug.WriteLine($"[Supabase] {message}");
            // Optional: log to file or database here
        }
    }
}
