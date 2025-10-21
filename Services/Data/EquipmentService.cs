using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    /// <summary>
    /// Handles CRUD operations for equipment, retrieval of summaries, and caching via BaseCachedService.
    /// </summary>
    public class EquipmentService : BaseCachedService
    {
        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        public async Task<List<Equipment>> GetEquipmentsAsync(int pageNumber, int pageSize)
        {
            string cacheKey = $"Equipments_Page_{pageNumber}_Size_{pageSize}";
            if (TryGetCache(cacheKey, out List<Equipment>? cachedList) && cachedList != null)
                return cachedList;

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

                var list = response.Models ?? new List<Equipment>();
                SetCache(cacheKey, list);
                return list;
            }
            catch
            {
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
                if (summary != null) SetCache(cacheKey, summary);
                return summary;
            }
            catch
            {
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
                if (inserted != null) ClearCache();
                return inserted;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Equipment?> UpdateEquipmentAsync(Equipment equipment)
        {
            if (equipment == null) return null;
            try
            {
                var client = await GetClientAsync();
                var updated = (await client.From<Equipment>().Where(e => e.Id == equipment.Id).Update(equipment)).Models?.FirstOrDefault();
                if (updated != null) ClearCache();
                return updated;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteEquipmentAsync(long id)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Equipment>().Where(e => e.Id == id).Delete();
                ClearCache();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ClearCache()
        {
            InvalidateCache("Equipment_Summary");
        }
    }
}
