/*
 * FILE: ThemeMotifService.cs
 * PURPOSE: Handles all Supabase operations for Theme & Motif entity — 
 *          CRUD, retrieval, and export preparation.
 * RESPONSIBILITY:
 *   - Abstracts data access logic away from ViewModels.
 *   - Provides reusable async methods for ThemeMotif operations.
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.Services.Data
{
    public class ThemeMotifService
    {
        #region Load Operations

        public static async Task<List<ThemeMotif>> GetPaginatedAsync(int from, int to)
        {
            try
            {
                AppLogger.Info($"Fetching ThemeMotifs: Range {from}-{to}");
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<ThemeMotif>()
                    .Select("*, packages(*)")
                    .Range(from, to)
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                AppLogger.Success($"Fetched {response.Models?.Count ?? 0} ThemeMotifs.");
                return response.Models;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching paginated ThemeMotifs.");
                throw;
            }
        }

        public static async Task<int> GetTotalCountAsync()
        {
            try
            {
                AppLogger.Info("Counting total ThemeMotifs...");
                var client = await SupabaseService.GetClientAsync();
                int count = await client.From<ThemeMotif>().Count(CountType.Exact);
                AppLogger.Success($"ThemeMotif total count: {count}");
                return count;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error counting ThemeMotifs.");
                throw;
            }
        }

        public static async Task<List<ThemeMotif>> SearchAsync(string query)
        {
            try
            {
                AppLogger.Info($"Searching ThemeMotifs for '{query}'");
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<ThemeMotif>()
                    .Select("*, packages(*)")
                    .Filter(x => x.Name, Operator.ILike, $"%{query}%")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                AppLogger.Success($"Found {response.Models?.Count ?? 0} matching results for '{query}'.");
                return response.Models;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Error searching ThemeMotifs with query '{query}'.");
                throw;
            }
        }

        public static async Task<List<Package>> GetPackagesAsync()
        {
            try
            {
                AppLogger.Info("Fetching available packages...");
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Package>()
                    .Select("*")
                    .Order(p => p.CreatedAt, Ordering.Descending)
                    .Get();

                AppLogger.Success($"Loaded {response.Models?.Count ?? 0} packages.");
                return response.Models;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error fetching packages.");
                throw;
            }
        }

        #endregion

        #region CRUD Operations

        public static async Task AddAsync(NewThemeMotif motif)
        {
            try
            {
                AppLogger.Info($"Adding new ThemeMotif: {motif.Name}");
                var client = await SupabaseService.GetClientAsync();
                await client.From<NewThemeMotif>().Insert(motif);
                AppLogger.Success($"ThemeMotif '{motif.Name}' added successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Error adding ThemeMotif '{motif.Name}'.");
                throw;
            }
        }

        public static async Task UpdateAsync(NewThemeMotif motif)
        {
            try
            {
                AppLogger.Info($"Updating ThemeMotif ID: {motif.Id} - Name: {motif.Name}");
                var client = await SupabaseService.GetClientAsync();
                await client.From<NewThemeMotif>().Where(x => x.Id == motif.Id).Update(motif);
                AppLogger.Success($"ThemeMotif '{motif.Name}' updated successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Error updating ThemeMotif '{motif.Name}'.");
                throw;
            }
        }

        public static async Task DeleteAsync(long id, string name)
        {
            try
            {
                AppLogger.Info($"Deleting ThemeMotif '{name}' (ID: {id})...");
                var client = await SupabaseService.GetClientAsync();
                await client.From<ThemeMotif>().Where(x => x.Id == id).Delete();
                AppLogger.Success($"ThemeMotif '{name}' deleted successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Error deleting ThemeMotif '{name}'.");
                throw;
            }
        }

        #endregion
    }
}
