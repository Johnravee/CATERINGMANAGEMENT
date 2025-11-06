using Supabase;
using Supabase.Gotrue;
using System;
using System.Net;
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
        private static string AppendExpiry(string baseRedirect, string purpose, int minutes)
        {
            try
            {
                var issued = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var exp = DateTimeOffset.UtcNow.AddMinutes(minutes).ToUnixTimeSeconds();
                var qs = $"purpose={Uri.EscapeDataString(purpose)}&issued={issued}&exp={exp}";
                var separator = baseRedirect.Contains('?') ? "&" : "?";
                return baseRedirect + separator + qs;
            }
            catch { return baseRedirect; }
        }

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
                    // Proactively resend a fresh confirmation link (valid for next 60 minutes)
                    await ResendEmailConfirmationAsync(email);
                    var msg = "Email not confirmed. We've sent you a new confirmation link valid for the next 60 minutes. Please check your inbox (and spam).";
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
                    // Proactively resend a fresh confirmation link (valid for next 60 minutes)
                    try { await ResendEmailConfirmationAsync(email); } catch { /* best-effort */ }
                    var msg = "Email not confirmed. We've sent you a new confirmation link valid for the next 60 minutes. Please check your inbox (and spam).";
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

        private static string ResolveVerifyRedirectTo()
        {
            // Preference: explicit desktop scheme -> bridge url -> mobile redirect (email confirmation variant)
            var desktop = Environment.GetEnvironmentVariable("APP_URI_SCHEME"); // can also serve for verify
            var bridge = Environment.GetEnvironmentVariable("EMAIL_CONFIRM_BRIDGE_URL"); // optional dedicated bridge for verify
            var mobile = Environment.GetEnvironmentVariable("MOBILE_REDIRECT_URI");

            if (!string.IsNullOrWhiteSpace(desktop)) return desktop!;
            if (!string.IsNullOrWhiteSpace(bridge)) return bridge!;
            if (!string.IsNullOrWhiteSpace(mobile)) return mobile!;

            // fallback default to desktop custom scheme used in this app
            return "cater://verify-email";
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
                var redirect = AppendExpiry(ResolveRedirectTo(), "recovery", minutes: 60);

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

                    // Treat rate limit as success to avoid user confusion on resend
                    if (res.StatusCode == HttpStatusCode.TooManyRequests ||
                        body.Contains("over_email_send_rate_limit", StringComparison.OrdinalIgnoreCase) ||
                        body.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                    {
                        AppLogger.Info("Recover request was rate-limited; treating as success (email likely already sent recently).");
                        return true;
                    }

                    AppLogger.Error($"Recover REST failed: {(int)res.StatusCode} {res.ReasonPhrase} - {body}", showToUser: false);
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
                        AppLogger.Error("SERVICE_ROLE_KEY is missing; cannot use admin generate_link fallback.", showToUser: false);
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
                        AppLogger.Error($"Admin generate_link failed: {(int)adminRes.StatusCode} {adminRes.ReasonPhrase} - {adminBody}", showToUser: false);
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
                        AppLogger.Error("Admin generate_link did not return action_link.", showToUser: false);
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

                    AppLogger.Error("Failed to send email using EmailService.", showToUser: false);
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

        public static async Task<bool> ResendEmailConfirmationAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                var anonKey = Environment.GetEnvironmentVariable("SUPABASE_API_KEY");
                var redirect = AppendExpiry(ResolveVerifyRedirectTo(), "signup_confirm", minutes: 60);

                if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(anonKey))
                {
                    AppLogger.Error("Missing SUPABASE_URL or SUPABASE_API_KEY env vars for resend confirmation.");
                    return false;
                }

                // 1) Try standard RESEND (Supabase emails user)
                try
                {
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Add("apikey", anonKey);
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", anonKey);

                    var payload = new { type = "signup", email = email.Trim(), redirect_to = redirect };
                    var endpoint = new Uri(new Uri(supabaseUrl), "/auth/v1/resend");

                    AppLogger.Info($"Requesting email confirmation resend via REST with redirect_to={redirect}");
                    var res = await http.PostAsJsonAsync(endpoint, payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    var body = await res.Content.ReadAsStringAsync();

                    if (res.IsSuccessStatusCode)
                    {
                        AppLogger.Success("Confirmation email re-sent successfully (REST).");
                        return true;
                    }

                    // Treat rate limit as success (email recently sent)
                    if (res.StatusCode == HttpStatusCode.TooManyRequests ||
                        body.Contains("over_email_send_rate_limit", StringComparison.OrdinalIgnoreCase) ||
                        body.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                    {
                        AppLogger.Info("Resend request was rate-limited; treating as success.");
                        return true;
                    }

                    AppLogger.Error($"Resend REST failed: {(int)res.StatusCode} {res.ReasonPhrase} - {body}", showToUser: false);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Resend REST threw an exception");
                }

                // 2) Fallback: use Admin Generate Link and send via our EmailService (requires SERVICE_ROLE_KEY)
                try
                {
                    var serviceKey = Environment.GetEnvironmentVariable("SERVICE_ROLE_KEY");
                    if (string.IsNullOrWhiteSpace(serviceKey))
                    {
                        AppLogger.Error("SERVICE_ROLE_KEY is missing; cannot use admin generate_link fallback for confirmation.", showToUser: false);
                        return false;
                    }

                    using var httpAdmin = new HttpClient();
                    httpAdmin.DefaultRequestHeaders.Add("apikey", serviceKey);
                    httpAdmin.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceKey);

                    var adminPayload = new { type = "signup", email = email.Trim(), redirect_to = redirect };
                    var adminEndpoint = new Uri(new Uri(supabaseUrl!), "/auth/v1/admin/generate_link");

                    AppLogger.Info("Attempting admin generate_link fallback for confirmation.");
                    var adminRes = await httpAdmin.PostAsJsonAsync(adminEndpoint, adminPayload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    var adminBody = await adminRes.Content.ReadAsStringAsync();

                    if (!adminRes.IsSuccessStatusCode)
                    {
                        AppLogger.Error($"Admin generate_link (confirmation) failed: {(int)adminRes.StatusCode} {adminRes.ReasonPhrase} - {adminBody}", showToUser: false);
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
                        AppLogger.Error("Admin generate_link (confirmation) did not return action_link.", showToUser: false);
                        return false;
                    }

                    var emailService = new EmailService();
                    var subject = "Confirm your email";
                    var bodyHtml = $"<p>Click the link to confirm your email (valid for 60 minutes):</p><p><a href=\"{actionLink}\">Confirm Email</a></p>";
                    var sent = await emailService.SendEmailAsync(email.Trim(), subject, bodyHtml, isHtml: true);
                    if (sent)
                    {
                        AppLogger.Success("Sent confirmation email via admin generate_link.");
                        return true;
                    }

                    AppLogger.Error("Failed to send confirmation email using EmailService.", showToUser: false);
                    return false;
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Admin generate_link fallback (confirmation) threw an exception");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Resend confirmation request failed: " + ex.Message);
                return false;
            }
        }
    }
}
