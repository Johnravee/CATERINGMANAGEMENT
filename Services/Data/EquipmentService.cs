/*
 * EquipmentService.cs
 * 
 * Service class responsible for managing equipment data operations including
 * CRUD functionalities, retrieval with pagination and summary, and caching support.
 * Inherits caching functionality from BaseCachedService.
 * 
 * Features:
 * - Paginated equipment fetching with caching.
 * - Fetching equipment summary with caching.
 * - Insert, update, delete operations with cache invalidation.
 * 
 * Author: [Your Name or Team]
 * Created: [Creation Date]
 * Last Modified: [Last Modified Date]
 */

using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using System.Diagnostics;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    #region EquipmentService Implementation
    public class EquipmentService : BaseCachedService
    {
        #region Private Methods

        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        #endregion

        #region Public Methods

        public async Task<List<Equipment>> GetEquipmentsAsync(int pageNumber, int pageSize)
        {
            string cacheKey = $"Equipments_Page_{pageNumber}_Size_{pageSize}";

            if (TryGetCache(cacheKey, out List<Equipment>? cachedEquipments) && cachedEquipments != null)
                return cachedEquipments;

            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * pageSize;
                int to = from + pageSize - 1;

                var response = await client
                    .From<Equipment>()
                    .Order(e => e.UpdatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                var equipments = response.Models ?? new List<Equipment>();

                SetCache(cacheKey, equipments);

                return equipments;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error loading equipments: {ex.Message}");
                return new List<Equipment>();
            }
        }

        public async Task<EquipmentSummary?> GetEquipmentSummaryAsync()
        {
            const string cacheKey = "Equipment_Summary";

            if (TryGetCache(cacheKey, out EquipmentSummary? cachedSummary) && cachedSummary != null)
                return cachedSummary;

            try
            {
                var client = await GetClientAsync();
                var summary = (await client.From<EquipmentSummary>().Get()).Models?.FirstOrDefault();

                if (summary != null)
                    SetCache(cacheKey, summary);

                return summary;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting equipment summary: {ex.Message}");
                return null;
            }
        }

        public async Task<Equipment?> InsertEquipmentAsync(Equipment newEquipment)
        {
            if (newEquipment == null) return null;

            try
            {
                var client = await GetClientAsync();
                var inserted = (await client.From<Equipment>().Insert(newEquipment)).Models?.FirstOrDefault();

                InvalidateAllEquipmentCaches();

                return inserted;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error inserting equipment: {ex.Message}");
                return null;
            }
        }

        public async Task<Equipment?> UpdateEquipmentAsync(Equipment equipment)
        {
            if (equipment == null) return null;

            try
            {
                var client = await GetClientAsync();
                var updated = (await client.From<Equipment>()
                    .Where(e => e.Id == equipment.Id)
                    .Update(equipment)).Models?.FirstOrDefault();

                InvalidateAllEquipmentCaches();

                return updated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error updating equipment: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteEquipmentAsync(long id)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Equipment>().Where(e => e.Id == id).Delete();

                InvalidateAllEquipmentCaches();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error deleting equipment: {ex.Message}");
                return false;
            }
        }

        public void InvalidateAllEquipmentCaches()
        {
            InvalidateCache("Equipment_Summary");
            InvalidateCacheByPrefix("Equipments_Page_");
        }

        #endregion
    }
    #endregion
}
