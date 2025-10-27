using Supabase;
using Supabase.Gotrue;
using System;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.Services
{
    public static class AuthService
    {
        public static async Task<User?> LoginAsync(string email, string password)
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var session = await client.Auth.SignIn(email, password);

                if (session?.User == null)
                    return null;

                SessionService.SetSession(session);

                var user = session.User;

                
                if (user.UserMetadata != null &&
                    user.UserMetadata.TryGetValue("role", out var role) &&
                    role?.ToString() == "admin")
                {
                    return user; 
                }

                return null; // not admin
            }
            catch (Exception ex)
            {
                Console.WriteLine("Login failed: " + ex.Message);
                return null;
            }
        }

        public static async Task<bool> RequestPasswordResetAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                var client = await SupabaseService.GetClientAsync();
                if (client == null) return false;

                // Supabase .NET SDK Gotrue: ResetPasswordForEmail sends the email
                await client.Auth.ResetPasswordForEmail(email.Trim());

                // If no exception, consider it successful (Supabase does not disclose whether email exists)
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Password reset request failed: " + ex.Message);
                return false;
            }
        }
    }
}
