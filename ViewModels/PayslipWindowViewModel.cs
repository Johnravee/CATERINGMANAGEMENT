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

namespace CATERINGMANAGEMENT.ViewModels
{
    public class PayslipWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Worker> Workers { get; set; } = new();
        public ObservableCollection<string> Months { get; set; } = new();
        public ObservableCollection<int> Years { get; set; } = new();

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

        public ICommand GeneratePayslipCommand { get; }

       
        public PayslipWindowViewModel()
        {
            GeneratePayslipCommand = new RelayCommand(async () => await GeneratePayslip());

            LoadWorkersAsync();
            LoadMonthAndYear();
        }

        private async void LoadWorkersAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<Worker>().Get();
                Workers.Clear();
                foreach (var worker in response.Models)
                    Workers.Add(worker);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load users: {ex.Message}", "Error");
            }
        }

        private void LoadMonthAndYear()
        {
            Months = new ObservableCollection<string>(Enumerable.Range(1, 12)
                .Select(i => new DateTime(1, i, 1).ToString("MMMM")));
            OnPropertyChanged(nameof(Months));

            var currentYear = DateTime.Now.Year;
            Years = new ObservableCollection<int>(Enumerable.Range(currentYear - 5, 10));
            SelectedYear = currentYear;
            SelectedMonth = DateTime.Now.ToString("MMMM");
        }


        private async Task GeneratePayslip()
        {
            if (SelectedWorker == null || SelectedMonth == null || SelectedCutoff == null)
            {
                MessageBox.Show("Please complete all selections.");
                return;
            }

            int month = DateTime.ParseExact(SelectedMonth, "MMMM", null).Month;
            int year = SelectedYear;

            DateTime startDate, endDate;

            if (SelectedCutoff == "1st Cutoff")
            {
                startDate = new DateTime(year, month, 1);
                endDate = new DateTime(year, month, 15);
            }
            else
            {
                startDate = new DateTime(year, month, 16);
                endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            }

            try
            {
                var client = await SupabaseService.GetClientAsync();

                var response = await client
                    .From<Payroll>()
                    .Select("*, reservations(*)")
                    .Filter("worker_id", Operator.Equals, SelectedWorker.Id)
                    .Filter("paid_status", Operator.Equals, "Paid")
                    .Get();

                var payrolls = response.Models;

                var filtered = payrolls.Where(p =>
                    p.Reservation?.EventDate >= startDate &&
                    p.Reservation?.EventDate <= endDate
                ).ToList();

                if (!filtered.Any())
                {
                    MessageBox.Show($"No payroll records found for {SelectedWorker.Name} in the selected cutoff.");
                    return;
                }

                decimal total = filtered.Sum(p => p.GrossPay ?? 0);
                string log = $"Payroll for {SelectedWorker.Name} - {SelectedCutoff} {startDate:MMMM yyyy}\n\n";
                foreach (var p in filtered)
                {
                    log += $"- Gross Pay: {p.GrossPay:C}, Event Date: {p.Reservation?.EventDate:MMM dd, yyyy}, Reservation: {p.Reservation?.ReceiptNumber ?? "N/A"}\n";
                }
                log += $"\nTotal: {total:C}";

                UserPayslipPdfGenerator.Generate(filtered, SelectedWorker.Name!, startDate, endDate);
                MessageBox.Show(log, "Payroll Summary");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
