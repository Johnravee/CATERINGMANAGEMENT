/*
 * FILE: WorkerService.cs
 * PURPOSE: Handles all Supabase operations for Worker entity:
 *          CRUD, pagination, search, exports (PDF/CSV) with selective caching.
 */

using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class WorkerService : BaseCachedService
    {
        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        // Paginated list – frequent changes, do NOT cache
        public async Task<(List<Worker> Workers, int TotalCount)> GetWorkersPageAsync(int pageNumber, int pageSize)
        {
            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * pageSize;
                int to = from + pageSize - 1;

                var response = await client.From<Worker>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                var totalCount = await client.From<Worker>()
                    .Select("id")
                    .Count(CountType.Exact);

                return (response.Models ?? new List<Worker>(), totalCount);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching workers page");
                return (new List<Worker>(), 0);
            }
        }

        // Search – optionally cached if same query is used frequently
        public async Task<List<Worker>> SearchWorkersAsync(string query)
        {
            string cacheKey = $"Worker_Search_{query}";
            if (TryGetCache(cacheKey, out List<Worker>? cached) && cached != null)
            {
                AppLogger.Info($"Loaded search '{query}' from cache");
                return cached;
            }

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Worker>()
                    .Filter(x => x.Name, Operator.ILike, $"%{query}%")
                    .Get();

                var result = response.Models ?? new List<Worker>();
                SetCache(cacheKey, result); // cache the result
                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error searching workers");
                return new List<Worker>();
            }
        }

        // Insert – frequent change, no cache
        public async Task<Worker?> InsertWorkerAsync(Worker worker)
        {
            if (worker == null) return null;
            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Worker>().Insert(worker);
                return response.Models?.Count > 0 ? response.Models[0] : null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error inserting worker");
                return null;
            }
        }

        // Update – frequent change, no cache
        public async Task<Worker?> UpdateWorkerAsync(Worker worker)
        {
            if (worker == null) return null;
            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Worker>()
                    .Where(w => w.Id == worker.Id)
                    .Update(worker);

                return response.Models?.Count > 0 ? response.Models[0] : null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error updating worker");
                return null;
            }
        }

        // Delete – frequent change, no cache
        public async Task<bool> DeleteWorkerAsync(long id)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Worker>().Where(w => w.Id == id).Delete();
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error deleting worker");
                return false;
            }
        }

        // Exports – cache the list to avoid repeated fetch
        private const string ExportCacheKey = "Worker_ExportList";

        private async Task<List<Worker>> GetExportListAsync()
        {
            if (TryGetCache(ExportCacheKey, out List<Worker>? cached) && cached != null)
            {
                AppLogger.Info("Loaded workers for export from cache");
                return cached;
            }

            var client = await GetClientAsync();
            var response = await client.From<Worker>()
                .Order(x => x.CreatedAt, Ordering.Descending)
                .Get();

            var workers = response.Models ?? new List<Worker>();
            SetCache(ExportCacheKey, workers);
            return workers;
        }

        public async Task ExportWorkersToPdfAsync()
        {
            try
            {
                var workers = await GetExportListAsync();
                if (workers.Count == 0)
                {
                    AppLogger.Info("No workers found to export to PDF");
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    workers,
                    "Workers",
                    "Id",
                    "Name",
                    "Role",
                    "HireDate",
                    "CreatedAt"
                );

                AppLogger.Success("Exported workers to PDF");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting workers to PDF");
            }
        }

        public async Task ExportWorkersToCsvAsync()
        {
            try
            {
                var workers = await GetExportListAsync();
                if (workers.Count == 0)
                {
                    AppLogger.Info("No workers found to export to CSV");
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    workers,
                    "Workers",
                    "Id",
                    "Name",
                    "Role",
                    "HireDate",
                    "CreatedAt"
                );

                AppLogger.Success("Exported workers to CSV");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting workers to CSV");
            }
        }

        // Invalidate cached export list if any insert/update/delete occurs
        public void InvalidateExportCache() => InvalidateCache(ExportCacheKey);
    }
}
