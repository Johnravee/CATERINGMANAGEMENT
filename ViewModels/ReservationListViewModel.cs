using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;
using static Supabase.Postgrest.Constants; 

namespace CATERINGMANAGEMENT.ViewModel
{
    public class ReservationListViewModel : INotifyPropertyChanged
    {


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<Reservation> AllReservations { get; } = new();
        public ObservableCollection<Reservation> FilteredReservations { get; } = new();

        public ICommand ViewReservationCommand { get; }
        public ICommand DeleteReservationCommand { get; }
        public ICommand UpdateReservationCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); }
        }

        private int _pendingCount;
        public int PendingCount
        {
            get => _pendingCount;
            set { _pendingCount = value; OnPropertyChanged(); }
        }

        private int _confirmedCount;
        public int ConfirmedCount
        {
            get => _confirmedCount;
            set { _confirmedCount = value; OnPropertyChanged(); }
        }

        private int _cancelledCount;
        public int CancelledCount
        {
            get => _cancelledCount;
            set { _cancelledCount = value; OnPropertyChanged(); }
        }

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
                ApplySearchFilter();
            }
        }

        // Pagination properties
        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set { _pageSize = value; OnPropertyChanged(); }
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); }
        }

        public ReservationListViewModel()
        {
            ViewReservationCommand = new RelayCommand<Reservation>(ViewReservation);
            DeleteReservationCommand = new RelayCommand<Reservation>(async (res) => await DeleteReservation(res));
            UpdateReservationCommand = new RelayCommand<Reservation>(async (res) => await UpdateReservation(res));

            NextPageCommand = new RelayCommand(
                async () => await LoadReservations(CurrentPage + 1),
                () => CurrentPage < TotalPages
            );

            PrevPageCommand = new RelayCommand(
                async () => await LoadReservations(CurrentPage - 1),
                () => CurrentPage > 1
            );


        }

        private void ApplySearchFilter()
        {
            var query = SearchText?.Trim().ToLower() ?? "";
            FilteredReservations.Clear();

            var results = string.IsNullOrEmpty(query)
                ? AllReservations
                : AllReservations.Where(r =>
                    (!string.IsNullOrEmpty(r.ReceiptNumber) && r.ReceiptNumber.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(r.Celebrant) && r.Celebrant.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(r.Venue) && r.Venue.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(r.Location) && r.Location.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(r.Status) && r.Status.ToLower().Contains(query)));

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

        public async Task LoadReservations(int pageNumber = 1)
        {
            IsLoading = true;

            try
            {
                var client = await SupabaseService.GetClientAsync();

                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var result = await client
                    .From<Reservation>()
                    .Select(@"
                        *,
                        profile:profile_id(*),
                        thememotif:theme_motif_id(*),
                        grazing:grazing_id(*),
                        package:package_id(*)
                    ")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Range(from, to)
                    .Get();

                AllReservations.Clear();
                FilteredReservations.Clear();

                foreach (var reservation in result.Models)
                {
                    AllReservations.Add(reservation);
                    FilteredReservations.Add(reservation);
                }

                // Count total rows (separate call, lightweight)
                TotalCount = await client
                    .From<Reservation>()
                    .Select("id")
                    .Count(CountType.Exact);

                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
                CurrentPage = pageNumber;

                UpdateReservationCounts();
                ApplySearchFilter();

                // subscribe realtime
                var channel = client.Realtime.Channel("realtime", "public", "reservations");
                await channel.Subscribe();

                channel.AddPostgresChangeHandler(ListenType.Updates, (sender, change) =>
                {
                    var updated = change.Model<Reservation>();
                    if (updated == null) return;

                    App.Current.Dispatcher.Invoke(() =>
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
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error loading reservations: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private Task ViewReservation(Reservation reservation)
        {
            if (reservation == null) return Task.CompletedTask;

            SelectedReservation = reservation;
            Debug.WriteLine($"📄 Viewing reservation: {reservation.Id} | {reservation.Celebrant}");

            App.Current.Dispatcher.Invoke(() =>
            {
                var detailsWindow = new View.Windows.ReservationDetails(reservation, UpdateReservationCommand);
                detailsWindow.ShowDialog();
            });

            return Task.CompletedTask;
        }

        private async Task UpdateReservation(Reservation reservation)
        {
            if (reservation == null) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();

                var updateResponse = await client
                    .From<Reservation>()
                    .Where(x => x.Id == reservation.Id)
                    .Set(r => r.Status, reservation.Status)
                    .Update();

                var refreshed = await client.From<Reservation>()
                    .Where(x => x.Id == reservation.Id)
                    .Select(@"
                        *,
                        profile:profile_id(*),
                        thememotif:theme_motif_id(*),
                        grazing:grazing_id(*),
                        package:package_id(*)
                    ")
                    .Single();

                if (refreshed != null)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = AllReservations.FirstOrDefault(r => r.Id == reservation.Id);
                        if (existing != null)
                        {
                            var index = AllReservations.IndexOf(existing);
                            AllReservations[index] = refreshed;
                            ApplySearchFilter();
                            UpdateReservationCounts();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error updating reservation: {ex.Message}");
            }
        }

        private async Task DeleteReservation(Reservation reservation)
        {
            if (reservation == null) return;

            var client = await SupabaseService.GetClientAsync();

            try
            {
                await client.From<Reservation>().Where(x => x.Id == reservation.Id).Delete();

                AllReservations.Remove(reservation);
                FilteredReservations.Remove(reservation);
                UpdateReservationCounts();

                Debug.WriteLine($"🗑️ Deleted reservation: {reservation.ReceiptNumber}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error deleting reservation: {ex.Message}");
            }
        }
    }
}
