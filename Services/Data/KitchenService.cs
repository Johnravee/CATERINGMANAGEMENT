/*
 * KitchenService.cs
 * 
 * Service class responsible for managing kitchen-related data operations including
 * CRUD functionalities, retrieval with pagination, summary, and caching support.
 * Inherits caching functionality from BaseCachedService.
 * 
 * Features:
 * - Paginated kitchen items fetching with caching.
 * - Searching kitchen items.
 * - Fetching kitchen summary with caching.
 * - Insert, update, delete operations with cache invalidation.
 * - Getting kitchen item counts with caching.
 * 
 * Author: [Your Name or Team]
 * Created: [Creation Date]
 * Last Modified: [Last Modified Date]
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using System.Diagnostics;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    #region KitchenService Implementation
    public class KitchenService : BaseCachedService
    {
        #region Private Methods

        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        #endregion

        #region Public Methods

        public async Task<List<Kitchen>> GetKitchenPageAsync(int pageNumber, int pageSize)
        {
            string cacheKey = $"Kitchen_Page_{pageNumber}_Size_{pageSize}";

            if (TryGetCache(cacheKey, out List<Kitchen>? cachedItems) && cachedItems != null)
                return cachedItems;

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

                var items = response.Models ?? new List<Kitchen>();

                SetCache(cacheKey, items);

                return items;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error loading kitchen page: {ex.Message}");
                return new List<Kitchen>();
            }
        }

        public async Task<List<Kitchen>> SearchKitchenItemsAsync(string query)
        {
            // No caching here because it is dynamic search
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
            const string cacheKey = "Kitchen_Summary";

            if (TryGetCache(cacheKey, out KitchenSummary? cachedSummary) && cachedSummary != null)
                return cachedSummary;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<KitchenSummary>().Get();

                var summary = response.Models?.FirstOrDefault();

                if (summary != null)
                    SetCache(cacheKey, summary);

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
            if (item == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client
                    .From<Kitchen>()
                    .Insert(item);

                var inserted = response.Models?.FirstOrDefault();

                if (inserted != null)
                {
                    AppLogger.Success($"Inserted kitchen item ID {inserted.Id}");
                    InvalidateAllKitchenCaches();
                }

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
            if (item == null) return null;

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
                {
                    AppLogger.Success($"Updated kitchen item ID {updated.Id}");
                    InvalidateAllKitchenCaches();
                }

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
                InvalidateAllKitchenCaches();
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error deleting kitchen item: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Cache Management

        public void InvalidateAllKitchenCaches()
        {
            InvalidateCache("Kitchen_Summary");
            InvalidateCacheByPrefix("Kitchen_Page_");
        }

        #endregion
    }
    #endregion
}
