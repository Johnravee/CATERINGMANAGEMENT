using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels.SchedulingVM
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

        private const int PageSize = 10;
        private int _currentPage = 1;

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // Data collections
        public ObservableCollection<Reservation> ContractSignedReservations { get; } = new();
        public ObservableCollection<Scheduling> Schedules { get; } = new();
        public ObservableCollection<GroupSchedule> GroupedSchedules { get; } = new();

        // Commands
        public ICommand LoadPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand OpenAssignWorkerCommand { get; }

        public SchedulingViewModel()
        {
            LoadPageCommand = new RelayCommand(async () => await LoadPage(CurrentPage));
            NextPageCommand = new RelayCommand(async () => await NextPage(), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await PrevPage(), () => CurrentPage > 1);

            OpenAssignWorkerCommand = new RelayCommand(OpenAssignWorkerDialogAsync);

            _ = LoadPage(1);
        }

        /// <summary>
        /// Loads scheduling data with pagination
        /// </summary>
        public async Task LoadPage(int pageNumber = 1)
        {
            IsLoading = true;

            try
            {
                var client = await SupabaseService.GetClientAsync();

                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                // 1️⃣ Fetch completed reservations (with limit)
                var reservationResult = await client
                    .From<Reservation>()
                    .Select(@"
                        *,
                        profile:profile_id(*),
                        thememotif:theme_motif_id(*),
                        grazing:grazing_id(*),
                        package:package_id(*)
                    ")
                    .Where(r => r.Status == "completed")
                    .Order(x => x.EventDate, Ordering.Ascending)
                    .Range(from, to)
                    .Get();

                ContractSignedReservations.Clear();
                foreach (var reservation in reservationResult.Models)
                    ContractSignedReservations.Add(reservation);

                // 2️⃣ Fetch all schedules (for grouping)
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

                // 3️⃣ Group by reservation
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

                // 4️⃣ Compute pagination
                TotalCount = await client
                    .From<Reservation>()
                    .Select("id")
                    .Count(CountType.Exact);

                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                CurrentPage = pageNumber;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error loading scheduling data: {ex.Message}");
                MessageBox.Show($"Error loading scheduling data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task NextPage()
        {
            if (CurrentPage < TotalPages)
                await LoadPage(CurrentPage + 1);
        }

        private async Task PrevPage()
        {
            if (CurrentPage > 1)
                await LoadPage(CurrentPage - 1);
        }

        /// <summary>
        /// Opens AssignWorker dialog window
        /// </summary>
        private async Task OpenAssignWorkerDialogAsync()
        {
            var window = new AssignWorker
            {
                Owner = Application.Current.MainWindow,
            };

            window.ShowDialog();
            await LoadPage(CurrentPage);
        }
    }
}
