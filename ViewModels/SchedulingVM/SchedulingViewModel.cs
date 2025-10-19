using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.SchedulingVM
{
    public class SchedulingViewModel : BaseViewModel
    {
        private readonly SchedulingService _schedulingService = new();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public ObservableCollection<Reservation> ContractSignedReservations { get; } = new();
        public ObservableCollection<GroupSchedule> GroupedSchedules { get; } = new();

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    _ = DebounceSearchAsync(_searchText); // debounce search
                }
            }
        }

        private CancellationTokenSource _debounceCts;

        public ICommand LoadPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand OpenAssignWorkerCommand { get; }
        public ICommand EditScheduledWorkerCommand { get; }

        public SchedulingViewModel()
        {
            LoadPageCommand = new RelayCommand(async () => await LoadPageAsync(CurrentPage));
            NextPageCommand = new RelayCommand(async () => await NextPageAsync(), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await PrevPageAsync(), () => CurrentPage > 1);
            OpenAssignWorkerCommand = new RelayCommand(OpenAssignWorkerDialogAsync);
            EditScheduledWorkerCommand = new RelayCommand<GroupSchedule>(OpenEditScheduleDialog);

            _ = LoadPageAsync(1);
        }

        // 🔹 Debounce search
        private async Task DebounceSearchAsync(string query)
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            try
            {
                await Task.Delay(400, token); // wait 400ms before running search
                if (!token.IsCancellationRequested)
                    await SearchSchedulesAsync(query);
            }
            catch (TaskCanceledException)
            {
                // Ignore cancelled searches
            }
        }

        // 🔹 Main data loader (pagination)
        private async Task LoadPageAsync(int pageNumber)
        {
            IsLoading = true;
            try
            {
                var (reservations, totalCount) = await _schedulingService.GetCompletedReservationsPagedAsync(pageNumber);
                TotalCount = totalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / 10);

                ContractSignedReservations.Clear();
                foreach (var reservation in reservations)
                    ContractSignedReservations.Add(reservation);

                var schedules = await _schedulingService.GetPagedSchedulesAsync(pageNumber);
                var grouped = _schedulingService.GroupSchedules(schedules);

                GroupedSchedules.Clear();
                foreach (var g in grouped)
                    GroupedSchedules.Add(g);

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

        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
                await LoadPageAsync(CurrentPage + 1);
        }

        private async Task PrevPageAsync()
        {
            if (CurrentPage > 1)
                await LoadPageAsync(CurrentPage - 1);
        }

        private async void OpenAssignWorkerDialogAsync()
        {
            var window = new AssignWorker
            {
                Owner = Application.Current.MainWindow,
            };
            window.ShowDialog();

            await LoadPageAsync(CurrentPage);
        }

        // 🔹 Search logic
        private async Task SearchSchedulesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                await LoadPageAsync(CurrentPage);
                return;
            }

            IsLoading = true;
            try
            {
                var results = await _schedulingService.SearchSchedulesAsync(query);

                GroupedSchedules.Clear();
                foreach (var g in results)
                    GroupedSchedules.Add(g);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error searching schedules: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async void OpenEditScheduleDialog(GroupSchedule groupSchedule)
        {
          new EditScheduleWindow(groupSchedule, this).ShowDialog();
           
        }

    }
}
