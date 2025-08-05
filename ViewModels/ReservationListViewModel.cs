using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;

using Supabase.Realtime;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
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


        private void UpdateReservationCounts()
        {
            TotalCount = AllReservations.Count;

            PendingCount = AllReservations.Count(r => r.Status?.ToLower() == "pending");
            ConfirmedCount = AllReservations.Count(r => r.Status?.ToLower() == "confirmed");
            CancelledCount = AllReservations.Count(r => r.Status?.ToLower() == "canceled");
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

        public async Task LoadReservations()
        {
            IsLoading = true;

            try
            {
                var client = await SupabaseService.GetClientAsync();

                // Load initial data
                var result = await client.From<Reservation>().Get();
                AllReservations.Clear();
                FilteredReservations.Clear();

                foreach (var reservation in result.Models)
                {
                    AllReservations.Add(reservation);
                    FilteredReservations.Add(reservation);
                }

                UpdateReservationCounts();

                // Subscribe to real-time updates
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

                        ApplySearchFilter();
                        UpdateReservationCounts();
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
    }
}
