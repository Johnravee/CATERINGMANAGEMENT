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

        public ObservableCollection<Reservation> AllReservations { get; } = new();
        public ObservableCollection<Reservation> FilteredReservations { get; } = new();

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

        public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); ApplySearchFilter(); } }
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
            DeleteReservationCommand = new RelayCommand<Reservation>(async (res) => await DeleteReservation(res));;
            NextPageCommand = new RelayCommand(async () => await LoadReservations(CurrentPage + 1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await LoadReservations(CurrentPage - 1), () => CurrentPage > 1);
        }

        // 🟢 Load Reservations
        public async Task LoadReservations(int pageNumber = 1)
        {
            IsLoading = true;
            try
            {
                var reservations = await _reservationService.GetReservationsAsync(pageNumber, PageSize);
                AllReservations.Clear();
                FilteredReservations.Clear();

                foreach (var res in reservations)
                {
                    AllReservations.Add(res);
                    FilteredReservations.Add(res);
                }

                TotalCount = await _reservationService.GetTotalCountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
                CurrentPage = pageNumber;

                UpdateReservationCounts();
                ApplySearchFilter();

                await SubscribeToRealtime();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 🟢 Realtime Subscription
        private async Task SubscribeToRealtime()
        {
            var client = await SupabaseService.GetClientAsync();
            var channel = client.Realtime.Channel("realtime", "public", "reservations");
            await channel.Subscribe();

            channel.AddPostgresChangeHandler(ListenType.Updates, (sender, change) =>
            {
                var updated = change.Model<Reservation>();
                if (updated == null) return;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
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

                    UpdateReservationCounts();
                    ApplySearchFilter();
                });
            });
        }

      

        private async Task DeleteReservation(Reservation reservation)
        {
            if (await _reservationService.DeleteReservationAsync(reservation))
            {
                AllReservations.Remove(reservation);
                FilteredReservations.Remove(reservation);
                UpdateReservationCounts();
            }
        }

        private void ApplySearchFilter()
        {
            var query = SearchText?.Trim().ToLower() ?? "";
            FilteredReservations.Clear();

            var results = string.IsNullOrEmpty(query)
                ? AllReservations
                : AllReservations.Where(r =>
                    (r.ReceiptNumber ?? "").ToLower().Contains(query) ||
                    (r.Celebrant ?? "").ToLower().Contains(query) ||
                    (r.Venue ?? "").ToLower().Contains(query) ||
                    (r.Location ?? "").ToLower().Contains(query) ||
                    (r.Status ?? "").ToLower().Contains(query));

            foreach (var res in results)
                FilteredReservations.Add(res);
        }

        private void UpdateReservationCounts()
        {
            TotalCount = AllReservations.Count;
            PendingCount = AllReservations.Count(r => r.Status?.ToLower() == "pending");
            ConfirmedCount = AllReservations.Count(r => r.Status?.ToLower() == "confirmed");
            CancelledCount = AllReservations.Count(r => r.Status?.ToLower() == "canceled");
        }

        private async Task ViewReservation(Reservation reservation)
        {
            if (reservation == null) return;

            try
            {
                SelectedReservation = reservation;

                // 🆕 Re-fetch full reservation with joined data
                var updated = await _reservationService.GetReservationWithJoinsAsync(reservation.Id);

                if (updated != null)
                {
                    SelectedReservation = updated;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var detailsWindow = new ReservationDetails(updated);
                        detailsWindow.ShowDialog();
                    });
                }
                else
                {
                    MessageBox.Show("Failed to load reservation details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error viewing reservation: {ex.Message}");
                MessageBox.Show($"Unexpected error.\n\n{ex.Message}", "Error");
            }
        }

    }
}
