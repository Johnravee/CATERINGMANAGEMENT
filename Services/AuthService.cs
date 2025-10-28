using Supabase;
using Supabase.Gotrue;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CATERINGMANAGEMENT.Helpers;

namespace CATERINGMANAGEMENT.Services
{
    // Detailed result codes for login attempts
    public enum LoginErrorCode
    {
        None,
        UnverifiedEmail,
        NotAdmin,
        InvalidCredentials,
        NetworkError,
        UnknownError
    }

    public sealed class LoginResponse
    {
        public User? User { get; init; }
        public LoginErrorCode Error { get; init; } = LoginErrorCode.None;
        public string? Message { get; init; }

        public static LoginResponse Success(User user) => new() { User = user, Error = LoginErrorCode.None };
        public static LoginResponse Fail(LoginErrorCode code, string? message = null) => new() { Error = code, Message = message };
    }

    public static class AuthService
    {
        public static async Task<LoginResponse> LoginAsync(string email, string password)
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var session = await client.Auth.SignIn(email, password);

                if (session?.User == null)
                {
                    // If no session returned, treat as invalid credentials by default
                    return LoginResponse.Fail(LoginErrorCode.InvalidCredentials, "Invalid email or password.");
                }

                SessionService.SetSession(session);

                var user = session.User;

                // Block login if email is not verified
                if (!IsEmailVerified(user))
                {
                    var msg = "Email not confirmed. Please check your inbox and confirm your email before signing in.";
                    AppLogger.Error(msg);
                    return LoginResponse.Fail(LoginErrorCode.UnverifiedEmail, msg);
                }

                // Only allow users with role=admin
                if (user.UserMetadata != null &&
                    user.UserMetadata.TryGetValue("role", out var role) &&
                    role?.ToString() == "admin")
                {
                    return LoginResponse.Success(user);
                }

                return LoginResponse.Fail(LoginErrorCode.NotAdmin, "Your account does not have admin access.");
            }
            catch (HttpRequestException httpEx)
            {
                var msg = "Network error while attempting to log in. Please check your connection.";
                AppLogger.Error(httpEx, msg);
                return LoginResponse.Fail(LoginErrorCode.NetworkError, msg);
            }
            catch (Exception ex)
            {
                // Inspect common Supabase auth errors
                var lower = ex.Message?.ToLowerInvariant() ?? string.Empty;

                // Specific: email not confirmed (Supabase returns error_code=email_not_confirmed)
                if (lower.Contains("email_not_confirmed") || lower.Contains("email not confirmed"))
                {
                    var msg = "Email not confirmed. Please check your inbox and confirm your email before signing in.";
                    AppLogger.Error(ex, msg);
                    return LoginResponse.Fail(LoginErrorCode.UnverifiedEmail, msg);
                }

                if (lower.Contains("invalid login") || lower.Contains("invalid email or password") || lower.Contains("invalid credentials"))
                {
                    return LoginResponse.Fail(LoginErrorCode.InvalidCredentials, "Invalid email or password.");
                }

                AppLogger.Error(ex, "Login failed due to an unexpected error.");
                return LoginResponse.Fail(LoginErrorCode.UnknownError, "Unexpected error occurred while logging in.");
            }
        }

        private static bool IsEmailVerified(User user)
        {
            // Email Verification Validation
            return user.EmailConfirmedAt != null || user.ConfirmedAt != null;
        }

        private static string ResolveRedirectTo()
        {
            // Preference: explicit desktop scheme -> bridge url -> mobile redirect
            var desktop = Environment.GetEnvironmentVariable("APP_URI_SCHEME"); // e.g., cater://reset-password OR https://<site>/auth-bridge
            var bridge = Environment.GetEnvironmentVariable("PASSWORD_RESET_BRIDGE_URL"); // e.g., https://site/auth-bridge
            var mobile = Environment.GetEnvironmentVariable("MOBILE_REDIRECT_URI"); // e.g., myapp:///auth or exp://...

            if (!string.IsNullOrWhiteSpace(desktop)) return desktop!;
            if (!string.IsNullOrWhiteSpace(bridge)) return bridge!;
            if (!string.IsNullOrWhiteSpace(mobile)) return mobile!;

            // fallback default to desktop custom scheme used in this app
            return "cater://reset-password";
        }

        public static async Task<bool> RequestPasswordResetAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                var client = await SupabaseService.GetClientAsync();
                if (client == null) return false;

                var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                var anonKey = Environment.GetEnvironmentVariable("SUPABASE_API_KEY");
                var redirect = ResolveRedirectTo();

                if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(anonKey))
                {
                    AppLogger.Error("Missing SUPABASE_URL or SUPABASE_API_KEY env vars.");
                    return false;
                }

                // 1) Try standard recover (Supabase emails user)
                try
                {
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Add("apikey", anonKey);
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", anonKey);

                    var payload = new { email = email.Trim(), redirect_to = redirect };
                    var endpoint = new Uri(new Uri(supabaseUrl), "/auth/v1/recover");

                    AppLogger.Info($"Sending password recovery email via REST with redirect_to={redirect}");
                    var res = await http.PostAsJsonAsync(endpoint, payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    var body = await res.Content.ReadAsStringAsync();

                    if (res.IsSuccessStatusCode)
                    {
                        AppLogger.Success("Recover email requested successfully (REST).");
                        return true;
                    }

                    AppLogger.Error($"Recover REST failed: {(int)res.StatusCode} {res.ReasonPhrase} - {body}");
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Recover REST threw an exception");
                }

                // 2) Fallback: use Admin Generate Link and send via our EmailService (requires SERVICE_ROLE_KEY)
                try
                {
                    var serviceKey = Environment.GetEnvironmentVariable("SERVICE_ROLE_KEY");
                    if (string.IsNullOrWhiteSpace(serviceKey))
                    {
                        AppLogger.Error("SERVICE_ROLE_KEY is missing; cannot use admin generate_link fallback.");
                        return false;
                    }

                    using var httpAdmin = new HttpClient();
                    httpAdmin.DefaultRequestHeaders.Add("apikey", serviceKey);
                    httpAdmin.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceKey);

                    var adminPayload = new { type = "recovery", email = email.Trim(), redirect_to = redirect };
                    var adminEndpoint = new Uri(new Uri(supabaseUrl!), "/auth/v1/admin/generate_link");

                    AppLogger.Info("Attempting admin generate_link fallback for recovery.");
                    var adminRes = await httpAdmin.PostAsJsonAsync(adminEndpoint, adminPayload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    var adminBody = await adminRes.Content.ReadAsStringAsync();

                    if (!adminRes.IsSuccessStatusCode)
                    {
                        AppLogger.Error($"Admin generate_link failed: {(int)adminRes.StatusCode} {adminRes.ReasonPhrase} - {adminBody}");
                        return false;
                    }

                    string? actionLink = null;
                    try
                    {
                        using var doc = JsonDocument.Parse(adminBody);
                        if (doc.RootElement.TryGetProperty("action_link", out var linkProp))
                            actionLink = linkProp.GetString();
                    }
                    catch { }

                    if (string.IsNullOrWhiteSpace(actionLink))
                    {
                        AppLogger.Error("Admin generate_link did not return action_link.");
                        return false;
                    }

                    var emailService = new EmailService();
                    var subject = "Reset your password";
                    var bodyHtml = $"<p>Click the link to reset your password:</p><p><a href=\"{actionLink}\">Reset Password</a></p>";
                    var sent = await emailService.SendEmailAsync(email.Trim(), subject, bodyHtml, isHtml: true);
                    if (sent)
                    {
                        AppLogger.Success("Sent recovery email via admin generate_link.");
                        return true;
                    }

                    AppLogger.Error("Failed to send email using EmailService.");
                    return false;
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Admin generate_link fallback threw an exception");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Password reset request failed: " + ex.Message);
                return false;
            }
        }
    }
}
