using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using Microsoft.Extensions.Caching.Memory;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    /// <summary>
    /// Handles all scheduling operations — fetching grouped schedules,
    /// pagination, searching, and worker removal.
    /// Includes in-memory caching via BaseCachedService.
    /// </summary>
    public class SchedulingService : BaseCachedService
    {
        private Supabase.Client? _client;
        private const int PageSize = 10;

        private async Task<Supabase.Client> GetClientAsync()
        {
            _client ??= await SupabaseService.GetClientAsync();
            return _client;
        }

        // 🔑 cache keys
        private string CacheKey_Grouped(int page) => $"GroupedSchedules_Page_{page}";
        private const string CacheKey_Completed = "CompletedReservations";

        /// <summary>
        /// Retrieves paged grouped schedules (cached per page).
        /// </summary>
        public async Task<(List<GroupedScheduleView> Schedules, int TotalCount)> GetPagedGroupedSchedulesAsync(int pageNumber)
        {
            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                AppLogger.Info($"[FETCH] Loading grouped schedules page {pageNumber} ({from}–{to})...");

                var response = await client
                    .From<GroupedScheduleView>()
                    .Order(x => x.EventDate, Ordering.Ascending)
                    .Range(from, to)
                    .Get();

                var totalCount = await client
                    .From<GroupedScheduleView>()
                    .Select("reservation_id")
                    .Count(CountType.Exact);

                return (response.Models ?? new List<GroupedScheduleView>(), totalCount);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ Error fetching grouped schedules: {ex}");
                return (new List<GroupedScheduleView>(), 0);
            }
        }


        /// <summary>
        /// Retrieves all completed reservations (cached).
        /// </summary>
        public async Task<List<Reservation>> GetCompletedReservationsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && TryGetCache(CacheKey_Completed, out List<Reservation>? cached) && cached != null)
            {
                AppLogger.Info("[CACHE] Using cached completed reservations");
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

                // 💾 Cache
                SetCache(CacheKey_Completed, completed);
                AppLogger.Info($"[CACHE] Stored {completed.Count} completed reservations");

                return completed;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ Error fetching completed reservations: {ex.Message}");
                return new List<Reservation>();
            }
        }

        /// <summary>
        /// Removes a specific worker from a reservation’s schedule.
        /// Invalidates affected caches automatically.
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

                // 🚮 Invalidate relevant caches
                InvalidateCache(CacheKey_Completed);
                AppLogger.Info("[CACHE] Invalidated caches after worker removal");

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ Failed to remove worker from schedule: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Local search (no cache).
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
                AppLogger.Error($"Error searching grouped schedules: {ex.Message}");
                return new List<GroupedScheduleView>();
            }
        }

        


        private bool ContainsIgnoreCase(string? source, string keyword) =>
            !string.IsNullOrEmpty(source) &&
            source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
