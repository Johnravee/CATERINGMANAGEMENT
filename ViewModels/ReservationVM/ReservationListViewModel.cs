/*
 * ReservationListViewModel.cs
 * 
 * ViewModel for managing the list of reservations in the catering management application.
 * 
 * Responsibilities:
 * - Load, paginate, and filter reservations.
 * - Manage reservation selection and detail viewing.
 * - Handle real-time updates via Supabase realtime subscription to the reservations table.
 * - Maintain reservation status counts (total, pending, confirmed, canceled) and update UI accordingly.
 * - Support commands for viewing and deleting reservations, as well as pagination controls.
 * - Implements debounced search functionality for efficient filtering.
 * 
 * Author: RAVE
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace CATERINGMANAGEMENT.ViewModels.ReservationVM
{
    public class ReservationListViewModel : BaseViewModel
    {
        #region Fields & Services
        private readonly ReservationService _reservationService = new();
        private CancellationTokenSource? _searchDebounceToken;
        #endregion

        #region Properties & Data
        private ObservableCollection<Reservation> _allReservations = new();
        public ObservableCollection<Reservation> AllReservations
        {
            get => _allReservations;
            set { _allReservations = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Reservation> _filteredReservations = new();
        public ObservableCollection<Reservation> FilteredReservations
        {
            get => _filteredReservations;
            set { _filteredReservations = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public int TotalCount { get => _totalCount; set { _totalCount = value; OnPropertyChanged(); } }
        private int _totalCount;

        public int PendingCount { get => _pendingCount; set { _pendingCount = value; OnPropertyChanged(); } }
        private int _pendingCount;

        public int ConfirmedCount { get => _confirmedCount; set { _confirmedCount = value; OnPropertyChanged(); } }
        private int _confirmedCount;

        public int CancelledCount { get => _cancelledCount; set { _cancelledCount = value; OnPropertyChanged(); } }
        private int _cancelledCount;

        private Reservation? _selectedReservation;
        public Reservation? SelectedReservation
        {
            get => _selectedReservation;
            set { _selectedReservation = value; OnPropertyChanged(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _ = ApplySearchDebounced();
            }
        }

        public int CurrentPage { get => _currentPage; set { _currentPage = value; OnPropertyChanged(); } }
        private int _currentPage = 1;

        public int PageSize { get => _pageSize; set { _pageSize = value; OnPropertyChanged(); } }
        private int _pageSize = 20;

        public int TotalPages { get => _totalPages; set { _totalPages = value; OnPropertyChanged(); } }
        private int _totalPages = 1;
        #endregion

        #region Commands
        public ICommand ViewReservationCommand { get; }
        public ICommand DeleteReservationCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        #endregion

        #region Constructor
        public ReservationListViewModel()
        {
            ViewReservationCommand = new RelayCommand<Reservation>(async (res) => await ViewReservation(res));
            DeleteReservationCommand = new RelayCommand<Reservation>(async (res) => await DeleteReservation(res));
            NextPageCommand = new RelayCommand(async () => await LoadReservations(CurrentPage + 1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await LoadReservations(CurrentPage - 1), () => CurrentPage > 1);

            _ = Task.Run(SubscribeToRealtime);
        }
        #endregion

        #region Reservation Loading & Pagination
        public async Task LoadReservations(int pageNumber = 1)
        {
            IsLoading = true;
            try
            {
                AppLogger.Info("Loading reservations...");

                var reservationsTask = _reservationService.GetReservationsAsync(pageNumber, PageSize);
                var countsTask = _reservationService.GetReservationStatusCountsAsync();

                await Task.WhenAll(reservationsTask, countsTask);

                AllReservations = new ObservableCollection<Reservation>(reservationsTask.Result);
                FilteredReservations = new ObservableCollection<Reservation>(reservationsTask.Result);

                var counts = countsTask.Result;
                if (counts != null)
                {
                    TotalCount = counts.TotalReservations;
                    PendingCount = counts.Pending;
                    ConfirmedCount = counts.Confirmed;
                    CancelledCount = counts.Canceled;

                    TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
                }
                else
                {
                    TotalCount = 0;
                    PendingCount = 0;
                    ConfirmedCount = 0;
                    CancelledCount = 0;
                    TotalPages = 1;
                }

                CurrentPage = pageNumber;

                if (!string.IsNullOrWhiteSpace(SearchText))
                    ApplySearch();

                AppLogger.Success("Reservations loaded successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load reservations");
            }
            finally { IsLoading = false; }
        }
        #endregion

        #region Search Filtering
        private async Task ApplySearchDebounced()
        {
            _searchDebounceToken?.Cancel();
            var cts = new CancellationTokenSource();
            _searchDebounceToken = cts;

            try
            {
                await Task.Delay(400, cts.Token);
                ApplySearch();
            }
            catch (TaskCanceledException) { /* ignore */ }
        }

        private void ApplySearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredReservations = new ObservableCollection<Reservation>(AllReservations);
                return;
            }

            var query = SearchText.Trim().ToLower();
            var filtered = AllReservations.Where(r =>
                (r.ReceiptNumber ?? "").ToLower().Contains(query) ||
                (r.Celebrant ?? "").ToLower().Contains(query) ||
                (r.Venue ?? "").ToLower().Contains(query) ||
                (r.Location ?? "").ToLower().Contains(query) ||
                (r.Status ?? "").ToLower().Contains(query)
            ).ToList();

            FilteredReservations = new ObservableCollection<Reservation>(filtered);
            AppLogger.Info($"Filtered {filtered.Count} reservations by '{query}'");
        }
        #endregion

        #region Realtime Updates
        private async Task SubscribeToRealtime()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                // Subscribe to the reservations table (public schema)
                var channel = client.Realtime.Channel("realtime", "public", "reservations");

                // Generic handler for all events, just logs raw payloads
                channel.AddPostgresChangeHandler(ListenType.All, (sender, change) =>
                {
                    Debug.WriteLine("Realtime event change: " + change.Event);
                    Debug.WriteLine("Realtime event change payload: " + change.Payload);
                });

                // Insert handler
                channel.AddPostgresChangeHandler(ListenType.Inserts, (sender, change) =>
                {
                    var inserted = change.Model<Reservation>();
                    if (inserted == null)
                    {
                        Debug.WriteLine("[Realtime Insert] Failed to deserialize inserted record.");
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var existing = AllReservations.FirstOrDefault(r => r.Id == inserted.Id);
                        if (existing == null)
                        {
                            AllReservations.Insert(0, inserted);
                            AppLogger.Info($"Realtime Insert: Added reservation ID {inserted.Id}");
                           await RefreshReservationCountsAsync();
                        }
                        else
                        {
                            var index = AllReservations.IndexOf(existing);
                            AllReservations[index] = inserted;
                            AppLogger.Info($"Realtime Insert (update existing): Updated reservation ID {inserted.Id}");
                        }

                        if (!string.IsNullOrWhiteSpace(SearchText))
                        {
                            ApplySearch();
                        }
                        else
                        {
                            FilteredReservations.Insert(0, inserted);
                        }
                    });
                });

                // Update handler
                channel.AddPostgresChangeHandler(ListenType.Updates, (sender, change) =>
                {
                    var updated = change.Model<Reservation>();
                    if (updated == null)
                    {
                        Debug.WriteLine("[Realtime Update] Failed to deserialize updated record.");
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var existing = AllReservations.FirstOrDefault(r => r.Id == updated.Id);
                        if (existing != null)
                        {
                            var index = AllReservations.IndexOf(existing);
                            AllReservations[index] = updated;
                            AppLogger.Info($"Realtime Update: Updated reservation ID {updated.Id}");
                            await RefreshReservationCountsAsync();
                        }
                        else
                        {
                            AllReservations.Insert(0, updated);
                            AppLogger.Info($"Realtime Update: Inserted missing reservation ID {updated.Id}");
                        }

                        if (!string.IsNullOrWhiteSpace(SearchText))
                        {
                            ApplySearch();
                        }
                        else
                        {
                            var filteredExisting = FilteredReservations.FirstOrDefault(r => r.Id == updated.Id);
                            if (filteredExisting != null)
                            {
                                var filteredIndex = FilteredReservations.IndexOf(filteredExisting);
                                FilteredReservations[filteredIndex] = updated;
                            }
                        }
                    });
                });

                var result = await channel.Subscribe();
                AppLogger.Success($"Subscribed to realtime reservation updates: {result}");
                Debug.WriteLine($"✅ Subscribed to realtime reservation updates: {result}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error subscribing to realtime reservation updates");
            }
        }
        #endregion

        #region Reservation Actions
        private async Task DeleteReservation(Reservation reservation)
        {
            if (reservation == null) return;

            var confirm = MessageBox.Show($"Delete reservation {reservation.ReceiptNumber}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                if (await _reservationService.DeleteReservationAsync(reservation))
                {
                    AllReservations.Remove(reservation);
                    FilteredReservations.Remove(reservation);
                    await RefreshReservationCountsAsync();
                    AppLogger.Success($"Deleted reservation ID: {reservation.Id}");
                }
                else
                {
                    AppLogger.Error("Failed to delete reservation.", showToUser: true);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error deleting reservation");
            }
        }

        private async Task ViewReservation(Reservation reservation)
        {
            if (reservation == null) return;

            try
            {
                SelectedReservation = await _reservationService.GetReservationWithJoinsAsync(reservation.Id);
                if (SelectedReservation != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ReservationDetails(SelectedReservation).ShowDialog();
                    });
                    AppLogger.Success($"Opened reservation details for ID {reservation.Id}");
                }
                else
                {
                    AppLogger.Error("Failed to load reservation details.", showToUser: true);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error opening reservation details");
            }
        }

        private async Task RefreshReservationCountsAsync()
        {
            try
            {
                // Invalidate the cached
                _reservationService.InvalidateAllReservationCaches();

                
                var counts = await _reservationService.GetReservationStatusCountsAsync();

                if (counts != null)
                {
                    TotalCount = counts.TotalReservations;
                    PendingCount = counts.Pending;
                    ConfirmedCount = counts.Confirmed;
                    CancelledCount = counts.Canceled;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error refreshing reservation counts");
            }
        }




        #endregion
    }
}
