using Supabase.Gotrue;



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

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(serviceRoleKey))
                {
                    Console.WriteLine("Missing SUPABASE_URL or SUPABASE_SERVICE_ROLE_KEY");
                    return false;
                }


                var adminClient = new Supabase.Client(supabaseUrl, serviceRoleKey);


                var adminAuth = adminClient.AdminAuth(serviceRoleKey);

                var adminUserAttrs = new AdminUserAttributes
                {
                    Email = email,
                    Password = password,
                    EmailConfirm = true,
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
