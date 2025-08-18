using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

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
            set
            {
                _selectedReservation = value;
                OnPropertyChanged();
            }
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

        public ReservationListViewModel()
        {
            ViewReservationCommand = new RelayCommand<Reservation>(ViewReservation);
            DeleteReservationCommand = new RelayCommand<Reservation>(async (res) => await DeleteReservation(res));
            UpdateReservationCommand = new RelayCommand<Reservation>(async (res) => await UpdateReservation(res));

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

        public async Task LoadReservations()
        {
            IsLoading = true;

            try
            {
                var client = await SupabaseService.GetClientAsync();

                var result = await client
                                        .From<Reservation>()
                                        .Select(@"
                                                *,
                                                profile:profile_id(*),
                                                thememotif:theme_motif_id(*),
                                                grazing:grazing_id(*),
                                                package:package_id(*)
                                            ")
                                        .Get();
                AllReservations.Clear();
                FilteredReservations.Clear();


                foreach (var reservation in result.Models)
                {
                    AllReservations.Add(reservation);
                    FilteredReservations.Add(reservation);
                }

                UpdateReservationCounts();

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
                        IsLoading = false;
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
            if (reservation == null)
            {
                Debug.WriteLine("❌ Reservation is null.");
                return;
            }

            try
            {
                var client = await SupabaseService.GetClientAsync();

                Debug.WriteLine($"🛠️ Updating reservation ID: {reservation.Id}");

               
                var updateResponse = await client
                    .From<Reservation>()
                    .Where(x => x.Id == reservation.Id)
                    .Set(r => r.Status, reservation.Status)
                    .Update();

                Debug.WriteLine($"✅ Update success for ID {reservation.Id}");

                // Refresh data from server to update local collection
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

                          
                            System.Windows.MessageBox.Show("✅ Reservation updated successfully!",
                                "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }
                    });
                }
                else
                {
                    Debug.WriteLine($"⚠️ Refreshed reservation is null. It may not exist.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error updating reservation: {ex.Message}");
                System.Windows.MessageBox.Show("❌ Failed to update reservation.\n\n" + ex.Message,
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }




        private async Task DeleteReservation(Reservation reservation)
        {
            if (reservation == null)
            {
                Debug.WriteLine("❌ Reservation is null.");
                return;
            }

            var client = await SupabaseService.GetClientAsync();

            try
            {
                Debug.WriteLine($"Attempting to delete reservation with ID: {reservation.Id}");

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

