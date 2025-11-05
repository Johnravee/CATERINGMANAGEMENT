/*
 * FILE: EditScheduleViewModel.cs
 * PURPOSE: ViewModel for managing and removing assigned workers from a reservation.
 * 
 * RESPONSIBILITIES:
 *  - Parse assigned workers from GroupedSchedule
 *  - Remove workers from a reservation
 *  - Update parent SchedulingViewModel after changes
 *  - Provide commands for UI interaction
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.Mailer;
using CATERINGMANAGEMENT.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace CATERINGMANAGEMENT.ViewModels.SchedulingVM
{
    public class EditScheduleViewModel : BaseViewModel
    {
        #region Services
        private readonly SchedulingService _schedulingService = new();
        private readonly SchedulingViewModel _parentViewModel;
        private readonly EmailService _emailService = new();
        private readonly RemoveWorkerMailer _removeWorkerMailer;
        #endregion

        #region Data
        public GroupedScheduleView GroupedSchedule { get; }
        public ObservableCollection<Worker> AssignedWorkers { get; } = new();
        #endregion

        #region UI State
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand RemoveWorkerCommand { get; }
        public ICommand CloseCommand { get; }
        #endregion

        #region Constructor
        public EditScheduleViewModel(GroupedScheduleView groupedSchedule, SchedulingViewModel parentViewModel)
        {
            GroupedSchedule = groupedSchedule ?? throw new ArgumentNullException(nameof(groupedSchedule));
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));

            _removeWorkerMailer = new RemoveWorkerMailer(_emailService);

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
        #endregion

        #region Private Methods
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

        private async Task<Worker?> FetchWorkerDetailsAsync(int workerId)
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<Worker>()
                                           .Where(w => w.Id == workerId)
                                           .Get();
                return response.Models?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Failed to fetch worker details for Id {workerId}");
                return null;
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

            // Capture event info up-front
            string eventName = GroupedSchedule.PackageName ?? "Event";
            string eventDate = GroupedSchedule.EventDate.ToString("MMMM dd, yyyy");
            string eventVenue = GroupedSchedule.Venue ?? "Venue";

            IsBusy = true;
            try
            {
                // Prefetch missing worker details before removal/reload
                if (string.IsNullOrWhiteSpace(worker.Email) || string.IsNullOrWhiteSpace(worker.Role) || string.IsNullOrWhiteSpace(worker.Name))
                {
                    var full = await FetchWorkerDetailsAsync(worker.Id);
                    if (full != null)
                    {
                        worker.Email = string.IsNullOrWhiteSpace(worker.Email) ? full.Email : worker.Email;
                        worker.Role = string.IsNullOrWhiteSpace(worker.Role) ? full.Role : worker.Role;
                        worker.Name = string.IsNullOrWhiteSpace(worker.Name) ? full.Name : worker.Name;
                    }
                }

                bool success = await _schedulingService.RemoveWorkerFromScheduleAsync(GroupedSchedule.ReservationId, worker.Id);

                if (success)
                {
                    AssignedWorkers.Remove(worker);
                    AppLogger.Success($"Removed worker '{worker.Name}' from reservation {GroupedSchedule.ReservationId}");

                    // Send email BEFORE parent reload
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(worker.Email))
                        {
                            bool emailed = await _removeWorkerMailer.SendWorkerRemovalEmailAsync(
                                worker.Email,
                                worker.Name ?? "Staff",
                                worker.Role ?? "Staff",
                                eventName,
                                eventDate,
                                eventVenue
                            );

                            if (emailed)
                                AppLogger.Success($"Removal notification email sent to {worker.Email}");
                            else
                                AppLogger.Error($"Failed to send removal email to {worker.Email}", showToUser: false);
                        }
                        else
                        {
                            AppLogger.Info($"No email available for worker {worker.Name}; skipping notification.");
                        }
                    }
                    catch (Exception mailEx)
                    {
                        AppLogger.Error(mailEx, "Error sending worker removal email", showToUser: false);
                    }

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
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
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
        #endregion
    }
}
