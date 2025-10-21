/*
 * FILE: PayrollViewModel.cs
 * PURPOSE: ViewModel for managing payroll data.
 *          Handles pagination, searching (with debounce), marking as paid, deleting records,
 *          and opening payslip/payroll generator windows.
 * 
 * RESPONSIBILITIES:
 *  - Load paginated payroll records from PayrollService
 *  - Apply search filter with debounce
 *  - Track current page and total pages
 *  - Mark payrolls as paid
 *  - Delete payroll records
 *  - Open Payslip and Payroll generator windows
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.PayrollVM
{
    public class PayrollViewModel : BaseViewModel
    {
        #region Services
        private readonly PayrollService _payrollService = new();
        #endregion

        #region Collections
        private ObservableCollection<Payroll> _payrollItems = new();
        private ObservableCollection<Payroll> _filteredPayrollItems = new();
        public ObservableCollection<Payroll> Items
        {
            get => _filteredPayrollItems;
            set { _filteredPayrollItems = value; OnPropertyChanged(); }
        }
        #endregion

        #region Pagination & UI State
        private const int PageSize = 20;

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); }
        }

        public int TotalCount { get; set; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _ = ApplySearchDebouncedAsync();
            }
        }

        private CancellationTokenSource? _searchDebounceToken;
        #endregion

        #region Commands
        public ICommand LoadPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand OpenPayslipGeneratorCommand { get; }
        public ICommand OpenPayrollGeneratorCommand { get; }
        public ICommand MarkAsPaidCommand { get; }
        public ICommand DeletePayrollCommand { get; }
        #endregion

        #region Constructor
        public PayrollViewModel()
        {
            LoadPageCommand = new RelayCommand(async () => await LoadPageAsync(1));
            NextPageCommand = new RelayCommand(async () => await LoadPageAsync(CurrentPage + 1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await LoadPageAsync(CurrentPage - 1), () => CurrentPage > 1);
            OpenPayslipGeneratorCommand = new RelayCommand(OpenPayslipGenerator);
            OpenPayrollGeneratorCommand = new RelayCommand(OpenPayrollGenerator);
            MarkAsPaidCommand = new RelayCommand<Payroll>(async p => await MarkAsPaidAsync(p));
            DeletePayrollCommand = new RelayCommand<Payroll>(async p => await DeletePayrollAsync(p));

            _ = LoadPageAsync(1);
        }
        #endregion

        #region Data Loading
        public async Task LoadPageAsync(int pageNumber)
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var (records, totalCount) = await _payrollService.GetPayrollPageAsync(pageNumber);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _payrollItems.Clear();
                    foreach (var p in records.OrderByDescending(x => x.PaidDate ?? DateTime.MinValue))
                        _payrollItems.Add(p);
                });

                TotalCount = totalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                CurrentPage = Math.Max(1, Math.Min(pageNumber, TotalPages == 0 ? 1 : TotalPages));

                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                ShowMessage($"Error loading payroll data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Search
        private async Task ApplySearchDebouncedAsync()
        {
            _searchDebounceToken?.Cancel();
            var cts = new CancellationTokenSource();
            _searchDebounceToken = cts;

            try
            {
                await Task.Delay(400, cts.Token);
                ApplySearchFilter();
            }
            catch (TaskCanceledException) { }
        }

        private void ApplySearchFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Items = new ObservableCollection<Payroll>(_payrollItems);
            }
            else
            {
                string query = SearchText.Trim().ToLower();
                Items = new ObservableCollection<Payroll>(_payrollItems.Where(p =>
                    (p.PaidStatus?.ToLower().Contains(query) ?? false) ||
                    (p.WorkerId.ToString().Contains(query)) ||
                    (p.ReservationId.ToString().Contains(query)) ||
                    (p.Worker?.Name?.ToLower().Contains(query) ?? false) ||
                    (p.Reservation?.ReceiptNumber?.ToLower().Contains(query) ?? false)
                ));
            }
        }
        #endregion

        #region Data Manipulation
        private async Task MarkAsPaidAsync(Payroll payroll)
        {
            if (payroll == null) return;

            var result = MessageBox.Show("Are you sure you want to mark this payroll as paid?", "Confirm Marking", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            if (await _payrollService.MarkAsPaidAsync(payroll))
            {
                await LoadPageAsync(CurrentPage);
            }
            else
            {
                ShowMessage("Failed to mark payroll as paid.", "Error");
            }
        }

        private async Task DeletePayrollAsync(Payroll payroll)
        {
            if (payroll == null) return;

            var result = MessageBox.Show("Are you sure you want to delete this payroll record?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            if (await _payrollService.DeletePayrollAsync(payroll))
            {
                await LoadPageAsync(CurrentPage);
            }
            else
            {
                ShowMessage("Error deleting payroll.", "Error");
            }
        }
        #endregion

        #region Window Operations
        private void OpenPayslipGenerator() => new PayslipWindow().ShowDialog();
        private void OpenPayrollGenerator() => new PayrollWindow().ShowDialog();
        #endregion
    }
}
