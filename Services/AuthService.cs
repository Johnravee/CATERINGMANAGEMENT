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
    }
}
