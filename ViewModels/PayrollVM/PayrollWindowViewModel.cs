/*
 * FILE: PayrollWindowViewModel.cs
 * PURPOSE: ViewModel for the PayrollWindow. 
 *          Handles loading reservations, fetching payroll data for the selected reservation,
 *          and generating payroll reports in PDF format.
 *          
 * RESPONSIBILITIES:
 *  - Load all reservations from the PayrollService
 *  - Track the selected reservation
 *  - Load payroll records for the selected reservation
 *  - Generate payroll PDF reports
 *  - Display messages to the user for errors or missing data
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.DocumentsGenerator;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.PayrollVM
{
    public class PayrollWindowViewModel : BaseViewModel
    {
        #region Services
        private readonly PayrollService _payrollService = new();
        #endregion

        #region Collections
        public ObservableCollection<Reservation> Reservations { get; } = new();
        public ObservableCollection<Payroll> Payrolls { get; } = new();
        #endregion

        #region Selected Items
        private Reservation? _selectedReservation;
        public Reservation? SelectedReservation
        {
            get => _selectedReservation;
            set
            {
                if (_selectedReservation != value)
                {
                    _selectedReservation = value;
                    OnPropertyChanged();
                    _ = LoadPayrollsAsync();
                }
            }
        }
        #endregion

        #region Commands
        public ICommand GeneratePayrollCommand { get; }
        #endregion

        #region Constructor
        public PayrollWindowViewModel()
        {
            GeneratePayrollCommand = new RelayCommand(async () => await GeneratePayrollAsync());
            _ = InitializeAsync();
        }
        #endregion

        #region Initialization
        private async Task InitializeAsync()
        {
            try
            {
                var reservations = await _payrollService.GetAllReservationsAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Reservations.Clear();
                    foreach (var res in reservations)
                        Reservations.Add(res);
                });
            }
            catch (System.Exception ex)
            {
                ShowMessage($"Failed to load reservations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Payroll Loading
        private async Task LoadPayrollsAsync()
        {
            if (SelectedReservation == null)
            {
                Payrolls.Clear();
                return;
            }

            try
            {
                var payrolls = await _payrollService.GetPayrollsByReservationAsync(SelectedReservation.Id);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Payrolls.Clear();
                    foreach (var payroll in payrolls)
                        Payrolls.Add(payroll);
                });
            }
            catch (System.Exception ex)
            {
                ShowMessage($"Failed to load payroll data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Payroll Generation
        private Task GeneratePayrollAsync()
        {
            if (SelectedReservation == null)
            {
                ShowMessage("Please select a reservation.", "Missing Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return Task.CompletedTask;
            }

            if (Payrolls.Count == 0)
            {
                ShowMessage("No payroll data found for the selected reservation.", "No Data");
                return Task.CompletedTask;
            }

            try
            {
                PayrollPdfGenerator.Generate(Payrolls.ToList(), SelectedReservation.ReceiptNumber, SelectedReservation.EventDate);
            }
            catch (System.Exception ex)
            {
                ShowMessage($"Failed to generate payroll report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return Task.CompletedTask;
        }
        #endregion
    }
}
