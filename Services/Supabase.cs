using Supabase;
using System;
using System.Threading.Tasks;
using CATERINGMANAGEMENT.Models;

namespace CATERINGMANAGEMENT.Services
{
    internal static class SupabaseService
    {
        private static Client? _client;

        public static async Task<Client> GetClientAsync()
        {
            if (_client != null)
                return _client;

            var url = "https://ezzpttxajkfwdgxsslsb.supabase.co"; // ← Replace with your real Supabase URL
            var key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImV6enB0dHhhamtmd2RneHNzbHNiIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDI2OTUwNTUsImV4cCI6MjA1ODI3MTA1NX0.kozAOx5JUe03pMAjfXY5KYhYmVXbh4LKyTeYMsONUYs";       // ← Replace with your Supabase Key

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = false
            };

            _client = new Client(url, key, options);
            await _client.InitializeAsync();

            return _client;
        }

 
    }
}
