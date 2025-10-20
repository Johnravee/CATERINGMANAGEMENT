/*
 * FILE: SchedulingViewModel.cs
 * PURPOSE: Acts as the main ViewModel for the Scheduling page.
 *          Handles the loading of both current schedules and completed reservations
 *          (ready to be scheduled). Supports pagination, refreshing,
 *          and integration with the AssignWorker window.
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.SchedulingVM
{
    public class SchedulingViewModel : BaseViewModel
    {
        private readonly SchedulingService _schedulingService = new();

        // ✅ Data collections
        public ObservableCollection<GroupedScheduleView> Schedules { get; set; } = new();
        public ObservableCollection<Reservation> CompletedReservations { get; set; } = new();

        // ✅ UI state flags
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

        // ✅ Pagination info
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

        private const int PageSize = 10;

        // ✅ Commands
        public ICommand RefreshCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand OpenAssignWorkerCommand { get; }

        public SchedulingViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await ReloadDataAsync());
            NextPageCommand = new RelayCommand(async () => await LoadSchedulesAsync(CurrentPage + 1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await LoadSchedulesAsync(CurrentPage - 1), () => CurrentPage > 1);
            OpenAssignWorkerCommand = new RelayCommand(OpenAssignWorkerWindow);

            _ = ReloadDataAsync();
        }

        /// <summary>
        /// Reloads both schedules and completed reservations.
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
        /// Loads paginated schedules for display in the main scheduling DataGrid.
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
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Loads reservations with status "completed" (ready for scheduling).
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
        /// Opens the AssignWorker window for assigning workers to a reservation.
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
    }
}
