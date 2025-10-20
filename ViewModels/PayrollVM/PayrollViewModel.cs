using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;


namespace CATERINGMANAGEMENT.ViewModels.PayrollVM
{
    public class PayrollViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Payroll> _payrollItems = new();
        private ObservableCollection<Payroll> _filteredPayrollItems = new();

        private const int PageSize = 20;
        private int _currentPage = 1;

        public ObservableCollection<Payroll> Items
        {
            get => _filteredPayrollItems;
            set { _filteredPayrollItems = value; OnPropertyChanged(); }
        }

        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

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
                ApplySearchFilter();
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand LoadPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand OpenPayslipGeneratorCommand { get; }
        public ICommand OpenPayrollGeneratorCommand { get; }
        public ICommand MarkAsPaidCommand { get; }
        public ICommand DeletePayrollCommand { get; }

        public PayrollViewModel()
        {
            LoadPageCommand = new RelayCommand(async () => await LoadPage());
            NextPageCommand = new RelayCommand(async () => await NextPage(), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await PrevPage(), () => CurrentPage > 1);
            OpenPayslipGeneratorCommand = new RelayCommand(OpenPaySlipGenerator);
            OpenPayrollGeneratorCommand = new RelayCommand(OpenPayrollGenerator);

            MarkAsPaidCommand = new RelayCommand<Payroll>(async (payroll) => await MarkAsPaidAsync(payroll));
            DeletePayrollCommand = new RelayCommand<Payroll>(async (payroll) => await DeletePayrollAsync(payroll));

        }

        public async Task LoadPage(int pageNumber = 1)
        {
            IsLoading = true;

            try
            {
                var client = await SupabaseService.GetClientAsync();

                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client
                    .From<Payroll>()
                    .Select("*, workers(*), reservations(*)")
                    .Range(from, to)
                    .Get();

                _payrollItems.Clear();

                if (response.Models != null)
                {
                    var orderedPayrolls = response.Models
                        .OrderByDescending(p => p.PaidDate ?? DateTime.MinValue);

                    foreach (var payroll in orderedPayrolls)
                    {
                        _payrollItems.Add(payroll);
                    }
                }

                TotalCount = await client
                    .From<Payroll>()
                    .Select("id")
                    .Count(CountType.Exact);

                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                CurrentPage = pageNumber;

                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payroll data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task NextPage()
        {
            if (CurrentPage < TotalPages)
                await LoadPage(CurrentPage + 1);
        }

        private async Task PrevPage()
        {
            if (CurrentPage > 1)
                await LoadPage(CurrentPage - 1);
        }

        private void OpenPaySlipGenerator()
        {
            var generatorWindow = new PayslipWindow();
            generatorWindow.ShowDialog();
        }

        private void OpenPayrollGenerator()
        {
            var generatorWindow = new PayrollWindow();
            generatorWindow.ShowDialog();
        }

        private void ApplySearchFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Items = new ObservableCollection<Payroll>(_payrollItems);
            }
            else
            {
                var query = SearchText.Trim().ToLower();

                Items = new ObservableCollection<Payroll>(
                    _payrollItems.Where(p =>
                        p.PaidStatus != null && p.PaidStatus.ToLower().Contains(query) ||
                        p.WorkerId != null && p.WorkerId.ToString().Contains(query) ||
                        p.ReservationId != null && p.ReservationId.ToString().Contains(query) ||
                        p.Worker != null && p.Worker.Name != null && p.Worker.Name.ToLower().Contains(query) ||
                        p.Reservation != null && p.Reservation.ReceiptNumber != null && p.Reservation.ReceiptNumber.ToLower().Contains(query)
                    ));
            }
        }

        private async Task MarkAsPaidAsync(Payroll payroll)
        {
            if (payroll == null) return;
            var result = MessageBox.Show("Are you sure you want to Mark as Paid this worker?", "Confirm Marking", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var client = await SupabaseService.GetClientAsync();

    
                var response = await client
                                .From<Payroll>()
                                .Where(p => p.Id == payroll.Id)
                                .Set(p => p.PaidDate, DateTime.Now)
                                .Set(p => p.PaidStatus, "Paid")
                                .Update();

                if (response.Models != null)
                {
                    var item = _payrollItems.FirstOrDefault(p => p.Id == payroll.Id);
                    if (item != null)
                    {
                        item.PaidStatus = payroll.PaidStatus;
                        item.PaidDate = payroll.PaidDate;
                    }
                    ApplySearchFilter();
                   await LoadPage(CurrentPage);
                }
                else
                {
                    MessageBox.Show("Failed to mark payroll as paid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error marking as paid:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeletePayrollAsync(Payroll payroll)
        {
            if (payroll == null) return;

            var result = MessageBox.Show("Are you sure you want to delete this payroll record?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var client = await SupabaseService.GetClientAsync();

                 await client.From<Payroll>()
                        .Where(p => p.Id == payroll.Id)
                        .Delete();

                
                    _payrollItems.Remove(payroll);
                    ApplySearchFilter();
                    await LoadPage(CurrentPage);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting payroll:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
