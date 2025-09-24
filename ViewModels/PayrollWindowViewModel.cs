using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class PayrollWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Reservation> Reservations { get; set; } = new();
        private Reservation? _selectedReservation;
        public Reservation? SelectedReservation
        {
            get => _selectedReservation;
            set { _selectedReservation = value; OnPropertyChanged(); }
        }

        private DateTime? _fromDate;
        public DateTime? FromDate
        {
            get => _fromDate;
            set { _fromDate = value; OnPropertyChanged(); }
        }

        private DateTime? _toDate;
        public DateTime? ToDate
        {
            get => _toDate;
            set { _toDate = value; OnPropertyChanged(); }
        }

        public RelayCommand GeneratePayrollCommand { get; }

        public PayrollWindowViewModel()
        {
            GeneratePayrollCommand = new RelayCommand(async () => await GeneratePayroll());
            LoadReservationsAsync();
        }

        private async void LoadReservationsAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var resp = await client
                    .From<Reservation>()
                    .Select("*")
                    .Get();

                Reservations.Clear();
                foreach (var r in resp.Models)
                    Reservations.Add(r);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load reservations: {ex.Message}", "Error");
            }
        }

        private async Task GeneratePayroll()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                // If a reservation is selected, use that
                if (SelectedReservation != null)
                {
                    // Perhaps fetch payroll(s) for that reservation
                    var resp = await client
                        .From<Payroll>()
                        .Select("*, workers(*)")
                        .Filter("reservation_id", Operator.Equals, SelectedReservation.Id)
                        .Get();

                    var payrolls = resp.Models;
                    if (payrolls == null || payrolls.Count == 0)
                    {
                        MessageBox.Show("No payroll records for this reservation.");
                        return;
                    }

                    // Generate PDF or report using those payrolls
                    // You can make a new PDF generator method for payroll by event
                    //PayrollByEventPdfGenerator.Generate(payrolls, SelectedReservation);
                    return;
                }

                // Otherwise, if FromDate & ToDate are set, filter payrolls in that range
                if (FromDate != null && ToDate != null)
                {
                    var resp = await client
                        .From<Payroll>()
                        .Select("*, reservations(*)")
                        .Get();

                    var payrolls = resp.Models?
                        .Where(p => p.Reservation?.EventDate >= FromDate
                                 && p.Reservation?.EventDate <= ToDate)
                        .ToList();

                    if (payrolls == null || payrolls.Count == 0)
                    {
                        MessageBox.Show("No payroll records in that date range.");
                        return;
                    }

                    //PayrollByDateRangePdfGenerator.Generate(payrolls, FromDate.Value, ToDate.Value);
                    return;
                }

                MessageBox.Show("Please select a reservation or a date range.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating payroll: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
