using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    /// <summary>
    /// Service class responsible for managing equipment data,
    /// including retrieval, insertion, update, deletion, and caching.
    /// Inherits caching functionality from BaseCachedService.
    /// </summary>
    public class EquipmentService : BaseCachedService
    {
        private Supabase.Client? _client;

        /// <summary>
        /// Default constructor calling base cached service constructor.
        /// </summary>
        public EquipmentService() : base()
        {
        }

        /// <summary>
        /// Lazily initializes and returns the Supabase client instance for database access.
        /// </summary>
        /// <returns>Supabase client</returns>
        private async Task<Supabase.Client> GetClientAsync()
        {
            if (_client == null)
                _client = await SupabaseService.GetClientAsync();

            return _client;
        }

        /// <summary>
        /// Retrieves a paginated list of equipment items from cache if available,
        /// otherwise fetches from the database and caches the results.
        /// </summary>
        /// <param name="pageNumber">Page number starting from 1</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>List of equipment items</returns>
        public async Task<List<Equipment>> GetEquipmentsAsync(int pageNumber, int pageSize)
        {
            string cacheKey = $"Equipments_Page_{pageNumber}_Size_{pageSize}";

            // Check if the equipment list for this page is cached
            if (TryGetCache(cacheKey, out List<Equipment>? cachedList) && cachedList != null)
            {
                AppLogger.Info($"Loaded equipment page {pageNumber} from cache");
                return cachedList;
            }

            try
            {
                var client = await GetClientAsync();

                int from = (pageNumber - 1) * pageSize;
                int to = from + pageSize - 1;

                // Query equipment data ordered by updated date descending
                var response = await client
                    .From<Equipment>()
                    .Order(e => e.UpdatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                var list = response.Models ?? new List<Equipment>();

                // Cache the fetched list
                SetCache(cacheKey, list);
                AppLogger.Info($" Loaded {list.Count} equipment items from Supabase (page {pageNumber})");
                return list;
            }
            catch (Exception ex)
            {
                AppLogger.Error($" Error fetching equipment list: {ex.Message}");
                return new List<Equipment>();
            }
        }

        /// <summary>
        /// Retrieves a summary of equipment counts (e.g., total, damaged, good),
        /// either from cache or from the database if not cached.
        /// </summary>
        /// <returns>EquipmentSummary object or null if error occurs</returns>
        public async Task<EquipmentSummary?> GetEquipmentSummaryAsync()
        {
            const string cacheKey = "Equipment_Summary";

            if (TryGetCache(cacheKey, out EquipmentSummary? cachedSummary) && cachedSummary != null)
            {
                AppLogger.Info("Loaded equipment summary from cache");
                return cachedSummary;
            }

            try
            {
                var client = await GetClientAsync();

                var response = await client
                    .From<EquipmentSummary>()
                    .Get();

                var summary = response.Models?.FirstOrDefault();

                if (summary != null)
                {
                    SetCache(cacheKey, summary);
                    AppLogger.Info($"Loaded summary: Total={summary.TotalCount}, Damaged={summary.DamagedCount}, Good={summary.GoodCount}");
                }

                return summary;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error loading equipment summary: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Inserts a new equipment record into the database.
        /// Clears the cache upon successful insertion.
        /// </summary>
        /// <param name="newEquipment">New equipment object to insert</param>
        /// <returns>Inserted equipment with generated ID, or null if failed</returns>
        public async Task<Equipment?> InsertEquipmentAsync(Equipment newEquipment)
        {
            if (newEquipment == null) return null;

            try
            {
                var client = await GetClientAsync();

                var response = await client
                    .From<Equipment>()
                    .Insert(newEquipment);

                var inserted = response.Models?.FirstOrDefault();

                if (inserted != null)
                {
                    AppLogger.Success($"Inserted equipment ID {inserted.Id}");
                    ClearCache();
                }

                return inserted;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error inserting equipment: {ex.Message}", showToUser: true);
                return null;
            }
        }

        /// <summary>
        /// Updates an existing equipment record in the database.
        /// Clears the cache upon successful update.
        /// </summary>
        /// <param name="equipment">Equipment object with updated data</param>
        /// <returns>Updated equipment object, or null if failed</returns>
        public async Task<Equipment?> UpdateEquipmentAsync(Equipment equipment)
        {
            if (equipment == null) return null;

            try
            {
                var client = await GetClientAsync();

                var response = await client
                    .From<Equipment>()
                    .Where(e => e.Id == equipment.Id)
                    .Update(equipment);

                var updated = response.Models?.FirstOrDefault();

                if (updated != null)
                {
                    AppLogger.Success($"Updated equipment ID {updated.Id}");
                    ClearCache();
                }

                return updated;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error updating equipment: {ex.Message}", showToUser: true);
                return null;
            }
        }

        /// <summary>
        /// Deletes an equipment record from the database by its ID.
        /// Clears the cache upon successful deletion.
        /// </summary>
        /// <param name="id">ID of the equipment to delete</param>
        /// <returns>True if deletion succeeded; false otherwise</returns>
        public async Task<bool> DeleteEquipmentAsync(long id)
        {
            try
            {
                var client = await GetClientAsync();

                await client
                    .From<Equipment>()
                    .Where(e => e.Id == id)
                    .Delete();

                AppLogger.Success($"Deleted equipment ID {id}");
                ClearCache();
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error deleting equipment: {ex.Message}", showToUser: true);
                return false;
            }
        }

        /// <summary>
        /// Helper method to invalidate cache entries related to equipment,
        /// such as equipment summaries.
        /// </summary>
        private void ClearCache()
        {
            InvalidateCache("Equipment_Summary");
            AppLogger.Info("Equipment cache invalidated");
        }
    }
}
