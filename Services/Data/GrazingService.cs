using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using CATERINGMANAGEMENT.View.Pages;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class GrazingService : BaseCachedService
    {
        private const int PageSize = 10;
        private const string ExportCacheKey = "Grazing_ExportList";
        private const string PageCachePrefix = "Grazing_Page_";

        private static async Task<Supabase.Client> GetClientAsync()
        {
            var client = await SupabaseService.GetClientAsync();
            if (client == null)
                throw new InvalidOperationException("Supabase client is not initialized.");
            return client;
        }

        // Paginated list with caching
        public async Task<(List<GrazingTable> Items, int TotalCount)> GetGrazingPageAsync(int pageNumber)
        {
            string cacheKey = $"{PageCachePrefix}{pageNumber}";

            if (TryGetCache(cacheKey, out (List<GrazingTable>, int) cached))
                return cached;

            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client.From<GrazingTable>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                int totalCount = await client.From<GrazingTable>().Select("id").Count(CountType.Exact);
                var items = response.Models ?? new List<GrazingTable>();
                var result = (items, totalCount);

                SetCache(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching paginated grazing options");
                return (new List<GrazingTable>(), 0);
            }
        }

        // Search
        public static async Task<List<GrazingTable>> SearchGrazingAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<GrazingTable>();

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<GrazingTable>()
                    .Filter(x => x.Name, Operator.ILike, $"%{query.Trim()}%")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                return response.Models ?? new List<GrazingTable>();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error searching grazing options");
                return new List<GrazingTable>();
            }
        }

        // Insert
        public async Task<GrazingTable?> InsertGrazingAsync(GrazingTable grazing)
        {
            if (grazing == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<GrazingTable>().Insert(grazing);
                InvalidateAllCaches();

                return response.Models?.Count > 0 ? response.Models[0] : null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error inserting grazing option");
                return null;
            }
        }

        // Update
        public async Task<GrazingTable?> UpdateGrazingAsync(GrazingTable grazing)
        {
            if (grazing == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<GrazingTable>()
                    .Where(g => g.Id == grazing.Id)
                    .Update(grazing);

                InvalidateAllCaches();
                return response.Models?.Count > 0 ? response.Models[0] : null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error updating grazing option");
                return null;
            }
        }

        // Delete
        public async Task<bool> DeleteGrazingAsync(long id)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<GrazingTable>().Where(g => g.Id == id).Delete();
                InvalidateAllCaches();
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error deleting package");

                // Detect common foreign key / constraint violation messages from PostgreSQL/Supabase
                var message = ex.Message ?? string.Empty;
                if (message.Contains("violates foreign key constraint", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("foreign key constraint", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("update or delete on table", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("constraint", StringComparison.OrdinalIgnoreCase) && message.Contains("references", StringComparison.OrdinalIgnoreCase))
                {
                    // Provide a clearer message for the UI
                    throw new InvalidOperationException("This package cannot be deleted because it is currently in use. Please remove any related items before trying again.", ex);

                }

                // For other errors, rethrow to allow the caller to handle/display
                throw;
            }
        }

        // Export list (cached)
        private async Task<List<GrazingTable>> GetExportListAsync()
        {
            if (TryGetCache(ExportCacheKey, out List<GrazingTable>? cached) && cached != null)
            {
                AppLogger.Info("Loaded grazing options for export from cache");
                return cached;
            }

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<GrazingTable>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var grazings = response.Models ?? new List<GrazingTable>();
                SetCache(ExportCacheKey, grazings);
                return grazings;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching grazing options for export");
                return new List<GrazingTable>();
            }
        }

        public async Task ExportGrazingToPdfAsync()
        {
            try
            {
                var grazings = await GetExportListAsync();
                if (grazings.Count == 0)
                {
                    AppLogger.Info("No grazing options found to export to PDF");
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    grazings,
                    "Grazing Options",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey"
                );

                AppLogger.Success("Exported grazing options to PDF");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting grazing options to PDF");
            }
        }

        public async Task ExportGrazingToCsvAsync()
        {
            try
            {
                var grazings = await GetExportListAsync();
                if (grazings.Count == 0)
                {
                    AppLogger.Info("No grazing options found to export to CSV");
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    grazings,
                    "Grazing Options",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey"
                );

                AppLogger.Success("Exported grazing options to CSV");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting grazing options to CSV");
            }
        }

        public async Task<int> GetTotalGrazingOptionsCountAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                int count = await client.From<GrazingTable>().Count(CountType.Exact);
                return count;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error getting total count of grazing options");
                return 0;
            }
        }

        // Invalidate caches
        public void InvalidateAllCaches()
        {
            InvalidateCache(ExportCacheKey);
            InvalidateCacheByPrefix(PageCachePrefix);
        }
    }
}
