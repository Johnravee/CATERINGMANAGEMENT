using Supabase;
using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.Services
{
    public static class AdminRegistrationService
    {
        public static async Task<bool> RegisterAdminAsync(string email, string password)
        {
            try
            {
                var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                var serviceRoleKey = Environment.GetEnvironmentVariable("SERVICE_ROLE_KEY");

                Console.WriteLine($"Supabase URL: {supabaseUrl}");
                Console.WriteLine($"Service Role Key: {serviceRoleKey}");

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(serviceRoleKey))
                {
                    Console.WriteLine("Missing SUPABASE_URL or SUPABASE_SERVICE_ROLE_KEY");
                    return false;
                }

                // Create a new Supabase.Client instance with the service role key
                var adminClient = new Supabase.Client(supabaseUrl, serviceRoleKey);

                // No need to call InitializeAsync() here

                var adminAuth = adminClient.AdminAuth(serviceRoleKey);

                var adminUserAttrs = new AdminUserAttributes
                {
                    Email = email,
                    Password = password,
                    EmailConfirm = false,
                    UserMetadata = new Dictionary<string, object>
                    {
                        { "role", "admin" }
                    }
                };

                var user = await adminAuth.CreateUser(adminUserAttrs);

                return user != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Admin create user failed: {ex.Message}");
                return false;
            }
        }
    }
}
