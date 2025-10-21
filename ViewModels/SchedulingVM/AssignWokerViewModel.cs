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

            _ = LoadData();
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
        private async Task LoadData()
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
            if (SelectedReservation == null || AssignedWorkers.Count == 0)
            {
                ShowMessage("Please select a reservation and at least one worker.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                var emailTasks = new List<Task<bool>>();
                bool anyFailed = false;

                foreach (var worker in AssignedWorkers)
                {
                    bool assigned = await _assignWorkerService.AssignWorkerAsync(worker, SelectedReservation);
                    if (!assigned)
                    {
                        AppLogger.Error($"Failed to assign worker {worker.Name} (ID: {worker.Id})");
                        ShowMessage($"Failed to assign {worker.Name}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        anyFailed = true;
                        continue;
                    }

                    emailTasks.Add(_assignWorkerService.SendEmailAsync(worker, SelectedReservation));
                }

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
                    ShowMessage("Workers successfully assigned and emails sent.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                CloseWindow();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error during batch assign workers");
                ShowMessage($"Error assigning workers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
