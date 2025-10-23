/*
 * FILE: SchedulingService.cs
 * PURPOSE: Service class for managing scheduling data operations — includes
 *          pagination, grouped schedules, search, and worker assignments.
 *          Implements in-memory caching for faster performance.
 *
 * FEATURES:
 * - Paginated grouped schedule fetching with caching.
 * - Completed reservations caching.
 * - Worker assignment removal with cache invalidation.
 * - Search for grouped schedules.
 * - Easy cache invalidation and consistency.
 *
 * AUTHOR: [Your Name or Team]
 * CREATED: [Creation Date]
 * LAST MODIFIED: [Last Modified Date]
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class SchedulingService : BaseCachedService
    {
        #region Constants
        private const int PageSize = 10;
        private const string CacheKey_Completed = "CompletedReservations";
        private const string CachePrefix_SchedulePage = "Schedule_Page_";
        #endregion

        #region Helpers
        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();
        #endregion

        #region Public Methods

        /// <summary>
        /// Fetches grouped schedules by page with caching.
        /// </summary>
        public async Task<(List<GroupedScheduleView> Schedules, int TotalCount)> GetPagedGroupedSchedulesAsync(int pageNumber)
        {
            string cacheKey = $"{CachePrefix_SchedulePage}{pageNumber}";

            if (TryGetCache(cacheKey, out (List<GroupedScheduleView> Schedules, int TotalCount) cachedPage)
                && cachedPage.Schedules != null && cachedPage.Schedules.Count > 0)
            {
                AppLogger.Info($"✅ Using cached grouped schedules (Page {pageNumber})");
                return cachedPage;
            }

            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client
                    .From<GroupedScheduleView>()
                    .Order(x => x.EventDate, Ordering.Ascending)
                    .Range(from, to)
                    .Get();

                var totalCount = await client
                    .From<GroupedScheduleView>()
                    .Select("reservation_id")
                    .Count(CountType.Exact);

                var schedules = response.Models ?? new List<GroupedScheduleView>();

                // Cache result
                SetCache(cacheKey, (schedules, totalCount));
                AppLogger.Info($"Cached grouped schedules (Page {pageNumber}) with {schedules.Count} items");

                return (schedules, totalCount);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Error fetching grouped schedules");
                return (new List<GroupedScheduleView>(), 0);
            }
        }

        /// <summary>
        /// Gets cached completed reservations (or fetches from server if not cached).
        /// </summary>
        public async Task<List<Reservation>> GetCompletedReservationsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && TryGetCache(CacheKey_Completed, out List<Reservation>? cached) && cached != null)
            {
                AppLogger.Info("✅ Using cached completed reservations");
                return cached;
            }

            try
            {
                var client = await GetClientAsync();
                var response = await client
                    .From<Reservation>()
                    .Select("*, package:packages(*)")
                    .Filter("status", Operator.Equals, "completed")
                    .Order(x => x.EventDate, Ordering.Ascending)
                    .Get();

                var completed = response.Models ?? new List<Reservation>();

                SetCache(CacheKey_Completed, completed);
                AppLogger.Info($"Cached {completed.Count} completed reservations");

                return completed;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Error fetching completed reservations");
                return new List<Reservation>();
            }
        }

        /// <summary>
        /// Removes a worker from a schedule and invalidates caches.
        /// </summary>
        public async Task<bool> RemoveWorkerFromScheduleAsync(long reservationId, long workerId)
        {
            try
            {
                var client = await GetClientAsync();
                AppLogger.Info($"Removing worker {workerId} from reservation {reservationId}");

                await client
                    .From<NewScheduling>()
                    .Where(x => x.ReservationId == reservationId && x.WorkerId == workerId)
                    .Delete();

                InvalidateAllSchedulingCaches();
                AppLogger.Info("✅ Invalidated scheduling caches after worker removal");

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Failed to remove worker from schedule");
                return false;
            }
        }

        /// <summary>
        /// Searches grouped schedules (no caching for search results).
        /// </summary>
        public async Task<List<GroupedScheduleView>> SearchGroupedSchedulesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<GroupedScheduleView>();

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<GroupedScheduleView>().Get();

                var filtered = response.Models?
                    .Where(s =>
                        ContainsIgnoreCase(s.ClientName, query) ||
                        ContainsIgnoreCase(s.PackageName, query) ||
                        ContainsIgnoreCase(s.Location, query) ||
                        ContainsIgnoreCase(s.Venue, query) ||
                        ContainsIgnoreCase(s.AssignedWorkers, query) ||
                        ContainsIgnoreCase(s.ReceiptNumber, query))
                    .ToList()
                    ?? new List<GroupedScheduleView>();

                return filtered;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Search failed for grouped schedules");
                return new List<GroupedScheduleView>();
            }
        }

        #endregion

        #region Private Helpers
        private bool ContainsIgnoreCase(string? source, string keyword) =>
            !string.IsNullOrEmpty(source) &&
            source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        #endregion

        #region Cache Management
        /// <summary>
        /// Clears all caches related to scheduling.
        /// </summary>
        public void InvalidateAllSchedulingCaches()
        {
            InvalidateCache(CacheKey_Completed);
            InvalidateCacheByPrefix(CachePrefix_SchedulePage);
        }
        #endregion
    }
}
