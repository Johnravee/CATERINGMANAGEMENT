using CATERINGMANAGEMENT.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class FeedbackService
    {
        private const int PageSize = 10;

        public async Task<(ObservableCollection<Feedback> Items, int TotalCount, int TotalPages)> GetFeedbackPageAsync(int page)
        {
            var client = await SupabaseService.GetClientAsync();

            int from = (page - 1) * PageSize;
            int to = from + PageSize - 1;

            var response = await client
                .From<Feedback>()
                .Select("*, profiles(*)")
                .Range(from, to)
                .Order(x => x.CreatedAt, Ordering.Descending)
                .Get();

            var items = new ObservableCollection<Feedback>(response.Models ?? []);

            var countResult = await client
                .From<Feedback>()
                .Count(CountType.Exact);

            int totalCount = countResult;
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));

            return (items, totalCount, totalPages);
        }

        public async Task<ObservableCollection<Feedback>> SearchFeedbacksAsync(string query)
        {
            var client = await SupabaseService.GetClientAsync();

            var response = await client
                .From<Feedback>()
                .Select("*")
                .Filter(x => x.Name, Operator.ILike, $"%{query}%")
                .Order(x => x.CreatedAt, Ordering.Descending)
                .Get();

            return new ObservableCollection<Feedback>(response.Models ?? []);
        }

        public static async Task<bool> DeleteFeedbackAsync(Feedback item)
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.From<Feedback>().Where(x => x.Id == item.Id).Delete(); // returns void Task
                return true;
            }
            catch
            {
                return false; 
            }
        }

        public async Task<Profile?> GetProfileByIdAsync(long profileId)
        {
            var client = await SupabaseService.GetClientAsync();
            var response = await client
                .From<Profile>()
                .Select("id, name")
                .Where(x => x.Id == profileId)
                .Get();

            var profile = response.Models?.FirstOrDefault();
            if (profile == null)
                System.Diagnostics.Debug.WriteLine($"No profile found for id {profileId}!");

            return profile;
        }

    }
}
