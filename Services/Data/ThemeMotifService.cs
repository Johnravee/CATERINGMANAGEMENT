using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class ThemeMotifService : BaseCachedService
    {
        private const int PageSize = 10;
        private const string ExportCacheKey = "ThemeMotif_ExportList";
        private const string PageCachePrefix = "ThemeMotif_Page_";

        private async Task<Supabase.Client> GetClientAsync() => await SupabaseService.GetClientAsync();

        // Paginated list with caching
        public async Task<(List<ThemeMotif> Items, int TotalCount)> GetThemeMotifPageAsync(int pageNumber)
        {
            string cacheKey = $"{PageCachePrefix}{pageNumber}";

            // Return cached result if available
            if (TryGetCache(cacheKey, out (List<ThemeMotif>, int) cached))
            {
                return cached;
            }

            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                // Fetch paginated ThemeMotif including related Package
                var response = await client.From<ThemeMotif>()
                    .Select("id, name, package_id, created_at, packages(*)") 
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                // Get total count
                int totalCount = await client.From<ThemeMotif>().Select("id").Count(CountType.Exact);

                var items = response.Models ?? new List<ThemeMotif>();

                // Cache the result
                var result = (items, totalCount);
                SetCache(cacheKey, result);

                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching paginated theme & motif list");
                return (new List<ThemeMotif>(), 0);
            }
        }

        // Search
        public async Task<List<ThemeMotif>> SearchThemeMotifsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<ThemeMotif>();

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<ThemeMotif>()
                    .Filter(x => x.Name, Operator.ILike, $"%{query.Trim()}%")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                return response.Models ?? new List<ThemeMotif>();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error searching ThemeMotifs");
                return new List<ThemeMotif>();
            }
        }

        // Insert
        public async Task<NewThemeMotif?> InsertThemeMotifAsync(NewThemeMotif motif)
        {
            if (motif == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<NewThemeMotif>().Insert(motif);
                InvalidateAllCaches();

                return response.Models?.Count > 0 ? response.Models[0] : null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error inserting ThemeMotif");
                return null;
            }
        }


        // Update
        public async Task<NewThemeMotif?> UpdateThemeMotifAsync(NewThemeMotif motif)
        {
            if (motif == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<NewThemeMotif>()
                    .Where(m => m.Id == motif.Id)
                    .Update(motif);

                InvalidateAllCaches();

                return response.Models?.Count > 0 ? response.Models[0] : null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error updating ThemeMotif");
                return null;
            }
        }

        // Delete
        public async Task<bool> DeleteThemeMotifAsync(long id)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<ThemeMotif>().Where(m => m.Id == id).Delete();

                InvalidateAllCaches();
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error deleting ThemeMotif");
                throw;
            }
        }

        // Export list cache
        private async Task<List<ThemeMotif>> GetExportListAsync()
        {
            if (TryGetCache(ExportCacheKey, out List<ThemeMotif>? cached) && cached != null)
            {
                AppLogger.Info("Loaded ThemeMotifs for export from cache");
                return cached;
            }

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<ThemeMotif>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var motifs = response.Models ?? new List<ThemeMotif>();
                SetCache(ExportCacheKey, motifs);
                return motifs;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching ThemeMotifs for export");
                return new List<ThemeMotif>();
            }
        }

        public async Task ExportThemeMotifsToPdfAsync()
        {
            try
            {
                var motifs = await GetExportListAsync();
                if (motifs.Count == 0)
                {
                    AppLogger.Info("No Theme & Motif found to export to PDF");
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    motifs,
                    "Theme & Motif List",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt"
                );

                AppLogger.Success("Exported ThemeMotifs to PDF");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting ThemeMotifs to PDF");
            }
        }

        public async Task ExportThemeMotifsToCsvAsync()
        {
            try
            {
                var motifs = await GetExportListAsync();
                if (motifs.Count == 0)
                {
                    AppLogger.Info("No Theme & Motif found to export to CSV");
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    motifs,
                    "Theme & Motif List",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt"
                );

                AppLogger.Success("Exported ThemeMotifs to CSV");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting ThemeMotifs to CSV");
            }
        }

        public async Task<int> GetTotalThemeMotifCountAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                int count = await client.From<ThemeMotif>().Count(CountType.Exact);
                return count;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error getting total count of ThemeMotifs");
                return 0;
            }
        }


        public async Task<ThemeMotif?> GetThemeMotifByIdAsync(long id)
        {
            try
            {
                var client = await GetClientAsync(); 

                var response = await client.From<ThemeMotif>()
                    .Select("id, name, package_id, created_at, packages(*)") 
                    .Where(p => p.Id == id)
                    .Single();

                return response;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Error fetching ThemeMotif ID {id}");
                return null;
            }
        }


        // Cache invalidation
        public void InvalidateAllCaches()
        {
            InvalidateCache(ExportCacheKey);
            InvalidateCacheByPrefix(PageCachePrefix);
        }

 
    }
}
