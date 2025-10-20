// ViewModel for managing and removing assigned workers from a reservation, with logging and UI commands.

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.ViewModels.SchedulingVM
{
    public class EditScheduleViewModel : BaseViewModel
    {
        // Services
        private readonly SchedulingService _schedulingService = new();
        private readonly SchedulingViewModel _parentViewModel;

        // Data
        public GroupedScheduleView GroupedSchedule { get; }
        public ObservableCollection<Worker> AssignedWorkers { get; } = new();

        // Commands
        public ICommand RemoveWorkerCommand { get; }
        public ICommand CloseCommand { get; }

        public EditScheduleViewModel(GroupedScheduleView groupedSchedule, SchedulingViewModel parentViewModel)
        {
            GroupedSchedule = groupedSchedule ?? throw new ArgumentNullException(nameof(groupedSchedule));
            _parentViewModel = parentViewModel;

            try
            {
                ParseAssignedWorkers();
                AppLogger.Info($"Parsed {AssignedWorkers.Count} assigned workers for reservation {GroupedSchedule.ReservationId}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Failed to parse assigned workers for reservation {GroupedSchedule.ReservationId}");
            }

            RemoveWorkerCommand = new RelayCommand<Worker>(async w => await RemoveWorkerAsync(w));
            CloseCommand = new RelayCommand(CloseWindow);
        }

        // Public methods
        private void ParseAssignedWorkers()
        {
            if (string.IsNullOrEmpty(GroupedSchedule.AssignedWorkers) ||
                string.IsNullOrEmpty(GroupedSchedule.AssignedWorkerIds))
                return;

            var names = GroupedSchedule.AssignedWorkers.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var ids = GroupedSchedule.AssignedWorkerIds.Split(',', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < Math.Min(names.Length, ids.Length); i++)
            {
                if (long.TryParse(ids[i].Trim(), out long workerId))
                {
                    AssignedWorkers.Add(new Worker
                    {
                        Id = (int)workerId,
                        Name = names[i].Trim()
                    });
                }
            }
        }

        private async Task RemoveWorkerAsync(Worker worker)
        {
            if (worker == null) return;

            var confirm = MessageBox.Show(
                $"Remove {worker.Name} from this reservation?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool success = await _schedulingService.RemoveWorkerFromScheduleAsync(GroupedSchedule.ReservationId, worker.Id);

                if (success)
                {
                    AssignedWorkers.Remove(worker);
                    AppLogger.Success($"Removed worker '{worker.Name}' from reservation {GroupedSchedule.ReservationId}");

                    await _parentViewModel.ReloadDataAsync();

                    MessageBox.Show("Worker removed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AppLogger.Error($"Failed to remove worker '{worker.Name}' from reservation {GroupedSchedule.ReservationId}", showToUser: false);
                    MessageBox.Show("Failed to remove worker.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to remove worker from schedule.");
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
