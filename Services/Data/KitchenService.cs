using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    /// <summary>
    /// Service responsible for handling kitchen-related data (cache disabled).
    /// </summary>
    public class KitchenService : BaseCachedService
    {
        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        public async Task<List<Kitchen>> GetKitchenPageAsync(int pageNumber, int pageSize)
        {
            try
            {
                int from = (pageNumber - 1) * pageSize;
                int to = from + pageSize - 1;

                var client = await GetClientAsync();
                var response = await client
                    .From<Kitchen>()
                    .Order(k => k.UpdatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                return response.Models ?? new List<Kitchen>();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error loading kitchen page: {ex.Message}");
                return new List<Kitchen>();
            }
        }

        public async Task<List<Kitchen>> SearchKitchenItemsAsync(string query)
        {
            try
            {
                var client = await GetClientAsync();
                var response = await client
                    .From<Kitchen>()
                    .Filter(k => k.ItemName, Operator.ILike, $"%{query}%")
                    .Get();

                return response.Models ?? new List<Kitchen>();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error searching kitchen items: {ex.Message}");
                return new List<Kitchen>();
            }
        }

        public async Task<KitchenSummary?> GetKitchenSummaryAsync()
        {
            try
            {
                var client = await GetClientAsync();
                var response = await client
                    .From<KitchenSummary>()
                    .Get();

                var summary = response.Models?.FirstOrDefault();
                return summary;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error loading kitchen summary: {ex.Message}");
                return null;
            }
        }

        public async Task<Kitchen?> InsertKitchenItemAsync(Kitchen item)
        {
            try
            {
                var client = await GetClientAsync();
                var response = await client
                    .From<Kitchen>()
                    .Insert(item);

                var inserted = response.Models?.FirstOrDefault();

                if (inserted != null)
                    AppLogger.Success($"Inserted kitchen item ID {inserted.Id}");

                return inserted;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error inserting kitchen item: {ex.Message}");
                return null;
            }
        }

        public async Task<Kitchen?> UpdateKitchenItemAsync(Kitchen item)
        {
            try
            {
                var client = await GetClientAsync();
                item.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<Kitchen>()
                    .Where(k => k.Id == item.Id)
                    .Update(item);

                var updated = response.Models?.FirstOrDefault();

                if (updated != null)
                    AppLogger.Success($"Updated kitchen item ID {updated.Id}");

                return updated;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error updating kitchen item: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteKitchenItemAsync(long id)
        {
            try
            {
                var client = await GetClientAsync();

                await client
                    .From<Kitchen>()
                    .Where(k => k.Id == id)
                    .Delete();

                AppLogger.Success($"Deleted kitchen item ID {id}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error deleting kitchen item: {ex.Message}");
                return false;
            }
        }

        // New method to get counts of kitchen items
        public async Task<(int TotalCount, int LowStockCount, int NormalStockCount)> GetKitchenCountsAsync()
        {
            try
            {
                var client = await GetClientAsync();

                var response = await client
                    .From<KitchenSummary>()   // Query the summary view
                    .Select("*")
                    .Get();

                var summary = response.Models?.FirstOrDefault();

                if (summary != null)
                {
                    return (summary.TotalCount, summary.LowCount, summary.NormalCount);
                }

                return (0, 0, 0);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error getting kitchen counts: {ex.Message}");
                return (0, 0, 0);
            }
        }


        // Cache-clearing disabled
        // private void ClearCache()
        // {
        //     InvalidateCache("Kitchen_Summary");
        //     AppLogger.Info("Kitchen cache invalidated");
        // }
    }
}
