/*
 * FILE: AssignWorkersViewModel.cs
 * PURPOSE: ViewModel for assigning workers to reservations, with search, selection, and batch assignment logic.
 *
 * RESPONSIBILITIES:
 *  - Load reservations and available workers
 *  - Filter and search workers dynamically
 *  - Assign and remove workers from a reservation
 *  - Send email notifications upon assignment
 *  - Refresh parent SchedulingViewModel after changes
 *  - Provide commands for UI interaction
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.SchedulingVM
{
    public class AssignWorkersViewModel : BaseViewModel
    {
        #region Fields & Services
        private readonly AssignWorkerService _assignWorkerService = new();
        private readonly SchedulingViewModel _parentViewModel;
        private readonly CollectionViewSource _filteredWorkers = new();
        #endregion

        #region Data Collections
        public ObservableCollection<Reservation> Reservations { get; } = new();
        public ObservableCollection<Worker> Workers { get; } = new();
        public ObservableCollection<Worker> AssignedWorkers { get; } = new();
        public ICollectionView FilteredWorkers => _filteredWorkers.View;
        #endregion

        #region Selected Reservation & Search
        private Reservation? _selectedReservation;
        public Reservation? SelectedReservation
        {
            get => _selectedReservation;
            set { _selectedReservation = value; OnPropertyChanged(); }
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilteredWorkers.Refresh();
            }
        }
        #endregion

        #region UI State
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand AssignWorkerCommand { get; }
        public ICommand RemoveAssignedWorkerCommand { get; }
        public ICommand BatchAssignCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region Constructor
        public AssignWorkersViewModel(SchedulingViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));

            AssignWorkerCommand = new RelayCommand<Worker>(ToggleAssign);
            RemoveAssignedWorkerCommand = new RelayCommand<Worker>(RemoveAssignedWorker);
            BatchAssignCommand = new RelayCommand(async () => await BatchAssignWorkers());
            CancelCommand = new RelayCommand(CloseWindow);

            _filteredWorkers.Source = Workers;
            _filteredWorkers.Filter += ApplyFilter;

            _ = LoadCompletedReservation();
        }
        #endregion

        #region Filtering
        private void ApplyFilter(object sender, FilterEventArgs e)
        {
            if (e.Item is Worker worker)
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    e.Accepted = true;
                    return;
                }

                string query = SearchText.ToLower();
                e.Accepted = (worker.Name?.ToLower().Contains(query) ?? false)
                          || (worker.Role?.ToLower().Contains(query) ?? false)
                          || (worker.Email?.ToLower().Contains(query) ?? false)
                          || (worker.Contact?.ToLower().Contains(query) ?? false);
            }
        }
        #endregion

        #region Worker Assignment Logic
        private void ToggleAssign(Worker worker)
        {
            if (worker == null) return;

           
            if (string.Equals(worker.Status, "Terminated", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(worker.Status, "On Leave", StringComparison.OrdinalIgnoreCase))
            {
                AppLogger.Info($"Cannot select worker '{worker.Name}' (ID: {worker.Id}) with status '{worker.Status}'.");
                MessageBox.Show($"Worker '{worker.Name}' is {worker.Status} and cannot be assigned.",
                                "Unavailable Worker",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (AssignedWorkers.Contains(worker))
                AssignedWorkers.Remove(worker);
            else
                AssignedWorkers.Add(worker);
        }


        private void RemoveAssignedWorker(Worker worker)
        {
            if (worker == null) return;
            AssignedWorkers.Remove(worker);
        }
        #endregion

        #region Data Loading
        private async Task LoadCompletedReservation()
        {
            try
            {
                IsLoading = true;

                var reservations = await _assignWorkerService.GetCompletedReservationsAsync();
                Reservations.Clear();
                foreach (var r in reservations) Reservations.Add(r);

                var workers = await _assignWorkerService.GetAllWorkersAsync();
                Workers.Clear();
                foreach (var w in workers) Workers.Add(w);

                FilteredWorkers.Refresh();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load data");
                ShowMessage($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Batch Assignment
        private async Task BatchAssignWorkers()
        {
            if (SelectedReservation == null)
            {
                MessageBox.Show("Please select a reservation first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AssignedWorkers.Count == 0)
            {
                MessageBox.Show("Please select at least one worker to assign.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                var emailTasks = new List<Task<bool>>();
                bool anyFailed = false;

                foreach (var worker in AssignedWorkers)
                {
                    // Validate worker before calling service
                    if (string.Equals(worker.Status, "Terminated", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(worker.Status, "On Leave", StringComparison.OrdinalIgnoreCase))
                    {
                        AppLogger.Info($"Cannot assign terminated worker '{worker.Name}' (ID: {worker.Id}).");
                        MessageBox.Show($"Worker '{worker.Name}' is terminated and cannot be assigned.",
                                        "Terminated Worker",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrEmpty(worker.Email))
                    {
                        MessageBox.Show($"Worker '{worker.Name}' does not have a valid email and cannot be notified.",
                                        "Missing Email",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        return;
                    }

                    // Assign worker
                    bool assigned = await _assignWorkerService.AssignWorkerAsync(worker, SelectedReservation);
                    if (!assigned)
                    {
                        AppLogger.Error($"Failed to assign worker {worker.Name} (ID: {worker.Id})");
                        MessageBox.Show($"Failed to assign '{worker.Name}'. Please try again.",
                                        "Assignment Failed",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                        anyFailed = true;
                        continue;
                    }

                    // Queue email sending
                    emailTasks.Add(_assignWorkerService.SendEmailAsync(worker, SelectedReservation));
                }

                // Wait for all emails to finish
                bool[] emailResults = await Task.WhenAll(emailTasks);

                for (int i = 0; i < emailResults.Length; i++)
                {
                    if (!emailResults[i])
                    {
                        var failedWorker = AssignedWorkers.ElementAt(i);
                        AppLogger.Error($"Email failed to send to {failedWorker.Name} ({failedWorker.Email})", showToUser: false);
                    }
                }

                await Task.Delay(500);
                await _parentViewModel.ReloadDataAsync();

                if (!anyFailed)
                {
                    MessageBox.Show("Workers successfully assigned and emails sent.",
                                    "Success",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                }

                CloseWindow();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "💥 Error during batch assign workers");
                MessageBox.Show($"An error occurred while assigning workers:\n{ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Window Management
        private void CloseWindow()
        {
            var win = Application.Current.Windows.OfType<AssignWorker>().FirstOrDefault();
            win?.Close();
        }
        #endregion
    }
}
