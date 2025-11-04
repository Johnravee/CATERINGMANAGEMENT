using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Shared;
using static Supabase.Postgrest.Constants;
using System;

namespace CATERINGMANAGEMENT.Services.Data
{
    internal class PackageService : BaseCachedService
    {
        private const int PageSize = 10;
        private const string ExportCacheKey = "Package_ExportList";
        private const string PageCachePrefix = "Package_Page_";
        private const string CountCacheKey = "Package_TotalCount";
        private const string PackageALlCacheKey = "Package_All";


        private static async Task<Supabase.Client> GetClientAsync()
        {
            var client = await SupabaseService.GetClientAsync();

            if (client == null)
                throw new InvalidOperationException("Supabase client is not initialized.");

            return client;
        }


        // Paginated list with caching
        public async Task<(List<Package> Items, int TotalCount)> GetPackagePageAsync(int pageNumber)
        {
            string cacheKey = $"{PageCachePrefix}{pageNumber}";

            if (TryGetCache(cacheKey, out (List<Package>, int) cached))
                return cached;

            try
            {
                var client = await GetClientAsync();
                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client.From<Package>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                int totalCount = await client.From<Package>().Select("id").Count(CountType.Exact);

                var items = response.Models ?? new List<Package>();
                var result = (items, totalCount);

                SetCache(cacheKey, result);

                return result;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching paginated packages");
                return (new List<Package>(), 0);
            }
        }

        // Search packages (no cache)
        public async Task<List<Package>> SearchPackagesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Package>();

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Package>()
                    .Filter(x => x.Name, Operator.ILike, $"%{query.Trim()}%")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                return response.Models ?? new List<Package>();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error searching packages");
                return new List<Package>();
            }
        }

        // Insert package
        public async Task<Package?> InsertPackageAsync(Package package)
        {
            if (package == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Package>().Insert(package);
                InvalidateAllCaches();

                return response.Models?.Count > 0 ? response.Models[0] : null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error inserting package");
                return null;
            }
        }

        // Update package
        public async Task<Package?> UpdatePackageAsync(Package package)
        {
            if (package == null) return null;

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Package>()
                    .Where(x => x.Id == package.Id)
                    .Update(package);

                InvalidateAllCaches();

                return response.Models?.Count > 0 ? response.Models[0] : null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error updating package");
                return null;
            }
        }

        // Delete package
        public async Task<bool> DeletePackageAsync(long  packageId)
        {
            try
            {
                var client = await GetClientAsync();
                // Let any exception bubble up so callers can display contextual messages
                await client.From<Package>().Where(x => x.Id == packageId).Delete();

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
                    throw new InvalidOperationException("This package can’t be deleted because it’s still referenced by other records. Please remove or update those references first.", ex);

                }

                // For other errors, rethrow to allow the caller to handle/display
                throw;
            }
        }

        // Get all packages for export (cached)
        private async Task<List<Package>> GetExportListAsync()
        {
            if (TryGetCache(ExportCacheKey, out List<Package>? cached) && cached != null)
            {
                AppLogger.Info("Loaded packages for export from cache");
                return cached;
            }

            try
            {
                var client = await GetClientAsync();
                var response = await client.From<Package>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var packages = response.Models ?? new List<Package>();
                SetCache(ExportCacheKey, packages);
                return packages;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching packages for export");
                return new List<Package>();
            }
        }

        // Export packages to PDF
        public async Task ExportPackagesToPdfAsync()
        {
            try
            {
                var packages = await GetExportListAsync();
                if (packages.Count == 0)
                {
                    AppLogger.Info("No packages found to export to PDF");
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    packages,
                    "Package List",
                    "Id", "BaseUrl", "RequestClientOptions", "TableName", "PrimaryKey", "UpdatedAt"
                );

                AppLogger.Success("Exported packages to PDF");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting packages to PDF");
                throw;
            }
        }

        public async Task ExportPackagesToCsvAsync()
        {
            try
            {
                var packages = await GetExportListAsync();
                if (packages.Count == 0)
                {
                    AppLogger.Info("No packages found to export to PDF");
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    packages,
                    "Package List",
                    "Id", "BaseUrl", "RequestClientOptions", "TableName", "PrimaryKey", "UpdatedAt"
                );

                AppLogger.Success("Exported packages to PDF");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting packages to PDF");
                throw;
            }
        }

        public async Task<int> GetPackageCountAsync()
        {
            if (TryGetCache(CountCacheKey, out int cachedCount))
                return cachedCount;

            try
            {
                var client = await GetClientAsync();
                int count = await client.From<Package>().Select("id").Count(CountType.Exact);
                SetCache(CountCacheKey, count);
                return count;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching package count");
                return 0;
            }
        }

        // Get all packages (cached)
        public async Task<List<Package>> GetAllPackagesAsync()
        {
           

            if (TryGetCache(PackageALlCacheKey, out List<Package>? cached) && cached != null)
            {
                AppLogger.Info("Loaded all packages from cache");
                return cached;
            }

            try
            {
                var client = await GetClientAsync();
                var response = await client
                    .From<Package>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var packages = response.Models ?? new List<Package>();
                SetCache(PackageALlCacheKey, packages);

                AppLogger.Success($"Fetched {packages.Count} total packages.");
                return packages;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching all packages");
                return new List<Package>();
            }
        }



        // Invalidate all caches
        public void InvalidateAllCaches()
        {
            InvalidateCache(ExportCacheKey);
            InvalidateCache(CountCacheKey);
            InvalidateCache(PackageALlCacheKey);
            InvalidateCacheByPrefix(PageCachePrefix);
        }
    }
}
