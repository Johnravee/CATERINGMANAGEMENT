using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.SchedulingVM
{
    public class EditScheduleViewModel : BaseViewModel
    {
        private readonly SchedulingService _schedulingService = new();
        private readonly SchedulingViewModel _parentViewModel;
        public GroupSchedule GroupSchedule { get; }

        public ObservableCollection<Worker> AssignedWorkers { get; } = new();

        public ICommand RemoveWorkerCommand { get; }
        public ICommand CloseCommand { get; }

        public EditScheduleViewModel(GroupSchedule groupSchedule, SchedulingViewModel parentViewModel)
        {
            GroupSchedule = groupSchedule ?? throw new ArgumentNullException(nameof(groupSchedule));
            _parentViewModel = parentViewModel;

            foreach (var worker in groupSchedule.Workers)
                AssignedWorkers.Add(worker);

            RemoveWorkerCommand = new RelayCommand<Worker>(async w => await RemoveWorkerAsync(w));
            CloseCommand = new RelayCommand(CloseWindow);
        }

        private async Task RemoveWorkerAsync(Worker worker)
        {
            //if (worker == null || GroupSchedule.Reservation == null) return;

            //var schedules = await _schedulingService.GetSchedulesByReservationId(GroupSchedule.Reservation.Id);
            //var schedule = schedules.FirstOrDefault(s => s.WorkerId == worker.Id);

            //if (schedule != null)
            //{
            //    var result = await _schedulingService.RemoveWorkerFromScheduleAsync(schedule.Id, worker.Id);
            //    if (result)
            //    {
            //        AssignedWorkers.Remove(worker);
            //        ShowMessage("Worker removed from schedule.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            //    }
            //    else
            //    {
            //        ShowMessage("Failed to remove worker.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
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
