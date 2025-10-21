/*
 * FILE: AddWorkerViewModel.cs
 * PURPOSE: Handles logic for adding a new worker, validation, and refreshing parent WorkerViewModel.
 *
 * RESPONSIBILITIES:
 *  - Initialize properties for a new worker
 *  - Validate input fields
 *  - Insert new worker via WorkerService
 *  - Refresh parent WorkerViewModel
 *  - Close window on completion or cancellation
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using System;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.WorkerVM
{
    public class AddWorkerViewModel : BaseViewModel
    {
        #region Services
        private readonly WorkerService _workerService;
        private readonly WorkerViewModel _parentViewModel;
        #endregion

        #region Properties
        private string _name = string.Empty;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private string _role = string.Empty;
        public string Role { get => _role; set { _role = value; OnPropertyChanged(); } }

        private string _contact = string.Empty;
        public string Contact { get => _contact; set { _contact = value; OnPropertyChanged(); } }

        private string _email = string.Empty;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        private string _salary = string.Empty;
        public string Salary { get => _salary; set { _salary = value; OnPropertyChanged(); } }

        private DateTime? _hireDate = DateTime.UtcNow;
        public DateTime? HireDate { get => _hireDate; set { _hireDate = value; OnPropertyChanged(); } }

        private string _status = "Active";
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region Constructor
        public AddWorkerViewModel(WorkerViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));
            _workerService = new WorkerService();

            SaveCommand = new RelayCommand(async () => await SaveAsync());
            CancelCommand = new RelayCommand(CloseWindow);
        }
        #endregion

        #region Save Worker
        private async Task SaveAsync()
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(Name))
                {
                    ShowMessage("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(Role))
                {
                    ShowMessage("Role is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(Contact))
                {
                    ShowMessage("Contact number is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!ValidationHelper.IsValidEmail(Email))
                {
                    ShowMessage("A valid email address is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(Salary) && !decimal.TryParse(Salary, out _))
                {
                    ShowMessage("Salary must be a valid number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create Worker object
                var newWorker = new Worker
                {
                    Name = Name.Trim(),
                    Role = Role.Trim(),
                    Contact = Contact.Trim(),
                    Email = Email.Trim(),
                    Salary = string.IsNullOrWhiteSpace(Salary) ? 0 : (long?)decimal.Parse(Salary),
                    HireDate = HireDate ?? DateTime.UtcNow,
                    Status = Status
                };

                AppLogger.Info("Attempting to insert new worker...");

                var inserted = await _workerService.InsertWorkerAsync(newWorker);

                if (inserted != null)
                {
                    AppLogger.Success($"Inserted worker: {inserted.Id} - {inserted.Name}");
                    ShowMessage("Worker added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    _workerService.InvalidateExportCache();
                    await _parentViewModel.LoadPageAsync(1);
                    CloseWindow();
                }
                else
                {
                    AppLogger.Error("InsertWorkerAsync returned null.");
                    ShowMessage("Failed to add worker.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error while adding worker.");
                ShowMessage($"Unexpected error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Close Window
        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }
        #endregion
    }
}
