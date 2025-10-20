using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.SchedulingVM
{
    /// <summary>
    /// ViewModel for editing an existing grouped schedule and managing assigned workers.
    /// </summary>
    public class EditScheduleViewModel : BaseViewModel
    {
        private readonly SchedulingService _schedulingService = new();
        private readonly SchedulingViewModel _parentViewModel;

        public GroupedScheduleView GroupedSchedule { get; }

        public ObservableCollection<Worker> AssignedWorkers { get; } = new();

        public ICommand RemoveWorkerCommand { get; }
        public ICommand CloseCommand { get; }

        public EditScheduleViewModel(GroupedScheduleView groupedSchedule, SchedulingViewModel parentViewModel)
        {
            GroupedSchedule = groupedSchedule ?? throw new ArgumentNullException(nameof(groupedSchedule));
            _parentViewModel = parentViewModel;

            // Parse assigned workers (assuming comma-separated string from view)
            if (!string.IsNullOrEmpty(groupedSchedule.AssignedWorkers))
            {
                var workerNames = groupedSchedule.AssignedWorkers.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in workerNames)
                {
                    AssignedWorkers.Add(new Worker { Name = name.Trim() });
                }
            }

            RemoveWorkerCommand = new RelayCommand<Worker>(async w => await RemoveWorkerAsync(w));
            CloseCommand = new RelayCommand(CloseWindow);
        }

        private async Task RemoveWorkerAsync(Worker worker)
        {
            if (worker == null)
                return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to remove {worker.Name} from this schedule?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            bool success = await _schedulingService.RemoveWorkerFromScheduleAsync(GroupedSchedule.ReservationId, worker.Id);

            if (success)
            {
                AssignedWorkers.Remove(worker);
                AppLogger.Info($"Removed worker {worker.Name} from reservation {GroupedSchedule.ReservationId}");
                MessageBox.Show("✅ Worker removed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh parent view
                await _parentViewModel.ReloadDataAsync();
            }
            else
            {
                MessageBox.Show("❌ Failed to remove worker.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = true;
                    window.Close();
                    break;
                }
            }
        }
    }
}
