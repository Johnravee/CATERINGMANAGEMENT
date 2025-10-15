using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using Supabase.Postgrest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    /// <summary>
    /// Service class responsible for managing reservation data,
    /// including CRUD operations and caching.
    /// Inherits caching functionality from BaseCachedService.
    /// </summary>
    public class ReservationService : BaseCachedService
    {
        private Supabase.Client? _client;

        /// <summary>
        /// Default constructor calling base caching service constructor.
        /// </summary>
        public ReservationService() : base() { }

        /// <summary>
        /// Lazy loads and returns a Supabase client instance for database operations.
        /// </summary>
        /// <returns>Supabase client</returns>
        private async Task<Supabase.Client> GetClientAsync()
        {
            if (_client == null)
                _client = await SupabaseService.GetClientAsync();
            return _client;
        }

        /// <summary>
        /// Retrieves a paginated list of reservations, including related entities,
        /// from cache if available; otherwise fetches from the database and caches the result. 
        /// </summary>
        /// <param name="pageNumber">Page number starting from 1</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>List of reservations with related data</returns>
        public async Task<List<Reservation>> GetReservationsAsync(int pageNumber, int pageSize)
        {
            try
            {
                string cacheKey = $"Reservations_Page_{pageNumber}_Size_{pageSize}";

                // Try retrieving cached reservations for the requested page
                if (TryGetCache(cacheKey, out List<Reservation>? cachedReservations) && cachedReservations != null)
                    return cachedReservations;

                var client = await GetClientAsync();

                int from = (pageNumber - 1) * pageSize;
                int to = from + pageSize - 1;

                // Query the database with joins on related entities
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

                // Cache the fetched results for future use
                SetCache(cacheKey, reservations);
                return reservations;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error loading reservations: {ex.Message}");
                return new List<Reservation>();
            }
        }

        /// <summary>
        /// Retrieves the counts of reservations by their statuses from cache if available;
        /// otherwise fetches from the database and caches the result.
        /// </summary>
        /// <returns>ReservationStatusCount object containing count data</returns>
        public async Task<ReservationStatusCount?> GetReservationStatusCountsAsync()
        {
            const string cacheKey = "Reservation_Status_Count";

            // Attempt to get cached status counts
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

        /// <summary>
        /// Updates a reservation record with new values.
        /// After successful update, invalidates relevant cached entries.
        /// </summary>
        /// <param name="reservation">Reservation object with updated data</param>
        /// <returns>The updated Reservation with related data, or null if failed</returns>
        public async Task<Reservation?> UpdateReservationAsync(Reservation reservation)
        {
            if (reservation == null) return null;

            try
            {
                var client = await GetClientAsync();

                // Update reservation fields in the database
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

                // Invalidate related cache entries because data changed
                InvalidateCache("Reservation_Status_Count");

                // Retrieve updated reservation with related data
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

        /// <summary>
        /// Deletes a reservation record from the database.
        /// Invalidates relevant cached data upon successful deletion.
        /// </summary>
        /// <param name="reservation">Reservation to delete</param>
        /// <returns>True if successful; otherwise, false</returns>
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

                // Invalidate cache related to reservations
                InvalidateCache("Reservation_Status_Count");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error deleting reservation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves all menu orders associated with a particular reservation.
        /// </summary>
        /// <param name="reservationId">Reservation ID</param>
        /// <returns>List of ReservationMenuOrder objects, or empty list if none found or on error</returns>
        public async Task<List<ReservationMenuOrder>> GetReservationMenuOrdersAsync(long reservationId)
        {
            try
            {
                var client = await GetClientAsync();

                var response = await client
                    .From<ReservationMenuOrder>()
                    .Select("*, menu_options(*)") // Include menu option details
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

        /// <summary>
        /// Retrieves a reservation along with its related entities based on reservation ID.
        /// </summary>
        /// <param name="reservationId">Reservation ID</param>
        /// <returns>Reservation object with related data or null on failure</returns>
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
    }
}
