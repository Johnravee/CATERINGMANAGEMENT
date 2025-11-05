/*
 * FILE: SchedulingViewModel.cs
 * PURPOSE: ViewModel for managing schedules and completed reservations.
 *
 * RESPONSIBILITIES:
 *  - Load paged grouped schedules
 *  - Load completed reservations
 *  - Search schedules with debounce
 *  - Handle pagination
 *  - Open AssignWorker and EditSchedule windows
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using CATERINGMANAGEMENT.Mailer;
using CATERINGMANAGEMENT.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace CATERINGMANAGEMENT.ViewModels.SchedulingVM
{
    public class SchedulingViewModel : BaseViewModel
    {
        #region Constants
        private const int PageSize = 10;
        #endregion

        #region Services
        private readonly SchedulingService _schedulingService = new();
        #endregion

        #region Fields
        private CancellationTokenSource? _searchDebounceToken;
        private bool _isLoading;
        private bool _isRefreshing;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private string _searchText = string.Empty;
        #endregion

        #region Data Collections
        public ObservableCollection<GroupedScheduleView> Schedules { get; set; } = new();
        public ObservableCollection<Reservation> CompletedReservations { get; set; } = new();
        #endregion

        #region UI State
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { _isRefreshing = value; OnPropertyChanged(); }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); }
        }

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
        public ICommand RefreshCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand OpenAssignWorkerCommand { get; }
        public ICommand OpenEditScheduleCommand { get; }
        public ICommand DeleteScheduledWorkerCommand { get; }
        #endregion

        #region Constructor
        public SchedulingViewModel()
        {
            RefreshCommand = new RelayCommand(async () => await ReloadDataAsync());
            NextPageCommand = new RelayCommand(async () => await LoadSchedulesAsync(CurrentPage + 1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await LoadSchedulesAsync(CurrentPage - 1), () => CurrentPage > 1);
            OpenAssignWorkerCommand = new RelayCommand(OpenAssignWorkerWindow);
            OpenEditScheduleCommand = new RelayCommand<GroupedScheduleView>(OpenEditScheduleWindow);
            DeleteScheduledWorkerCommand = new RelayCommand<GroupedScheduleView>(async (row) => await DeleteRowAsync(row));

            _ = ReloadDataAsync();

            // Start realtime subscription for grouped schedules
            _ = Task.Run(SubscribeToRealtimeAsync);
        }
        #endregion

        #region Public Methods
        public async Task ReloadDataAsync()
        {
            if (IsRefreshing) return;
            IsRefreshing = true;

            try
            {
                _schedulingService.InvalidateAllSchedulingCaches();
                await Task.WhenAll(LoadSchedulesAsync(CurrentPage), LoadCompletedReservationsAsync());
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error reloading data");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

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

                AppLogger.Info($"Loaded {Schedules.Count} schedules (Page {CurrentPage}/{TotalPages})");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error loading schedules");
                ShowMessage($"Error loading schedules:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

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

                AppLogger.Info($"Loaded {CompletedReservations.Count} completed reservations");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error loading completed reservations");
                ShowMessage($"Error loading completed reservations:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Private Methods
        private void OpenAssignWorkerWindow()
        {
            try
            {
                var assignWin = new AssignWorker(this);
                assignWin.ShowDialog();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to open AssignWorker window");
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
                AppLogger.Error(ex, "Failed to open EditSchedule window");
                ShowMessage($"Failed to open EditSchedule window:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteRowAsync(GroupedScheduleView row)
        {
            if (row == null) return;

            var confirm = MessageBox.Show($"Remove all workers from reservation {row.ReceiptNumber}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                // fetch workers to email before deletion
                var workers = await _schedulingService.GetAssignedWorkersByReservationAsync(row.ReservationId);

                var ok = await _schedulingService.RemoveAllWorkersFromScheduleAsync(row.ReservationId);
                if (ok)
                {
                    // fire-and-forget email notifications
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var mailer = new RemoveWorkerMailer(new Services.EmailService());
                            await mailer.NotifyWorkersRemovalAsync(
                                workers,
                                row.PackageName ?? "Event",
                                row.EventDate.ToString("MMMM dd, yyyy"),
                                row.Venue ?? "Venue");
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error(ex, "Error emailing workers on delete", showToUser: false);
                        }
                    });

                    // Do NOT force-refresh here; caches were invalidated in the service and
                    // realtime subscription will trigger ReloadDataAsync upon DB change events.
                    AppLogger.Success($"Cleared schedule for reservation {row.ReservationId}. Waiting for realtime sync...");
                }
                else
                {
                    ShowMessage("Delete failed. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error deleting scheduled workers for row");
                ShowMessage($"Error deleting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SubscribeToRealtimeAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var channel = client.Realtime.Channel("realtime", "public", "scheduling");

                channel.AddPostgresChangeHandler(ListenType.All, (s, change) =>
                {
                    Debug.WriteLine($"[schedules realtime] event: {change.Event}");
                });

                // Inserts/Updates/Deletes -> refresh current page
                channel.AddPostgresChangeHandler(ListenType.Inserts, async (s, c) => await ReloadDataAsync());
                channel.AddPostgresChangeHandler(ListenType.Updates, async (s, c) => await ReloadDataAsync());
                channel.AddPostgresChangeHandler(ListenType.Deletes, async (s, c) => await ReloadDataAsync());

                var result = await channel.Subscribe();
                AppLogger.Success($"Subscribed to realtime grouped schedules: {result}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error subscribing to realtime schedules");
            }
        }

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
                AppLogger.Error(ex, "Search failed");
                ShowMessage($"Search failed:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion
    }
}
