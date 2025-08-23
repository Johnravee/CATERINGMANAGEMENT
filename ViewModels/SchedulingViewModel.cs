using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class SchedulingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Collections exposed to your view
        public ObservableCollection<Reservation> ContractSignedReservations { get; } = new();
        public ObservableCollection<Scheduling> Schedules { get; } = new();

        public SchedulingViewModel()
        {
            _ = LoadData();
        }

        /// <summary>
        /// Fetch both contract signed reservations and schedules
        /// </summary>
        public async Task LoadData()
        {
            IsLoading = true;
            try
            {
                var client = await SupabaseService.GetClientAsync();

                // 1. Fetch contract signed reservations
                var reservationResult = await client
                    .From<Reservation>()
                    .Select(@"
                        *,
                        profile:profile_id(*),
                        thememotif:theme_motif_id(*),
                        grazing:grazing_id(*),
                        package:package_id(*)
                    ")
                    .Where(r => r.Status == "done")
                    .Order(x => x.EventDate, Ordering.Ascending)
                    .Get();

                ContractSignedReservations.Clear();
                foreach (var reservation in reservationResult.Models)
                    ContractSignedReservations.Add(reservation);

                // 2. Fetch schedules
                var scheduleResult = await client
                    .From<Scheduling>() 
                    .Select("*, reservation:reservation_id(*)")
                    .Order(x => x.CreatedAt, Ordering.Ascending) // 👈 use a real column (not just x)
                    .Get();

                Schedules.Clear();
                foreach (var schedule in scheduleResult.Models)
                    Schedules.Add(schedule);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error loading scheduling data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
