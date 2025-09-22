using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class PayrollViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Payroll> _payrollItems = new();  // Master list
        private ObservableCollection<Payroll> _filteredPayrollItems = new(); // Filtered view

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
        public ICommand ReloadCommand { get; }

        public PayrollViewModel()
        {
            LoadPageCommand = new RelayCommand(async () => await LoadPage());
            NextPageCommand = new RelayCommand(async () => await NextPage(), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await PrevPage(), () => CurrentPage > 1);
            ReloadCommand = new RelayCommand(async () => await LoadPage());
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
                        (p.PaidStatus != null && p.PaidStatus.ToLower().Contains(query)) ||
                        (p.WorkerId != null && p.WorkerId.ToString().Contains(query)) ||
                        (p.ReservationId != null && p.ReservationId.ToString().Contains(query))
                    ));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
