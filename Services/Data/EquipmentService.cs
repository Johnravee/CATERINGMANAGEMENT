using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    /// <summary>
    /// Handles CRUD operations for equipment (cache disabled temporarily).
    /// </summary>
    public class EquipmentService : BaseCachedService
    {
        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        public async Task<List<Equipment>> GetEquipmentsAsync(int pageNumber, int pageSize)
        {
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

                return response.Models ?? new List<Equipment>();
            }
            catch
            {
                return new List<Equipment>();
            }
        }

        public async Task<EquipmentSummary?> GetEquipmentSummaryAsync()
        {
            try
            {
                var client = await GetClientAsync();
                var summary = (await client.From<EquipmentSummary>().Get()).Models?.FirstOrDefault();
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
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Cache-clearing temporarily disabled
        // private void ClearCache()
        // {
        //     InvalidateCache("Equipment_Summary");
        // }
    }
}
