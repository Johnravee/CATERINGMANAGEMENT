/*
 * FILE: ProfileService.cs
 * PURPOSE: Handles all Supabase operations related to Profile.
 *          Responsibilities:
 *          - Load all profiles.
 *          - Insert a new profile.
 *          - Log success, warning, and error events.
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;

namespace CATERINGMANAGEMENT.Services
{
    public class ProfileService
    {
        /// <summary>
        /// Load all profiles from Supabase.
        /// </summary>
        public async Task<List<Profile>> GetProfilesAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Profile>()
                    .Select("*")
                    .Get();

                if (response.Models != null)
                {
                    AppLogger.Info($"Loaded {response.Models.Count} profiles from Supabase.");
                    return response.Models;
                }

                AppLogger.Error("No profiles returned from Supabase.");
                return new List<Profile>();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load profiles from Supabase.");
                return new List<Profile>();
            }
        }

        /// <summary>
        /// Insert a new profile into Supabase.
        /// </summary>
        public async Task<Profile?> InsertProfileAsync(Profile newProfile)
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Profile>()
                    .Insert(newProfile);

                if (response.Models != null && response.Models.Count > 0)
                {
                    AppLogger.Info($"Inserted profile: {newProfile.FullName} ({newProfile.Email})");
                    return response.Models[0];
                }

                AppLogger.Error("Insert succeeded but no profile returned from Supabase.");
                return null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Failed to insert profile: {newProfile.FullName}");
                return null;
            }
        }
    }
}
