using CATERINGMANAGEMENT.Models;
using System.Diagnostics;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class ReservationService
    {
        //  Get paginated reservations with joins
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

        //  Get reservation count
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

        //  Update reservation
        public async Task<Reservation?> UpdateReservationAsync(Reservation reservation)
        {
            if (reservation == null) return null;

            try
            {
                Debug.WriteLine("🔁 UpdateReservationAsync called");
                var client = await SupabaseService.GetClientAsync();

                await client
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

        //  Delete reservation
        public async Task<bool> DeleteReservationAsync(Reservation reservation)
        {
            if (reservation == null) return false;

            try
            {
                var client = await SupabaseService.GetClientAsync();

                await client
                    .From<Reservation>()
                    .Where(x => x.Id == reservation.Id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error deleting reservation: {ex.Message}");
                return false;
            }
        }

        //  Get all menu orders for a reservation
        public async Task<List<ReservationMenuOrder>> GetReservationMenuOrdersAsync(long reservationId)
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var response = await client
                    .From<ReservationMenuOrder>()
                    .Select("*, menu_options(*)")
                    .Where(x => x.ReservationId == reservationId)
                    .Get();

                Debug.WriteLine("📦 RAW Supabase JSON Response:");
                Debug.WriteLine(response.Content);

                return response.Models ?? new List<ReservationMenuOrder>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error loading menu orders: {ex.Message}");
                return new List<ReservationMenuOrder>();
            }
        }


        public async Task<Reservation?> GetReservationWithJoinsAsync(long reservationId)
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var result = await client
                    .From<Reservation>()
                    .Where(x => x.Id == reservationId)
                    .Select(@"
                *,
                profile:profile_id(*),
                thememotif:theme_motif_id(*),
                grazing:grazing_id(*),
                package:package_id(*)
            ")
                    .Single();

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting reservation with joins: {ex.Message}");
                return null;
            }
        }

    }
}
