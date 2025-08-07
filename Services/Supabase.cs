using Supabase;
using System.Windows;


namespace CATERINGMANAGEMENT.Services
{
    internal static class SupabaseService
    {
        private static Client? _client;

        public static async Task<Client> GetClientAsync()
        {

            if (_client != null)
                return _client;

            var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var key = Environment.GetEnvironmentVariable("SUPABASE_API_KEY");

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true
            };

            _client = new Client(url, key, options);
            await _client.InitializeAsync();

            return _client;
        }

 
    }
}
