using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace CATERINGMANAGEMENT.Services
{
    public static class AdminRegistrationService
    {
        private static string ResolveRedirectTo()
        {
            var desktop = Environment.GetEnvironmentVariable("APP_URI_SCHEME");            // e.g., cater://reset-password or https://site/auth-bridge
            var bridge  = Environment.GetEnvironmentVariable("PASSWORD_RESET_BRIDGE_URL"); // e.g., https://site/auth-bridge
            var mobile  = Environment.GetEnvironmentVariable("MOBILE_REDIRECT_URI");       // e.g., myapp:///auth

            if (!string.IsNullOrWhiteSpace(desktop)) return desktop!;
            if (!string.IsNullOrWhiteSpace(bridge))  return bridge!;
            if (!string.IsNullOrWhiteSpace(mobile))  return mobile!;

            // Default to desktop custom scheme used by this app
            return "cater://reset-password";
        }

        public static async Task<bool> RegisterAdminAsync(string email, string password)
        {
            try
            {
                var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                var anonKey = Environment.GetEnvironmentVariable("SUPABASE_API_KEY");
                if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(anonKey))
                {
                    Console.WriteLine("Missing SUPABASE_URL or SUPABASE_API_KEY");
                    return false;
                }

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("apikey", anonKey);
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", anonKey);

                // Always send a redirect_to so links open the app in production
                var redirectTo = ResolveRedirectTo();

                var payload = new
                {
                    email = email.Trim(),
                    password = password,
                    data = new Dictionary<string, object> { { "role", "admin" } },
                    redirect_to = redirectTo
                };

                var endpoint = new Uri(new Uri(supabaseUrl), "/auth/v1/signup");
                var res = await http.PostAsJsonAsync(endpoint, payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                var body = await res.Content.ReadAsStringAsync();

                if (res.IsSuccessStatusCode)
                    return true;

                Console.WriteLine($"SignUp failed: {(int)res.StatusCode} {res.ReasonPhrase} - {body}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Admin sign-up failed: {ex.Message}");
                return false;
            }
        }
    }
}
