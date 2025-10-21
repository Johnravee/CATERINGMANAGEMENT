

using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Mailer;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services.Shared;


namespace CATERINGMANAGEMENT.Services.Data
{
    /// <summary>
    /// Handles assigning workers to reservations, retrieving completed reservations and all workers,
    /// sending notification emails, with caching support via BaseCachedService.
    /// </summary>
    public class AssignWorkerService : BaseCachedService
    {


        private const string ReservationCacheKey = "CompletedReservations";
        private const string WorkerCacheKey = "AllWorkers";

        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        public async Task<List<Reservation>> GetCompletedReservationsAsync()
        {
            if (TryGetCache<List<Reservation>>(ReservationCacheKey, out var cachedReservations))
                return cachedReservations!;

            try
            {
                var client = await GetClientAsync();

                var response = await client
                    .From<Reservation>()
                    .Select("*, package:package_id(*)")
                    .Where(r => r.Status == "completed")
                    .Get();

                var result = response.Models ?? new List<Reservation>();
                SetCache(ReservationCacheKey, result);

                AppLogger.Info($"Loaded {result.Count} completed reservations from database.");
                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load completed reservations.");
                return new List<Reservation>();
            }
        }

        public async Task<List<Worker>> GetAllWorkersAsync()
        {
            if (TryGetCache<List<Worker>>(WorkerCacheKey, out var cachedWorkers))
                return cachedWorkers!;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Worker>().Get();

                var result = response.Models ?? new List<Worker>();
                SetCache(WorkerCacheKey, result);

                AppLogger.Info($"Loaded {result.Count} workers from database.");
                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load workers.");
                return new List<Worker>();
            }
        }

        public async Task<bool> AssignWorkerAsync(Worker worker, Reservation reservation)
        {
            try
            {
                var client = await GetClientAsync();

                var parameters = new
                {
                    p_worker_id = worker.Id,
                    p_reservation_id = (int)reservation.Id,
                    p_paid_status = "Unpaid",
                    p_paid_date = (DateTime?)null
                };

                var rpcResponse = await client.Rpc("insert_payroll_and_scheduling", parameters);

                bool success = rpcResponse.ResponseMessage != null &&
                               rpcResponse.ResponseMessage.IsSuccessStatusCode;

                if (success)
                {
                    AppLogger.Success($"Worker {worker.Name} assigned to reservation {reservation.Id}.");
                    InvalidateCache(ReservationCacheKey, WorkerCacheKey); // Invalidate related caches
                }
                else
                {
                    string error = rpcResponse.ResponseMessage != null
                        ? await rpcResponse.ResponseMessage.Content.ReadAsStringAsync()
                        : "No response received.";
                    AppLogger.Error($"Failed to assign worker {worker.Name}: {error}", showToUser: false);
                }

                return success;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Exception assigning worker {worker.Name} to reservation {reservation.Id}");
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(Worker worker, Reservation reservation)
        {
            try
            {
                var mailer = new AssignWorkerMailer(new EmailService());

                bool success = await mailer.SendWorkerScheduleEmailAsync(
                    worker.Email ?? "",
                    worker.Name ?? "Staff",
                    worker.Role ?? "Staff",
                    reservation.Package?.Name ?? "Event",
                    reservation.EventDate.ToString("MMMM dd, yyyy"),
                    reservation.Venue ?? "Venue"
                );

                if (success)
                {
                    AppLogger.Success($"Email sent to {worker.Email}");
                }
                else
                {
                    AppLogger.Error($"Failed to send email to {worker.Email}", showToUser: false);
                }

                return success;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Email send failed for {worker.Email}", showToUser: false);
                return false;
            }
        }
    }
}
