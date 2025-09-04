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

                var user = session.User;

                // Optional: check if user has "role": "admin" in metadata
                if (user.UserMetadata != null &&
                    user.UserMetadata.TryGetValue("role", out var role) &&
                    role?.ToString() == "admin")
                {
                    return user; // user is admin
                }

                return null; // not admin
            }
            catch (Exception ex)
            {
                Console.WriteLine("Login failed: " + ex.Message);
                return null;
            }
        }
    }
}
