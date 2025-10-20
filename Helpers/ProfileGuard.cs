using System.Threading.Tasks;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;

namespace CATERINGMANAGEMENT.Helpers
{
    public class ProfileGuard
    {
        /// <summary>
        /// Checks asynchronously if the current user has a profile.
        /// </summary>
        /// <returns>True if the user has a profile; otherwise, false.</returns>
        public static async Task<bool> HasProfileAsync()
        {
            var client = await SupabaseService.GetClientAsync();

            // Assuming you want to check by AuthId or Email of the current user
            var currentUserId = SessionService.CurrentUser?.Id;

            if (currentUserId == null)
            {
                return false; // No user logged in
            }

            var response = await client
                .From<Profile>()
                .Where(p => p.AuthId == currentUserId)  // filter by AuthId
                .Select("*")
                .Get();

            return response.Models != null && response.Models.Count > 0;
        }
    }
}
