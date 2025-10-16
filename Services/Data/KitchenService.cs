using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    /// <summary>
    /// Service responsible for handling kitchen-related data,
    /// including CRUD operations and caching.
    /// </summary>
    public class KitchenService : BaseCachedService
    {
        private Supabase.Client? _client;

        /// <summary>
        /// Default constructor that initializes base caching.
        /// </summary>
        public KitchenService() : base()
        {
        }

        /// <summary>
        /// Lazily retrieves the Supabase client.
        /// </summary>
        /// <returns>Supabase client instance</returns>
        private async Task<Supabase.Client> GetClientAsync()
        {
            if (_client == null)
                _client = await SupabaseService.GetClientAsync();

            return _client;
        }

        /// <summary>
        /// Gets a paginated list of kitchen items, using cache when available.
        /// </summary>
        /// <param name="pageNumber">Page number starting from 1</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>List of Kitchen items</returns>
        public async Task<List<Kitchen>> GetKitchenPageAsync(int pageNumber, int pageSize)
        {
            string cacheKey = $"Kitchen_Page_{pageNumber}_Size_{pageSize}";

            if (TryGetCache(cacheKey, out List<Kitchen>? cachedList) && cachedList != null)
            {
                AppLogger.Info($"Loaded kitchen page {pageNumber} from cache");
                return cachedList;
            }

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

                var list = response.Models ?? new List<Kitchen>();

                SetCache(cacheKey, list);
                AppLogger.Info($"Loaded {list.Count} kitchen items from Supabase (page {pageNumber})");

                return list;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error loading kitchen page: {ex.Message}");
                return new List<Kitchen>();
            }
        }

        /// <summary>
        /// Searches kitchen items by name using case-insensitive LIKE. No cache.
        /// </summary>
        /// <param name="query">Text to search for</param>
        /// <returns>List of matched kitchen items</returns>
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

        /// <summary>
        /// Retrieves kitchen summary (e.g., total, low stock), cached for performance.
        /// </summary>
        /// <returns>KitchenSummary object</returns>
        public async Task<KitchenSummary?> GetKitchenSummaryAsync()
        {
            const string cacheKey = "Kitchen_Summary";

            if (TryGetCache(cacheKey, out KitchenSummary? cachedSummary) && cachedSummary != null)
            {
                AppLogger.Info("Loaded kitchen summary from cache");
                return cachedSummary;
            }

            try
            {
                var client = await GetClientAsync();
                var response = await client
                    .From<KitchenSummary>()
                    .Get();

                var summary = response.Models?.FirstOrDefault();

                if (summary != null)
                {
                    SetCache(cacheKey, summary);
                    AppLogger.Info($"Loaded summary: Total={summary.TotalCount}, Normal={summary.NormalCount}, Low={summary.LowCount}");
                }

                return summary;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error loading kitchen summary: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Inserts a new kitchen item into the database.
        /// Cache is cleared after successful insertion.
        /// </summary>
        /// <param name="item">Kitchen item to insert</param>
        /// <returns>Inserted kitchen item</returns>
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
                {
                    AppLogger.Success($"Inserted kitchen item ID {inserted.Id}");
                    ClearCache();
                }

                return inserted;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error inserting kitchen item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates an existing kitchen item.
        /// Cache is cleared after update.
        /// </summary>
        /// <param name="item">Updated kitchen item</param>
        /// <returns>Updated kitchen item</returns>
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
                {
                    AppLogger.Success($"Updated kitchen item ID {updated.Id}");
                    ClearCache();
                }

                return updated;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error updating kitchen item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a kitchen item by ID.
        /// Cache is cleared after deletion.
        /// </summary>
        /// <param name="id">ID of kitchen item</param>
        /// <returns>True if deleted successfully</returns>
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
                ClearCache();

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error deleting kitchen item: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves all kitchen items ordered by name.
        /// No cache used to avoid stale bulk data.
        /// </summary>
        /// <returns>List of all kitchen items</returns>
        public async Task<List<Kitchen>> GetAllKitchenItemsAsync()
        {
            try
            {
                var client = await GetClientAsync();
                var response = await client
                    .From<Kitchen>()
                    .Order(k => k.ItemName, Ordering.Ascending)
                    .Get();

                return response.Models ?? new List<Kitchen>();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error loading all kitchen items: {ex.Message}");
                return new List<Kitchen>();
            }
        }

        /// <summary>
        /// Clears all related kitchen cache entries.
        /// Called after insert, update, or delete.
        /// </summary>
        private void ClearCache()
        {
            InvalidateCache("Kitchen_Summary");

            // Invalidate all paged data manually tracked
            // You could enhance this logic later to use prefix matching if needed
            //foreach (var page in Enumerable.Range(1, 10)) // Adjust this range based on expected page numbers
            //{
            //    InvalidateCache($"Kitchen_Page_{page}_Size_10");
            //    InvalidateCache($"Kitchen_Page_{page}_Size_20");
            //    InvalidateCache($"Kitchen_Page_{page}_Size_50");
            //}

            AppLogger.Info("Kitchen cache invalidated");
        }
    }
}
