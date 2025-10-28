using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class MenuService : BaseCachedService
    {
        private const int PageSize = 10;
        private const string ExportCacheKey = "Menu_ExportList";
        private const string PageCachePrefix = "Menu_Page_";



        private static async Task<Supabase.Client> GetClientAsync()
        {
            var client = await SupabaseService.GetClientAsync();

            if (client == null)
                throw new InvalidOperationException("Supabase client is not initialized.");

            return client;
        }


        // Paginated list with caching
        public async Task<(List<MenuOption> Items, int TotalCount)> GetMenuPageAsync(int pageNumber)
        {
            string cacheKey = $"{PageCachePrefix}{pageNumber}";

            if (TryGetCache(cacheKey, out (List<MenuOption>, int) cached))
            {
                return cached;
            }

            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client.From<MenuOption>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                int totalCount = await client.From<MenuOption>().Select("id").Count(CountType.Exact);

                var items = response.Models ?? new List<MenuOption>();
                var result = (items, totalCount);

                SetCache(cacheKey, result);

                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching paginated menu");
                return (new List<MenuOption>(), 0);
            }
        }

        // Search without caching because results may vary widely
        public static async Task<List<MenuOption>> SearchMenusAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<MenuOption>();

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<MenuOption>()
                    .Filter(static x => x.Name , Operator.ILike, $"%{query.Trim()}%")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                return response.Models ?? new List<MenuOption>();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error searching menus");
                return new List<MenuOption>();
            }
        }

        // Insert - invalidate caches after insert
        public async Task<MenuOption?> InsertMenuAsync(MenuOption menu)
        {
            if (menu == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<MenuOption>().Insert(menu);
                InvalidateAllCaches();

                return response.Models?.Count > 0 ? response.Models[0] : null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error inserting menu");
                return null;
            }
        }

        // Update - invalidate caches after update
        public async Task<MenuOption?> UpdateMenuAsync(MenuOption menu)
        {
            if (menu == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<MenuOption>()
                    .Where(m => m.Id == menu.Id)
                    .Update(menu);

                InvalidateAllCaches();

                return response.Models?.Count > 0 ? response.Models[0] : null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error updating menu");
                return null;
            }
        }

        // Delete - invalidate caches after delete
        public async Task<bool> DeleteMenuAsync(long id)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<MenuOption>().Where(m => m.Id == id).Delete();

                InvalidateAllCaches();

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error Deleting Menu");
                
               throw;
            }
        }

        // Export list cached
        private async Task<List<MenuOption>> GetExportListAsync()
        {
            if (TryGetCache(ExportCacheKey, out List<MenuOption>? cached) && cached != null)
            {
                AppLogger.Info("Loaded menus for export from cache");
                return cached;
            }

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<MenuOption>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var menus = response.Models ?? new List<MenuOption>();
                SetCache(ExportCacheKey, menus);
                return menus;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching menus for export");
                return new List<MenuOption>();
            }
        }

        public async Task ExportMenusToPdfAsync()
        {
            try
            {
                var menus = await GetExportListAsync();
                if (menus.Count == 0)
                {
                    AppLogger.Info("No menus found to export to PDF");
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    menus,
                    "Menu Options",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt"
                );

                AppLogger.Success("Exported menus to PDF");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting menus to PDF");
            }
        }

        public async Task ExportMenusToCsvAsync()
        {
            try
            {
                var menus = await GetExportListAsync();
                if (menus.Count == 0)
                {
                    AppLogger.Info("No menus found to export to CSV");
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    menus,
                    "Menu Options",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt"
                );

                AppLogger.Success("Exported menus to CSV");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting menus to CSV");
            }
        }

        public async Task<int> GetTotalMenuOptionsCountAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                int count = await client.From<MenuOption>().Count(CountType.Exact);
                return count;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error getting total count of menu options");
                return 0;
            }
        }


        // Invalidate all related caches
        public void InvalidateAllCaches()
        {
            InvalidateCache(ExportCacheKey);
            InvalidateCacheByPrefix(PageCachePrefix);
        }
    }
}
