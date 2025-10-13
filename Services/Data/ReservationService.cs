using CATERINGMANAGEMENT.Models;
using System.Diagnostics;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class ReservationService
    {
        // Load reservations with joins (pagination)
        public async Task<List<Reservation>> GetReservationsAsync(int pageNumber, int pageSize)
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                int from = (pageNumber - 1) * pageSize;
                int to = from + pageSize - 1;

                var result = await client
                    .From<Reservation>()
                    .Select(@"
                        *,
                        profile:profile_id(*),
                        thememotif:theme_motif_id(*),
                        grazing:grazing_id(*),
                        package:package_id(*)
                    ")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                return result.Models;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error loading reservations: {ex.Message}");
                return new List<Reservation>();
            }
        }

        // Count total reservations
        public async Task<int> GetTotalCountAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                return await client
                    .From<Reservation>()
                    .Select("id")
                    .Count(CountType.Exact);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error counting reservations: {ex.Message}");
                return 0;
            }
        }

        // ✅ Update ANY field (not just status)
        public async Task<Reservation?> UpdateReservationAsync(Reservation reservation)
        {
            if (reservation == null) return null;

            try
            {
                var client = await SupabaseService.GetClientAsync();

                // Update all editable fields (you can adjust as needed)
                var updateResponse = await client
                    .From<Reservation>()
                    .Where(x => x.Id == reservation.Id)
                    .Set(r => r.Status, reservation.Status)
                    .Set(r => r.Celebrant, reservation.Celebrant)
                    .Set(r => r.Venue, reservation.Venue)
                    .Set(r => r.Location, reservation.Location)
                    .Set(r => r.EventDate, reservation.EventDate)
                    .Set(r => r.AdultsQty, reservation.AdultsQty)
                    .Set(r => r.KidsQty, reservation.KidsQty)
                    .Update();

                // Fetch updated record with joins
                var refreshed = await client
                    .From<Reservation>()
                    .Where(x => x.Id == reservation.Id)
                    .Select(@"
                        *,
                        profile:profile_id(*),
                        thememotif:theme_motif_id(*),
                        grazing:grazing_id(*),
                        package:package_id(*)
                    ")
                    .Single();

                return refreshed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error updating reservation: {ex.Message}");
                return null;
            }
        }

        // Delete
        public async Task<bool> DeleteReservationAsync(Reservation reservation)
        {
            if (reservation == null) return false;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.From<Reservation>().Where(x => x.Id == reservation.Id).Delete();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error deleting reservation: {ex.Message}");
                return false;
            }
        }
    }
}
