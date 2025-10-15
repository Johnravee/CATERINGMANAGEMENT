using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace CATERINGMANAGEMENT.ViewModels.ReservationVM
{
    public class ReservationListViewModel : INotifyPropertyChanged
    {
        private readonly ReservationService _reservationService = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<Reservation> AllReservations { get => _allReservations; set { _allReservations = value; OnPropertyChanged(); } }
        private ObservableCollection<Reservation> _allReservations = new();

        public ObservableCollection<Reservation> FilteredReservations { get => _filteredReservations; set { _filteredReservations = value; OnPropertyChanged(); } }
        private ObservableCollection<Reservation> _filteredReservations = new();

        public ICommand ViewReservationCommand { get; }
        public ICommand DeleteReservationCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }

        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
        private bool _isLoading;

        public int TotalCount { get => _totalCount; set { _totalCount = value; OnPropertyChanged(); } }
        private int _totalCount;

        public int PendingCount { get => _pendingCount; set { _pendingCount = value; OnPropertyChanged(); } }
        private int _pendingCount;

        public int ConfirmedCount { get => _confirmedCount; set { _confirmedCount = value; OnPropertyChanged(); } }
        private int _confirmedCount;

        public int CancelledCount { get => _cancelledCount; set { _cancelledCount = value; OnPropertyChanged(); } }
        private int _cancelledCount;

        public Reservation? SelectedReservation { get => _selectedReservation; set { _selectedReservation = value; OnPropertyChanged(); } }
        private Reservation? _selectedReservation;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                if (!string.IsNullOrWhiteSpace(_searchText))
                    ApplySearchFilter();
                else
                    FilteredReservations = new ObservableCollection<Reservation>(AllReservations);
            }
        }
        private string _searchText = string.Empty;

        public int CurrentPage { get => _currentPage; set { _currentPage = value; OnPropertyChanged(); } }
        private int _currentPage = 1;

        public int PageSize { get => _pageSize; set { _pageSize = value; OnPropertyChanged(); } }
        private int _pageSize = 20;

        public int TotalPages { get => _totalPages; set { _totalPages = value; OnPropertyChanged(); } }
        private int _totalPages = 1;

        public ReservationListViewModel()
        {
            ViewReservationCommand = new RelayCommand<Reservation>(ViewReservation);
            DeleteReservationCommand = new RelayCommand<Reservation>(async (res) => await DeleteReservation(res));
            NextPageCommand = new RelayCommand(async () => await LoadReservations(CurrentPage + 1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await LoadReservations(CurrentPage - 1), () => CurrentPage > 1);

            _ = Task.Run(SubscribeToRealtime);
        }

        public async Task LoadReservations(int pageNumber = 1)
        {
            IsLoading = true;
            try
            {
                AppLogger.Info("Loading reservations...");

                var reservationsTask = _reservationService.GetReservationsAsync(pageNumber, PageSize);
                var countsTask = _reservationService.GetReservationStatusCountsAsync();

                await Task.WhenAll(reservationsTask, countsTask);

                var reservations = reservationsTask.Result;
                var counts = countsTask.Result;

                AllReservations = new ObservableCollection<Reservation>(reservations);
                FilteredReservations = new ObservableCollection<Reservation>(reservations);

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
                    TotalPages = 1;
                    PendingCount = 0;
                    ConfirmedCount = 0;
                    CancelledCount = 0;
                }

                CurrentPage = pageNumber;

                if (!string.IsNullOrWhiteSpace(SearchText))
                    ApplySearchFilter();

                AppLogger.Success("Reservations loaded successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error loading reservations: {ex.Message}", showToUser: true);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SubscribeToRealtime()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var channel = client.Realtime.Channel("realtime", "public", "reservations");
                await channel.Subscribe();

                AppLogger.Info("Subscribed to realtime reservation updates.");

                channel.AddPostgresChangeHandler(ListenType.Updates, (sender, change) =>
                {
                    var updated = change.Model<Reservation>();
                    if (updated == null) return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = AllReservations.FirstOrDefault(r => r.Id == updated.Id);
                        if (existing != null)
                        {
                            var index = AllReservations.IndexOf(existing);
                            AllReservations[index] = updated;
                        }
                        else
                        {
                            AllReservations.Add(updated);
                        }

                        if (!string.IsNullOrWhiteSpace(SearchText))
                            ApplySearchFilter();
                    });
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error subscribing to realtime updates: {ex.Message}");
            }
        }

        private async Task DeleteReservation(Reservation reservation)
        {
            try
            {
                if (await _reservationService.DeleteReservationAsync(reservation))
                {
                    AllReservations.Remove(reservation);
                    FilteredReservations.Remove(reservation);

                    AppLogger.Success($"Deleted reservation ID: {reservation.Id}");
                }
                else
                {
                    MessageBox.Show("Failed to delete reservation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    AppLogger.Error($"Failed to delete reservation with ID {reservation.Id}", showToUser: false);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error deleting reservation: {ex.Message}", showToUser: true);
            }
        }

        private void ApplySearchFilter()
        {
            var query = SearchText?.Trim().ToLower() ?? "";
            var results = AllReservations.Where(r =>
                (r.ReceiptNumber ?? "").ToLower().Contains(query) ||
                (r.Celebrant ?? "").ToLower().Contains(query) ||
                (r.Venue ?? "").ToLower().Contains(query) ||
                (r.Location ?? "").ToLower().Contains(query) ||
                (r.Status ?? "").ToLower().Contains(query)).ToList();

            FilteredReservations = new ObservableCollection<Reservation>(results);
        }

        private async Task ViewReservation(Reservation reservation)
        {
            if (reservation == null) return;

            try
            {
                SelectedReservation = reservation;
                var updated = await _reservationService.GetReservationWithJoinsAsync(reservation.Id);

                if (updated != null)
                {
                    SelectedReservation = updated;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var detailsWindow = new ReservationDetails(updated);
                        detailsWindow.ShowDialog();
                    });

                    AppLogger.Success($"Opened reservation details for ID {reservation.Id}");
                }
                else
                {
                    MessageBox.Show("Failed to load reservation details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    AppLogger.Error($"Failed to load reservation details for ID {reservation.Id}", showToUser: false);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error viewing reservation: {ex.Message}", showToUser: true);
            }
        }
    }
}
