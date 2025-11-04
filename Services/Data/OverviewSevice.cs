/*
 * FILE: OverviewService.cs
 * PURPOSE: Handles all data fetching for the Dashboard module.
 *          Provides methods to get dashboard counters, monthly reservations,
 *          upcoming reservations, and event type distribution.
 *          Added in-memory caching for performance.
 */

using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.DocumentsGenerator;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class OverviewService
    {
        #region Cache
        private readonly Dictionary<string, (object Data, DateTime Expiry)> _cache = new();
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

        private bool TryGetFromCache<T>(string key, out T? result)
        {
            result = default;
            if (_cache.ContainsKey(key))
            {
                var (data, expiry) = _cache[key];
                if (DateTime.Now <= expiry)
                {
                    result = (T)data;
                    return true;
                }
                else
                {
                    _cache.Remove(key); // expired
                }
            }
            return false;
        }

        private void SetCache(string key, object data)
        {
            _cache[key] = (data, DateTime.Now.Add(_cacheDuration));
        }
        #endregion

        #region Dashboard Counters
        public async Task<DashboardCounters?> GetDashboardCountersAsync()
        {
            const string cacheKey = "DashboardCounters";

            if (TryGetFromCache(cacheKey, out DashboardCounters? cached))
                return cached;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<DashboardCounters>().Get();
                var counters = response.Models.FirstOrDefault();

                if (counters != null)
                    SetCache(cacheKey, counters);

                return counters;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region Monthly Reservations
        public async Task<List<MonthlyReservationSummary>> GetMonthlyReservationSummariesAsync()
        {
            const string cacheKey = "MonthlyReservations";

            if (TryGetFromCache(cacheKey, out List<MonthlyReservationSummary>? cached))
                return cached;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<MonthlyReservationSummary>().Get();
                var list = response.Models ?? new List<MonthlyReservationSummary>();

                SetCache(cacheKey, list);
                return list;
            }
            catch
            {
                return new List<MonthlyReservationSummary>();
            }
        }

        // Generate a CSV report for the given month and year; return file path or null.
        public async Task<string?> GenerateMonthlyReservationsReportAsync(int month, int year)
        {
            try
            {
                var summaries = await GetMonthlyReservationSummariesAsync();
                var found = summaries.FirstOrDefault(s => s.ReservationMonth == month && s.ReservationYear == year);
                if (found == null)
                    return null;

                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CateringReports");
                Directory.CreateDirectory(folder);

                var fileName = $"MonthlyReservations_{year}_{month:00}.csv";
                var path = Path.Combine(folder, fileName);

                using (var sw = new StreamWriter(path, false))
                {
                    await sw.WriteLineAsync("Year,Month,Label,TotalReservations");
                    await sw.WriteLineAsync($"{found.ReservationYear},{found.ReservationMonth},\"{found.YearMonthLabel}\",{found.TotalReservations}");
                }

                return path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error generating monthly report: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region Upcoming Reservations
        public async Task<List<Reservation>> GetUpcomingReservationsAsync()
        {
            const string cacheKey = "UpcomingReservations";

            if (TryGetFromCache(cacheKey, out List<Reservation>? cached))
                return cached;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Reservation>()
                    .Select("id, receipt_number, event_date, venue")
                    .Where(r => r.Status == "completed")
                    .Get();
                var list = response.Models ?? new List<Reservation>();

                SetCache(cacheKey, list);
                return list;
            }
            catch
            {
                return new List<Reservation>();
            }
        }
        #endregion

        #region Event Type Distribution
        public async Task<List<Reservation>> GetAllReservationsWithPackageAsync()
        {
            const string cacheKey = "AllReservationsWithPackage";

            if (TryGetFromCache(cacheKey, out List<Reservation>? cached))
                return cached;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Reservation>()
                    .Select("*, package:package_id(name)")
                    .Get();
                var list = response.Models ?? new List<Reservation>();

                SetCache(cacheKey, list);
                return list;
            }
            catch
            {
                return new List<Reservation>();
            }
        }

        public Dictionary<string, int> GetEventTypeDistribution(List<Reservation> reservations)
        {
            const string cacheKey = "EventTypeDistribution";

            if (TryGetFromCache(cacheKey, out Dictionary<string, int>? cached))
                return cached;

            if (reservations == null || reservations.Count == 0)
                return new Dictionary<string, int>();

            var distribution = reservations
                .Where(r => r.Package != null)
                .GroupBy(r => r.Package!.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            SetCache(cacheKey, distribution);
            return distribution;
        }
        #endregion
    }
}
