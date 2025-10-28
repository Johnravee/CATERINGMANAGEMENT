using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace CATERINGMANAGEMENT.Services
{
    public static class AdminRegistrationService
    {
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

                // Optional redirect_to for email confirmation (can be omitted)
                string? redirectTo = Environment.GetEnvironmentVariable("APP_URI_SCHEME");

                var payload = new
                {
                    email = email.Trim(),
                    password = password,
                    data = new Dictionary<string, object> { { "role", "admin" } },
                    redirect_to = string.IsNullOrWhiteSpace(redirectTo) ? null : redirectTo
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
