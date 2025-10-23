/*
 * FILE: WorkerService.cs
 * PURPOSE: Handles all Supabase operations for Worker entity:
 *          CRUD, pagination, search, and export (PDF/CSV) with smart caching.
 * 
 * RESPONSIBILITIES:
 *  - Retrieve paginated and searchable worker data
 *  - Insert, update, delete worker records
 *  - Cache list and export data for performance
 *  - Invalidate caches automatically on changes
 */

using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class WorkerService : BaseCachedService
    {
        #region Constants
        private const int PageSize = 20;
        private const string WorkersCacheKey = "Workers_List";
        private const string WorkerPagePrefix = "Workers_Page_";
        private const string WorkerSearchPrefix = "Workers_Search_";
        private const string ExportCacheKey = "Workers_ExportList";
        #endregion

        #region Supabase Client
        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();
        #endregion

        #region Get Paginated Workers (with cache)
        public async Task<(List<Worker> Workers, int TotalCount)> GetWorkersPageAsync(int pageNumber)
        {
            string cacheKey = $"{WorkerPagePrefix}{pageNumber}_Size_{PageSize}";
            if (TryGetCache(cacheKey, out (List<Worker> Workers, int TotalCount) cached) && cached.Workers != null)
                return cached;

            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client.From<Worker>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                var totalCount = await client.From<Worker>()
                    .Select("id")
                    .Count(CountType.Exact);

                var result = (response.Models ?? new List<Worker>(), totalCount);
                SetCache(cacheKey, result);

                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Error fetching workers page");
                return (new List<Worker>(), 0);
            }
        }
        #endregion

        #region Get All Workers (for dropdowns or caching)
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
                AppLogger.Error(ex, "❌ Error fetching all workers");
                return new List<Worker>();
            }
        }
        #endregion

        #region Search Workers (with caching)
        public async Task<List<Worker>?> SearchWorkersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Worker>();

            string cacheKey = $"{WorkerSearchPrefix}{query.ToLower()}";
            if (TryGetCache(cacheKey, out List<Worker>? cached) && cached != null)
                return cached;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Worker>()
                    .Filter(x => x.Name, Operator.ILike, $"%{query}%")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var results = response.Models ?? new List<Worker>();
                SetCache(cacheKey, results);
                return results;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Error searching workers");
                return new List<Worker>();
            }
        }
        #endregion

        #region Insert Worker
        public async Task<Worker?> InsertWorkerAsync(Worker worker)
        {
            if (worker == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Worker>().Insert(worker);

                if (response.Models?.Count > 0)
                {
                    InvalidateWorkerCaches();
                    return response.Models[0];
                }

                return null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Error inserting worker");
                return null;
            }
        }
        #endregion

        #region Update Worker
        public async Task<Worker?> UpdateWorkerAsync(Worker worker)
        {
            if (worker == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Worker>()
                    .Where(w => w.Id == worker.Id)
                    .Update(worker);

                if (response.Models?.Count > 0)
                {
                    InvalidateWorkerCaches();
                    return response.Models[0];
                }

                return null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Error updating worker");
                return null;
            }
        }
        #endregion

        #region Delete Worker
        public async Task<bool> DeleteWorkerAsync(long id)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Worker>().Where(w => w.Id == id).Delete();

                InvalidateWorkerCaches();
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Error deleting worker");
                return false;
            }
        }
        #endregion

        #region Export (PDF / CSV)
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
                    AppLogger.Info("No workers to export to PDF");
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    workers,
                    "Workers",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt",
                    "HireDate"
                );

                AppLogger.Success("✅ Exported workers to PDF");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Error exporting workers to PDF");
            }
        }

        public async Task ExportWorkersToCsvAsync()
        {
            try
            {
                var workers = await GetExportListAsync();
                if (workers.Count == 0)
                {
                    AppLogger.Info("⚠️ No workers to export to CSV");
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    workers,
                    "Workers",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt",
                    "HireDate"
                );

                AppLogger.Success("✅ Exported workers to CSV");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "❌ Error exporting workers to CSV");
            }
        }
        #endregion

        #region Cache Invalidation
        /// <summary>
        /// Invalidates all worker-related caches (pages, searches, lists, exports).
        /// </summary>
        public void InvalidateWorkerCaches()
        {
            InvalidateCache(WorkersCacheKey);
            InvalidateCache(ExportCacheKey);
            InvalidateCacheByPrefix(WorkerPagePrefix);
            InvalidateCacheByPrefix(WorkerSearchPrefix);
        }
        #endregion
    }
}
