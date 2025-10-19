using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    /// <summary>
    /// Handles scheduling operations: assigning workers to reservations,
    /// paginating results, searching schedules, and caching.
    /// </summary>
    public class SchedulingService : BaseCachedService
    {
        private Supabase.Client? _client;
        private const int PageSize = 10;

        /// <summary>
        /// Lazily retrieves the Supabase client.
        /// </summary>
        private async Task<Supabase.Client> GetClientAsync()
        {
            if (_client == null)
                _client = await SupabaseService.GetClientAsync();

            return _client;
        }

        /// <summary>
        /// Retrieves paged completed reservations with joins. Cached.
        /// </summary>
        public async Task<(List<Reservation> Reservations, int TotalCount)> GetCompletedReservationsPagedAsync(int pageNumber)
        {
            string cacheKey = $"CompletedReservations_Page_{pageNumber}";

            if (TryGetCache(cacheKey, out (List<Reservation>, int) cachedData))
                return cachedData;

            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client
                    .From<Reservation>()
                    .Select(@"
                        *,
                        profile:profile_id(*),
                        thememotif:theme_motif_id(*),
                        grazing:grazing_id(*),
                        package:package_id(*)
                    ")
                    .Where(r => r.Status == "completed")
                    .Order(static x => x.EventDate, Ordering.Ascending)
                    .Range(from, to)
                    .Get();

                var totalCount = await client
                    .From<Reservation>()
                    .Select("id")
                    .Where(r => r.Status == "completed")
                    .Count(CountType.Exact);

                var result = (response.Models ?? new List<Reservation>(), totalCount);
                SetCache(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error fetching completed reservations: {ex.Message}");
                return (new List<Reservation>(), 0);
            }
        }

        /// <summary>
        /// Gets a paged list of schedules with joined data. Cached.
        /// </summary>
        public async Task<List<Scheduling>> GetPagedSchedulesAsync(int pageNumber)
        {
            string cacheKey = $"Schedules_Page_{pageNumber}";

            if (TryGetCache(cacheKey, out List<Scheduling> cachedSchedules))
                return cachedSchedules;

            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client
                    .From<Scheduling>()
                    .Select(@"
                        *,
                        reservations:reservation_id(
                            *,
                            package:package_id(*),
                            profile:profile_id(*),
                            thememotif:theme_motif_id(*),
                            grazing:grazing_id(*)
                        ),
                        workers:worker_id(*)
                    ")
                    .Order(static x => x.CreatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                var schedules = response.Models ?? new List<Scheduling>();
                SetCache(cacheKey, schedules);
                return schedules;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error fetching paged schedules: {ex.Message}");
                return new List<Scheduling>();
            }
        }

        /// <summary>
        /// Groups schedules by reservation and includes assigned workers.
        /// </summary>
        public List<GroupSchedule> GroupSchedules(List<Scheduling> schedules)
        {
            return schedules
                .Where(s => s.Reservations != null && s.Workers != null)
                .GroupBy(s => s.ReservationId)
                .Select(group => new GroupSchedule
                {
                    Reservation = group.First().Reservations!,
                    Workers = group.Select(s => s.Workers!).ToList()
                })
                .ToList();
        }

        /// <summary>
        /// Searches schedules client-side based on reservation & worker info. Cached.
        /// </summary>
        public async Task<List<GroupSchedule>> SearchSchedulesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<GroupSchedule>();

            string cacheKey = $"SearchSchedules_{query.Trim().ToLower()}";

            if (TryGetCache(cacheKey, out List<GroupSchedule> cachedResults))
                return cachedResults;

            try
            {
                var client = await GetClientAsync();

                var reservations = await client
                    .From<Reservation>()
                    .Select(@"
                        *,
                        profile:profile_id(*),
                        package:package_id(*),
                        thememotif:theme_motif_id(*),
                        grazing:grazing_id(*)
                    ")
                    .Get();

                var schedules = await client
                    .From<Scheduling>()
                    .Select(@"
                        *,
                        reservations:reservation_id(
                            *,
                            profile:profile_id(*),
                            package:package_id(*),
                            thememotif:theme_motif_id(*),
                            grazing:grazing_id(*)
                        ),
                        workers:worker_id(*)
                    ")
                    .Get();

                var filtered = schedules.Models?.Where(s =>
                {
                    var r = s.Reservations;
                    var w = s.Workers;

                    return r != null && (
                        ContainsIgnoreCase(r.Location, query) ||
                        ContainsIgnoreCase(r.Venue, query) ||
                        ContainsIgnoreCase(r.Celebrant, query) ||
                        ContainsIgnoreCase(r.ReceiptNumber, query) ||
                        ContainsIgnoreCase(r.Profile?.FullName, query) ||
                        ContainsIgnoreCase(r.Package?.Name, query) ||
                        ContainsIgnoreCase(w?.Name, query)
                    );
                }).ToList() ?? new List<Scheduling>();

                var grouped = GroupSchedules(filtered);
                SetCache(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error searching schedules: {ex.Message}");
                return new List<GroupSchedule>();
            }
        }

        /// <summary>
        /// Gets all schedules tied to a specific reservation, with joins.
        /// </summary>
        public async Task<List<Scheduling>> GetSchedulesByReservationId(long reservationId)
        {
            try
            {
                var client = await GetClientAsync();

                var response = await client
                    .From<Scheduling>()
                    .Select(@"
                *,
                reservations:reservation_id(
                    *,
                    profile:profile_id(*),
                    package:package_id(*),
                    thememotif:theme_motif_id(*),
                    grazing:grazing_id(*)
                ),
                workers:worker_id(*)
            ")
                    .Match(new Dictionary<string, string>
                    {
                { "reservation_id", reservationId.ToString() }
                    })
                    .Get();

                return response.Models ?? new List<Scheduling>();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error fetching schedules by reservation ID: {ex.Message}");
                return new List<Scheduling>();
            }
        }


        /// <summary>
        /// Case-insensitive match helper.
        /// </summary>
        private bool ContainsIgnoreCase(string? source, string keyword)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Invalidates all schedule-related cached data.
        /// </summary>
        public void ClearCache()
        {
            InvalidateCache();
            AppLogger.Info("Scheduling cache invalidated");
        }
    }
}
