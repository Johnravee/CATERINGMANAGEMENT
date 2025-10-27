/*
 * FILE: FeedbackViewModel.cs
 * PURPOSE: Handles loading, pagination, search (with debounce), deleting, and realtime sync of Feedback data.
 * 
 * RESPONSIBILITIES:
 *  - Load feedbacks with pagination
 *  - Search feedbacks with debounce
 *  - Delete feedback entries
 *  - Subscribe to Supabase Realtime updates (insert, update, delete)
 *  - Display messages for user interaction
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace CATERINGMANAGEMENT.ViewModels.FeedbackVM
{
    public class FeedbackViewModel : BaseViewModel
    {
        #region Constants
        private const int PageSize = 10;
        #endregion

        #region Services
        private readonly FeedbackService _feedbackService = new();
        #endregion

        #region Fields
        private CancellationTokenSource? _searchDebounceToken;
        #endregion

        #region Collections
        public ObservableCollection<Feedback> Items { get; } = new();
        #endregion

        #region UI State
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _ = ApplySearchDebouncedAsync();
            }
        }
        #endregion

        #region Commands
        public ICommand DeleteFeedbackCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        #endregion

        #region Constructor
        public FeedbackViewModel()
        {
            DeleteFeedbackCommand = new RelayCommand<Feedback>(async f => await DeleteFeedbackAsync(f));
            NextPageCommand = new RelayCommand(async () => await LoadPageAsync(CurrentPage + 1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await LoadPageAsync(CurrentPage - 1), () => CurrentPage > 1);

            _ = LoadPageAsync(1);

            // ✅ Start realtime subscription
            _ = Task.Run(SubscribeToRealtime);
        }
        #endregion

        #region Load Feedbacks
        public async Task LoadPageAsync(int pageNumber)
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var (feedbacks, totalCount, totalPages) = await _feedbackService.GetFeedbackPageAsync(pageNumber);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    foreach (var f in feedbacks)
                        Items.Add(f);
                });

                TotalCount = totalCount;
                TotalPages = totalPages;
                CurrentPage = Math.Max(1, Math.Min(pageNumber, totalPages == 0 ? 1 : totalPages));
            }
            catch (Exception ex)
            {
                ShowMessage($"Error loading feedbacks:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex, "Error loading feedback page");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Search (with debounce)
        private async Task ApplySearchDebouncedAsync()
        {
            _searchDebounceToken?.Cancel();
            var cts = new CancellationTokenSource();
            _searchDebounceToken = cts;

            try
            {
                await Task.Delay(400, cts.Token); // debounce delay
                await ApplySearchAsync();
            }
            catch (TaskCanceledException)
            {
                // Ignore cancelled debounce task
            }
        }

        private async Task ApplySearchAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadPageAsync(1);
                    return;
                }

                IsLoading = true;

                var results = await _feedbackService.SearchFeedbacksAsync(SearchText);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    foreach (var f in results)
                        Items.Add(f);
                });

                TotalPages = 1;
                CurrentPage = 1;
                TotalCount = Items.Count;
            }
            catch (Exception ex)
            {
                ShowMessage($"Search failed:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex, "Error searching feedbacks");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Delete Feedback
        private async Task DeleteFeedbackAsync(Feedback feedback)
        {
            if (feedback == null) return;

            var confirm = MessageBox.Show(
                $"Delete feedback from {feedback.Name}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                bool success = await FeedbackService.DeleteFeedbackAsync(feedback);

                if (success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Items.Remove(feedback);
                        TotalCount--;
                    });

                    ShowMessage("Feedback deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ShowMessage("Delete failed. The record may not exist or could not be removed.",
                                "Delete Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }


                ShowMessage("Feedback deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"Delete failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex, "Error deleting feedback");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Realtime Sync
        private async Task SubscribeToRealtime()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                // Subscribe to "feedback" table in "public" schema
                var channel = client.Realtime.Channel("realtime", "public", "feedbacks");

                // Log all events
                channel.AddPostgresChangeHandler(ListenType.All, (sender, change) =>
                {
                    Debug.WriteLine($"[Realtime] Event: {change.Event}");
                    Debug.WriteLine($"Payload: {change.Payload}");
                });

                // INSERT handler
                channel.AddPostgresChangeHandler(ListenType.Inserts, async (sender, change) =>
                {
                    var inserted = change.Model<Feedback>();
                    if (inserted == null)
                    {
                        Debug.WriteLine("[Realtime Insert] Failed to deserialize feedback.");
                        return;
                    }

                    if (inserted.Profile == null && inserted.ProfileId.HasValue)
                    {
                        var profile = await _feedbackService.GetProfileByIdAsync(inserted.ProfileId.Value);
                        inserted.Profile = profile;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(f => f.Id == inserted.Id);
                        if (existing == null)
                        {
                            Items.Insert(0, inserted);
                            TotalCount++;
                            Debug.WriteLine($"Realtime Insert: Added feedback ID {inserted.Id}");
                        }
                    });
                });

                var result = await channel.Subscribe();
                AppLogger.Success($"✅ Subscribed to realtime feedback updates: {result}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error subscribing to realtime feedback updates");
            }
        }
        #endregion
    }
}
