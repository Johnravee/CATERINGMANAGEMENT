/*
 * FILE: SchedulingViewModel.cs
 * PURPOSE: Main ViewModel for the Scheduling page.
 *          Handles loading schedules, completed reservations,
 *          pagination, searching (with debounce), and integration
 *          with AssignWorker and EditSchedule windows.
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.SchedulingVM
{
    public class SchedulingViewModel : BaseViewModel
    {
        private readonly SchedulingService _schedulingService = new();
        private CancellationTokenSource? _searchDebounceToken;

        // ✅ Data collections
        public ObservableCollection<GroupedScheduleView> Schedules { get; set; } = new();
        public ObservableCollection<Reservation> CompletedReservations { get; set; } = new();

        // ✅ UI state
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { _isRefreshing = value; OnPropertyChanged(); }
        }

        // ✅ Pagination
        private const int PageSize = 10;
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

        // ✅ Search
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

        // ✅ Commands
        public ICommand RefreshCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand OpenAssignWorkerCommand { get; }
        public ICommand OpenEditScheduleCommand { get; }

        public SchedulingViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await ReloadDataAsync());
            NextPageCommand = new RelayCommand(async () => await LoadSchedulesAsync(CurrentPage + 1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await LoadSchedulesAsync(CurrentPage - 1), () => CurrentPage > 1);
            OpenAssignWorkerCommand = new RelayCommand(OpenAssignWorkerWindow);
            OpenEditScheduleCommand = new RelayCommand<GroupedScheduleView>(OpenEditScheduleWindow);

            _ = ReloadDataAsync();
        }

        /// <summary>
        /// Reload both schedules and completed reservations.
        /// </summary>
        public async Task ReloadDataAsync()
        {
            if (IsRefreshing) return;
            IsRefreshing = true;

            try
            {
                await Task.WhenAll(LoadSchedulesAsync(CurrentPage), LoadCompletedReservationsAsync());
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ Error reloading data: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// Load paged schedules.
        /// </summary>
        public async Task LoadSchedulesAsync(int pageNumber)
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var (schedules, totalCount) = await _schedulingService.GetPagedGroupedSchedulesAsync(pageNumber);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Schedules = new ObservableCollection<GroupedScheduleView>(schedules ?? new List<GroupedScheduleView>());
                    OnPropertyChanged(nameof(Schedules));
                });

                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
                CurrentPage = Math.Max(1, Math.Min(pageNumber, TotalPages == 0 ? 1 : TotalPages));

                AppLogger.Info($"[FETCH] Loaded {Schedules.Count} schedules (Page {CurrentPage}/{TotalPages}).");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ Error loading schedules: {ex.Message}");
                ShowMessage($"Error loading schedules:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        /// <summary>
        /// Load completed reservations (ready for scheduling).
        /// </summary>
        public async Task LoadCompletedReservationsAsync()
        {
            try
            {
                var completed = await _schedulingService.GetCompletedReservationsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    CompletedReservations.Clear();
                    foreach (var r in completed)
                        CompletedReservations.Add(r);
                });

                AppLogger.Info($"[FETCH] Loaded {CompletedReservations.Count} completed reservations.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ Error loading completed reservations: {ex.Message}");
                ShowMessage($"Error loading completed reservations:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens AssignWorker window.
        /// </summary>
        private void OpenAssignWorkerWindow()
        {
            try
            {
                var assignWin = new AssignWorker(this);
                assignWin.ShowDialog();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ Failed to open AssignWorker window: {ex.Message}");
                ShowMessage($"Failed to open AssignWorker window:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEditScheduleWindow(GroupedScheduleView groupedSchedule)
        {
            if (groupedSchedule == null) return;

            try
            {
                var editWindow = new EditScheduleWindow(groupedSchedule, this);
                editWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ Failed to open EditSchedule window: {ex.Message}");
                ShowMessage($"Failed to open EditSchedule window:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Debounced search: wait 400ms after typing stops before applying filter.
        /// </summary>
        private async Task ApplySearchDebouncedAsync()
        {
            _searchDebounceToken?.Cancel();
            var cts = new CancellationTokenSource();
            _searchDebounceToken = cts;

            try
            {
                await Task.Delay(400, cts.Token);
                await ApplySearchAsync();
            }
            catch (TaskCanceledException) { }
        }

        /// <summary>
        /// Apply search filter to schedules.
        /// </summary>
        private async Task ApplySearchAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    var (schedules, totalCount) = await _schedulingService.GetPagedGroupedSchedulesAsync(CurrentPage);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Schedules = new ObservableCollection<GroupedScheduleView>(schedules ?? new List<GroupedScheduleView>());
                        OnPropertyChanged(nameof(Schedules));
                    });

                    TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
                    return;
                }

                var results = await _schedulingService.SearchGroupedSchedulesAsync(SearchText);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Schedules = new ObservableCollection<GroupedScheduleView>(results ?? new List<GroupedScheduleView>());
                    OnPropertyChanged(nameof(Schedules));
                });

            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ Search failed: {ex.Message}");
                ShowMessage($"Search failed:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

    }
}
