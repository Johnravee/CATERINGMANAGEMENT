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
using Supabase.Postgrest.Exceptions;
using System.Diagnostics;
using System.Net.Http;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    #region ReservationService Implementation
    public class ReservationService : BaseCachedService
    {
        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        // Normalize dates to midday UTC to avoid tz-based day shifts when PostgREST casts to DATE
        private static DateTime NormalizeDate(DateTime dt)
            => DateTime.SpecifyKind(dt.Date.AddHours(12), DateTimeKind.Utc);

        private static void NormalizeReservationDates(Reservation? r)
        {
            if (r == null) return;
            r.EventDate = r.EventDate.Date;
        }

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

                foreach (var r in reservations)
                    NormalizeReservationDates(r);

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
                    .Set(r => r.EventDate, NormalizeDate(reservation.EventDate))
                    .Set(r => r.EventTime, reservation.EventTime)
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

                NormalizeReservationDates(updated);
                return updated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error updating reservation: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fine-grained update for reservation details (does NOT modify Status unless explicitly passed).
        /// Pass only the fields you want to change; null means leave as-is.
        /// </summary>
        public async Task<Reservation?> UpdateReservationDetailsAsync(
            long reservationId,
            DateTime? eventDate = null,
            TimeSpan? eventTime = null,
            string? celebrant = null,
            string? venue = null,
            string? location = null,
            long? adultsQty = null,
            long? kidsQty = null,
            long? themeMotifId = null,
            long? packageId = null,
            long? grazingId = null,
            string? newStatus = null)
        {
            try
            {
                bool anyChanges = eventDate.HasValue || eventTime.HasValue || celebrant != null || venue != null || location != null ||
                                   adultsQty.HasValue || kidsQty.HasValue || themeMotifId.HasValue || packageId.HasValue || grazingId.HasValue ||
                                   !string.IsNullOrWhiteSpace(newStatus);

                if (!anyChanges)
                {
                    var current = await GetReservationWithJoinsAsync(reservationId);
                    NormalizeReservationDates(current);
                    return current;
                }

                var client = await GetClientAsync();
                var builder = client
                    .From<Reservation>()
                    .Where(r => r.Id == reservationId);

                if (eventDate.HasValue) builder = builder.Set(r => r.EventDate, NormalizeDate(eventDate.Value));
                if (eventTime.HasValue) builder = builder.Set(r => r.EventTime, eventTime.Value);
                if (celebrant != null) builder = builder.Set(r => r.Celebrant, celebrant);
                if (venue != null) builder = builder.Set(r => r.Venue, venue);
                if (location != null) builder = builder.Set(r => r.Location, location);
                if (adultsQty.HasValue) builder = builder.Set(r => r.AdultsQty, adultsQty.Value);
                if (kidsQty.HasValue) builder = builder.Set(r => r.KidsQty, kidsQty.Value);
                if (themeMotifId.HasValue) builder = builder.Set(r => r.ThemeMotifId, themeMotifId.Value);
                if (packageId.HasValue) builder = builder.Set(r => r.PackageId, packageId.Value);
                if (grazingId.HasValue) builder = builder.Set(r => r.GrazingId, grazingId.Value);
                if (!string.IsNullOrWhiteSpace(newStatus)) builder = builder.Set(r => r.Status, newStatus!);

                await builder.Update();

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

                NormalizeReservationDates(updated);
                return updated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error updating reservation details: {ex.Message}");
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
            catch (HttpRequestException)
            {
                throw;
            }
            catch (PostgrestException pex)
            {
                Debug.WriteLine($"❌ Postgrest error deleting reservation: {pex.Message}");
                return false;
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

                NormalizeReservationDates(result);
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

                NormalizeReservationDates(updated);
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
