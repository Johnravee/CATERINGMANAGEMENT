using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;
using System.Globalization;


namespace CATERINGMANAGEMENT.ViewModels.PayrollVM
{
    public class PayslipWindowViewModel : INotifyPropertyChanged
    {
        private Supabase.Client? _client;

        public ObservableCollection<Worker> Workers { get; } = new();
        public ObservableCollection<string> Months { get; } = new();
        public ObservableCollection<int> Years { get; } = new();
        public ObservableCollection<string> CutoffOptions { get; } = new ObservableCollection<string> { "1", "2" };


        private Worker? _selectedWorker;
        public Worker? SelectedWorker
        {
            get => _selectedWorker;
            set { _selectedWorker = value; OnPropertyChanged(); }
        }

        private string? _selectedMonth;
        public string? SelectedMonth
        {
            get => _selectedMonth;
            set { _selectedMonth = value; OnPropertyChanged(); }
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); }
        }

        private string? _selectedCutoff;
        public string? SelectedCutoff
        {
            get => _selectedCutoff;
            set { _selectedCutoff = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand GeneratePayslipCommand { get; }

        public PayslipWindowViewModel()
        {
            GeneratePayslipCommand = new RelayCommand(async () => await GeneratePayslipAsync(), () => !IsBusy);

            InitializeAsync();
            LoadMonthAndYear();
        }

        private async void InitializeAsync()
        {
            IsBusy = true;
            try
            {
                _client = await SupabaseService.GetClientAsync();
                await LoadWorkersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadWorkersAsync()
        {
            if (_client == null) return;

            try
            {
                var response = await _client.From<Worker>().Get();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Workers.Clear();
                    foreach (var worker in response.Models)
                        Workers.Add(worker);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load workers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMonthAndYear()
        {
            Months.Clear();
            foreach (var monthName in Enumerable.Range(1, 12)
                .Select(i => new DateTime(1, i, 1).ToString("MMMM", CultureInfo.InvariantCulture)))
            {
                Months.Add(monthName);
            }

            var currentYear = DateTime.Now.Year;
            Years.Clear();
            foreach (var y in Enumerable.Range(currentYear - 5, 11)) // 11 years total
                Years.Add(y);

            SelectedYear = currentYear;
            SelectedMonth = DateTime.Now.ToString("MMMM", CultureInfo.InvariantCulture);
        }

        private async Task GeneratePayslipAsync()
        {
            if (IsBusy) return;

            if (SelectedWorker == null || SelectedMonth == null || SelectedCutoff == null)
            {
                MessageBox.Show("Please complete all selections.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                _client ??= await SupabaseService.GetClientAsync();

                int month = DateTime.ParseExact(SelectedMonth, "MMMM", CultureInfo.InvariantCulture).Month;
                int year = SelectedYear;

                DateTime startDate, endDate;

                if (SelectedCutoff == "1")
                {
                    startDate = new DateTime(year, month, 1);
                    endDate = new DateTime(year, month, 15);
                }
                else if (SelectedCutoff == "2")
                {
                    startDate = new DateTime(year, month, 16);
                    endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                }
                else
                {
                    MessageBox.Show("Invalid cutoff selected.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var response = await _client
                    .From<Payroll>()
                    .Select("*, reservations(*)")
                    .Filter("worker_id", Operator.Equals, SelectedWorker.Id)
                    .Filter("paid_status", Operator.Equals, "Paid")
                    .Get();

                var payrolls = response.Models;

                var filtered = payrolls
                    .Where(p => p.Reservation != null &&
                                p.Reservation.EventDate >= startDate &&
                                p.Reservation.EventDate <= endDate)
                    .ToList();

                if (!filtered.Any())
                {
                    MessageBox.Show($"No payroll records found for {SelectedWorker.Name} in the selected cutoff.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                UserPayslipPdfGenerator.Generate(filtered, SelectedWorker.Name!, startDate, endDate);

     
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating payslip: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}
