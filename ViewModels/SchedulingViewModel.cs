using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
        public ObservableCollection<GroupSchedule> GroupedSchedules { get; } = new();

        public ICommand OpenAssignWorkerCommand { get; }

        public SchedulingViewModel()
        {
            OpenAssignWorkerCommand = new RelayCommand(OpenAssignWorkerDialog);
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

                // 1. Fetch reservations
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
                            .Select(@"
                                *,
                                reservations:reservation_id(
                                    *,
                                    package:package_id(*),
                                    profile:profile_id(*),
                                    thememotif:theme_motif_id(*),
                                    grazing:grazing_id(*)
                                ),
                                workers:worker_id(*)
                            ")
                            .Get();


                Schedules.Clear();
                foreach (var schedule in scheduleResult.Models)
                    Schedules.Add(schedule);

                // 3. Group by reservation
                GroupedSchedules.Clear();
                var grouped = Schedules
                    .Where(s => s.Reservations != null && s.Workers != null)
                    .GroupBy(s => s.ReservationId)
                    .ToList();

                foreach (var group in grouped)
                {
                    var reservation = group.First().Reservations!;
                    var workers = group.Select(s => s.Workers!).ToList();

                    GroupedSchedules.Add(new GroupSchedule
                    {
                        Reservation = reservation,
                        Workers = workers
                    });
                }
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

        /// <summary>
        /// Opens AssignWorker dialog window
        /// </summary>
        private void OpenAssignWorkerDialog()
        {
          
            var window = new AssignWorker
            {
                Owner = Application.Current.MainWindow,
            };

            window.ShowDialog();

            // TODO: after closing, pull new schedules or refresh
            // await LoadData();
        }
    }
}
