using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    /// <summary>
    /// Handles scheduling operations: fetching schedules, pagination, searching, worker removal.
    /// Uses in-memory caching via BaseCachedService.
    /// </summary>
    public class SchedulingService : BaseCachedService
    {

        private const int PageSize = 10;

        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        // Cache keys
        private const string CacheKey_Completed = "CompletedReservations";

        public async Task<(List<GroupedScheduleView> Schedules, int TotalCount)> GetPagedGroupedSchedulesAsync(int pageNumber)
        {
            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                AppLogger.Info($"Loading grouped schedules page {pageNumber} ({from}-{to})");

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
                AppLogger.Error(ex, "Error fetching grouped schedules");
                return (new List<GroupedScheduleView>(), 0);
            }
        }

        public async Task<List<Reservation>> GetCompletedReservationsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && TryGetCache(CacheKey_Completed, out List<Reservation>? cached) && cached != null)
            {
                AppLogger.Info("Using cached completed reservations");
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
                AppLogger.Error(ex, "Error fetching completed reservations");
                return new List<Reservation>();
            }
        }

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

                InvalidateCache(CacheKey_Completed);
                AppLogger.Info("Invalidated caches after worker removal");

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to remove worker from schedule");
                return false;
            }
        }

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
                AppLogger.Error(ex, "Search failed for grouped schedules");
                return new List<GroupedScheduleView>();
            }
        }

        private bool ContainsIgnoreCase(string? source, string keyword) =>
            !string.IsNullOrEmpty(source) &&
            source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
