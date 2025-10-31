/*
 * ReservationService.cs
 * 
 * Service class responsible for managing reservation data operations including
 * CRUD functionalities, retrieval with related entities, and caching support.
 * Inherits caching functionality from BaseCachedService.
 * 
 * Features:
 * - Paginated reservation fetching with caching.
 * - Fetching reservation status counts with caching.
 * - Updating, deleting reservations with cache invalidation.
 * - Retrieval of reservation menu orders.
 * - Fetching a reservation with related joined data.
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
    #region ReservationService Implementation
    public class ReservationService : BaseCachedService
    {
        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        public async Task<List<Reservation>> GetReservationsAsync(int pageNumber, int pageSize)
        {
            try
            {
                string cacheKey = $"Reservations_Page_{pageNumber}_Size_{pageSize}";

                if (TryGetCache(cacheKey, out List<Reservation>? cachedReservations) && cachedReservations != null)
                    return cachedReservations;

                var client = await GetClientAsync();

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

                var reservations = result.Models ?? new List<Reservation>();

                SetCache(cacheKey, reservations);
                return reservations;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error loading reservations: {ex.Message}");
                return new List<Reservation>();
            }
        }

        public async Task<ReservationStatusCount?> GetReservationStatusCountsAsync()
        {
            const string cacheKey = "Reservation_Status_Count";

            if (TryGetCache(cacheKey, out ReservationStatusCount? cachedCounts) && cachedCounts != null)
                return cachedCounts;

            try
            {
                var client = await GetClientAsync();

                var result = await client
                    .From<ReservationStatusCount>()
                    .Get();

                var countData = result.Models?.FirstOrDefault();

                if (countData != null)
                    SetCache(cacheKey, countData);

                return countData;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting reservation status counts: {ex.Message}");
                return null;
            }
        }

        public async Task<Reservation?> UpdateReservationAsync(Reservation reservation)
        {
            if (reservation == null) return null;

            try
            {
                var client = await GetClientAsync();

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

                InvalidateAllReservationCaches();

                var updated = await client
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

                return updated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error updating reservation: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteReservationAsync(Reservation reservation)
        {
            if (reservation == null) return false;

            try
            {
                var client = await GetClientAsync();

                await client
                    .From<Reservation>()
                    .Where(x => x.Id == reservation.Id)
                    .Delete();

                InvalidateAllReservationCaches();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error deleting reservation: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ReservationMenuOrder>> GetReservationMenuOrdersAsync(long reservationId)
        {
            try
            {
                var client = await GetClientAsync();

                var response = await client
                    .From<ReservationMenuOrder>()
                    .Select("*, menu_options(*)")
                    .Where(x => x.ReservationId == reservationId)
                    .Get();

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
                var client = await GetClientAsync();

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

        public void InvalidateAllReservationCaches()
        {
            InvalidateCache("Reservation_Status_Count");
            InvalidateCacheByPrefix("Reservations_Page_");
        }
 
        public async Task<Reservation?> UpdateReservationStatusAsync(long reservationId, string status)
        {
            try
            {
                var client = await GetClientAsync();

                await client
                    .From<Reservation>()
                    .Where(x => x.Id == reservationId)
                    .Set(r => r.Status, status)
                    .Update();

                InvalidateAllReservationCaches();

                var updated = await client
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

                return updated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error updating reservation status: {ex.Message}");
                return null;
            }
        }
    }
    #endregion
}
