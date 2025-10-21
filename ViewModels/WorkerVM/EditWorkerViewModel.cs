/*
 * FILE: EditWorkerViewModel.cs
 * PURPOSE: Handles logic for editing an existing worker, validation, and refreshing parent WorkerViewModel.
 *
 * RESPONSIBILITIES:
 *  - Initialize properties from existing worker
 *  - Validate input fields
 *  - Save changes via WorkerService
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
    public class EditWorkerViewModel : BaseViewModel
    {
        #region Services
        private readonly WorkerService _workerService = new();
        private readonly WorkerViewModel _parentVM;
        #endregion

        #region Properties
        public Worker Worker { get; }

        private string _name;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private string _role;
        public string Role { get => _role; set { _role = value; OnPropertyChanged(); } }

        private string _contact;
        public string Contact { get => _contact; set { _contact = value; OnPropertyChanged(); } }

        private string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        private string _salary;
        public string Salary { get => _salary; set { _salary = value; OnPropertyChanged(); } }

        private DateTime? _hireDate;
        public DateTime? HireDate { get => _hireDate; set { _hireDate = value; OnPropertyChanged(); } }

        private string _status;
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region Events
        public event Action<bool>? CloseRequested;
        #endregion

        #region Constructor
        public EditWorkerViewModel(Worker existingWorker, WorkerViewModel parentVM)
        {
            Worker = existingWorker ?? throw new ArgumentNullException(nameof(existingWorker));
            _parentVM = parentVM ?? throw new ArgumentNullException(nameof(parentVM));

            // Initialize binding properties
            _name = Worker.Name ?? string.Empty;
            _role = Worker.Role ?? string.Empty;
            _contact = Worker.Contact ?? string.Empty;
            _email = Worker.Email ?? string.Empty;
            _salary = Worker.Salary?.ToString() ?? string.Empty;
            _hireDate = Worker.HireDate;
            _status = Worker.Status ?? string.Empty;

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
                    ShowMessage("Contact is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (string.IsNullOrWhiteSpace(Email) || !System.Text.RegularExpressions.Regex.IsMatch(Email, emailPattern))
                {
                    ShowMessage("A valid email address is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                long? salaryValue = null;
                if (!string.IsNullOrWhiteSpace(Salary))
                {
                    if (long.TryParse(Salary, out long parsedSalary))
                        salaryValue = parsedSalary;
                    else
                    {
                        ShowMessage("Salary must be a valid number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Apply changes
                Worker.Name = Name;
                Worker.Role = Role;
                Worker.Contact = Contact;
                Worker.Email = Email;
                Worker.Salary = salaryValue;
                Worker.HireDate = HireDate;
                Worker.Status = Status;

                // Save via service
                var updatedWorker = await _workerService.UpdateWorkerAsync(Worker);

                if (updatedWorker != null)
                {
                    // Refresh parent VM
                    await _parentVM.LoadPageAsync(_parentVM.CurrentPage);
                    CloseWindow();

                    ShowMessage("Worker updated successfully!", "Success");
                    CloseRequested?.Invoke(true);
                }
                else
                {
                    ShowMessage("Failed to update worker.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error updating worker:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex);
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
