/*
 * FILE: PayrollService.cs
 * PURPOSE: Handles all payroll-related data operations, including fetching workers,
 *          reservations, payroll records (by reservation or by worker with cutoff),
 *          pagination, marking as paid, and deleting payroll records.
 *          Utilizes caching to improve performance.
 * 
 * RESPONSIBILITIES:
 *  - Retrieve all workers and reservations with caching
 *  - Fetch payrolls by reservation or by worker + cutoff dates
 *  - Support paginated payroll retrieval
 *  - Mark payroll records as paid
 *  - Delete payroll records
 *  - Invalidate relevant caches when data changes
 * 
 * Author: [Your Name]
 * Created: [Creation Date]
 * Last Modified: [Last Modified Date]
 */

using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using System.Diagnostics;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    #region PayrollService Implementation
    public class PayrollService : BaseCachedService
    {
        #region Constants
        private const int PageSize = 20;
        private const string WorkersCacheKey = "Workers_List";
        private const string ReservationsCacheKey = "Reservations_List";
        #endregion

        #region Supabase Client
        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();
        #endregion

        #region Workers
        public async Task<List<Worker>> GetAllWorkersAsync()
        {
            if (TryGetCache(WorkersCacheKey, out List<Worker>? cached) && cached != null)
                return cached;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Worker>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var workers = response.Models ?? new List<Worker>();
                SetCache(WorkersCacheKey, workers);
                return workers;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error fetching workers: {ex.Message}");
                return new List<Worker>();
            }
        }

        public void InvalidateWorkersCache() => InvalidateCache(WorkersCacheKey);
        #endregion

        #region Reservations
        public async Task<List<Reservation>> GetAllReservationsAsync()
        {
            if (TryGetCache(ReservationsCacheKey, out List<Reservation>? cached) && cached != null)
                return cached;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Reservation>()
                    .Order(x => x.EventDate, Ordering.Descending)
                    .Get();

                var reservations = response.Models ?? new List<Reservation>();
                SetCache(ReservationsCacheKey, reservations);
                return reservations;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error fetching reservations: {ex.Message}");
                return new List<Reservation>();
            }
        }

        public void InvalidateReservationsCache() => InvalidateCache(ReservationsCacheKey);
        #endregion

        #region Payroll by Reservation
        public async Task<List<Payroll>> GetPayrollsByReservationAsync(long reservationId)
        {
            string cacheKey = $"Payrolls_Reservation_{reservationId}";

            if (TryGetCache(cacheKey, out List<Payroll>? cached) && cached != null)
                return cached;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Payroll>()
                    .Select("*, workers(*)")
                    .Filter("reservation_id", Operator.Equals, reservationId)
                    .Get();

                var payrolls = response.Models?.ToList() ?? new List<Payroll>();
                SetCache(cacheKey, payrolls);
                return payrolls;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error fetching payrolls by reservation: {ex.Message}");
                return new List<Payroll>();
            }
        }
        #endregion

        #region Payroll by Worker + Cutoff
        public async Task<List<Payroll>> GetPayrollsByWorkerAsync(long workerId, DateTime startDate, DateTime endDate)
        {
            string cacheKey = $"Payrolls_Worker_{workerId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

            if (TryGetCache(cacheKey, out List<Payroll>? cached) && cached != null)
                return cached;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Payroll>()
                    .Select("*, reservations(*)")
                    .Filter("worker_id", Operator.Equals, workerId)
                    .Filter("paid_status", Operator.Equals, "Paid")
                    .Get();

                var payrolls = response.Models?
                    .Where(p => p.Reservation != null &&
                                p.Reservation.EventDate >= startDate &&
                                p.Reservation.EventDate <= endDate)
                    .ToList() ?? new List<Payroll>();

                SetCache(cacheKey, payrolls);
                return payrolls;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error fetching payrolls by worker: {ex.Message}");
                return new List<Payroll>();
            }
        }
        #endregion

        #region Payroll Pagination
        public async Task<(List<Payroll> Records, int TotalCount)> GetPayrollPageAsync(int pageNumber)
        {
            string cacheKey = $"Payrolls_Page_{pageNumber}_Size_{PageSize}";

            if (TryGetCache(cacheKey, out (List<Payroll> Records, int TotalCount) cached) && cached.Records != null)
                return cached;

            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client.From<Payroll>()
                    .Select("*, workers(*), reservations(*)")
                    .Order(x => x.PaidDate, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                var totalCount = await client.From<Payroll>()
                    .Select("id")
                    .Count(CountType.Exact);

                var result = (response.Models?.ToList() ?? new List<Payroll>(), totalCount);
                SetCache(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error fetching payroll page: {ex.Message}");
                return (new List<Payroll>(), 0);
            }
        }

        #endregion

        #region Mark As Paid
        public async Task<bool> MarkAsPaidAsync(Payroll payroll)
        {
            if (payroll == null) return false;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Payroll>()
                    .Where(p => p.Id == payroll.Id)
                    .Set(p => p.PaidStatus, "Paid")
                    .Set(p => p.PaidDate, DateTime.Now)
                    .Update();

                if (response.Models != null && response.Models.Any())
                {
                    InvalidatePayrollCaches(payroll);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error marking payroll as paid: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Delete Payroll
        public async Task<bool> DeletePayrollAsync(Payroll payroll)
        {
            if (payroll == null) return false;

            try
            {
                var client = await GetClientAsync();
                await client.From<Payroll>().Where(p => p.Id == payroll.Id).Delete();

                InvalidatePayrollCaches(payroll);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error deleting payroll: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Cache Invalidation
        public void InvalidateAllPayrollCaches()
        {
            InvalidateCacheByPrefix("Payrolls_Page_");
            InvalidateCacheByPrefix("Payrolls_Reservation_");
            InvalidateCacheByPrefix("Payrolls_Worker_");
        }

        private void InvalidatePayrollCaches(Payroll payroll)
        {
            InvalidateCache($"Payrolls_Reservation_{payroll.ReservationId}");
            InvalidateCacheByPrefix($"Payrolls_Worker_{payroll.WorkerId}_");
            InvalidateCacheByPrefix("Payrolls_Page_");
        }
        #endregion
    }
    #endregion
}
