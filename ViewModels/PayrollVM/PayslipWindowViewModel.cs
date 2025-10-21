/*
 * FILE: PayslipWindowViewModel.cs
 * PURPOSE: ViewModel for the PayslipWindow.
 *          Handles loading workers, selecting month/year/cutoff,
 *          fetching payrolls for a specific worker and cutoff period,
 *          and generating individual payslip PDFs.
 *
 * RESPONSIBILITIES:
 *  - Load all workers from the PayrollService
 *  - Provide month, year, and cutoff selection
 *  - Fetch payrolls for the selected worker and period
 *  - Generate PDF payslips using UserPayslipPdfGenerator
 *  - Show messages for errors or missing data
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.DocumentsGenerator;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.PayrollVM
{
    public class PayslipWindowViewModel : BaseViewModel
    {
        #region Services
        private readonly PayrollService _payrollService = new();
        #endregion

        #region Collections
        public ObservableCollection<Worker> Workers { get; } = new();
        public ObservableCollection<string> Months { get; } = new();
        public ObservableCollection<int> Years { get; } = new();
        public ObservableCollection<string> CutoffOptions { get; } = new ObservableCollection<string> { "1", "2" };
        #endregion

        #region Selected Items
        private Worker? _selectedWorker;
        public Worker? SelectedWorker { get => _selectedWorker; set { _selectedWorker = value; OnPropertyChanged(); } }

        private string? _selectedMonth;
        public string? SelectedMonth { get => _selectedMonth; set { _selectedMonth = value; OnPropertyChanged(); } }

        private int _selectedYear;
        public int SelectedYear { get => _selectedYear; set { _selectedYear = value; OnPropertyChanged(); } }

        private string? _selectedCutoff;
        public string? SelectedCutoff { get => _selectedCutoff; set { _selectedCutoff = value; OnPropertyChanged(); } }
        #endregion

        #region UI State
        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        #endregion

        #region Commands
        public ICommand GeneratePayslipCommand { get; }
        #endregion

        #region Constructor
        public PayslipWindowViewModel()
        {
            GeneratePayslipCommand = new RelayCommand(async () => await GeneratePayslipAsync(), () => !IsBusy);
            LoadMonthAndYear();
            _ = InitializeAsync();
        }
        #endregion

        #region Initialization
        private async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                var workers = await _payrollService.GetAllWorkersAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Workers.Clear();
                    foreach (var worker in workers)
                        Workers.Add(worker);
                });
            }
            catch (Exception ex)
            {
                ShowMessage($"Failed to load workers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }
        #endregion

        #region Helpers
        private void LoadMonthAndYear()
        {
            Months.Clear();
            foreach (var monthName in Enumerable.Range(1, 12)
                .Select(i => new DateTime(1, i, 1).ToString("MMMM", CultureInfo.InvariantCulture)))
                Months.Add(monthName);

            var currentYear = DateTime.Now.Year;
            Years.Clear();
            foreach (var y in Enumerable.Range(currentYear - 5, 11)) Years.Add(y);

            SelectedYear = currentYear;
            SelectedMonth = DateTime.Now.ToString("MMMM", CultureInfo.InvariantCulture);
        }
        #endregion

        #region Payslip Generation
        private async Task GeneratePayslipAsync()
        {
            if (IsBusy) return;
            if (SelectedWorker == null || SelectedMonth == null || SelectedCutoff == null)
            {
                ShowMessage("Please complete all selections.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                int month = DateTime.ParseExact(SelectedMonth, "MMMM", CultureInfo.InvariantCulture).Month;
                int year = SelectedYear;

                // Determine the start and end dates based on cutoff
                DateTime startDate = SelectedCutoff == "1" ? new DateTime(year, month, 1) : new DateTime(year, month, 16);
                DateTime endDate = SelectedCutoff == "1" ? new DateTime(year, month, 15) : new DateTime(year, month, DateTime.DaysInMonth(year, month));

                var payrolls = await _payrollService.GetPayrollsByWorkerAsync(SelectedWorker.Id, startDate, endDate);

                if (!payrolls.Any())
                {
                    ShowMessage($"No payroll records found for {SelectedWorker.Name} in the selected cutoff.", "No Data");
                    return;
                }

                UserPayslipPdfGenerator.Generate(payrolls, SelectedWorker.Name!, startDate, endDate);
            }
            catch (Exception ex)
            {
                ShowMessage($"Error generating payslip: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }
        #endregion
    }
}
