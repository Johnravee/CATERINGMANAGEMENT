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

namespace CATERINGMANAGEMENT.ViewModels.PayrollVM
{
    public class PayrollWindowViewModel : INotifyPropertyChanged
    {
       private  Supabase.Client? _client;

        public ObservableCollection<Reservation> Reservations { get; } = new();
        public ObservableCollection<Payroll> Payrolls { get; } = new();

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
                    _ = LoadPayrollsAsync(); // Async fire-and-forget
                }
            }
        }

        public ICommand GeneratePayrollCommand { get; }

        public PayrollWindowViewModel()
        {
            GeneratePayrollCommand = new RelayCommand(async () => await GeneratePayrollAsync());

            // Async constructor pattern
            Task.Run(async () =>
            {
                _client = await SupabaseService.GetClientAsync();
                await LoadReservationsAsync();
            });
        }

        private async Task LoadReservationsAsync()
        {
            try
            {
                var client = _client ?? await SupabaseService.GetClientAsync();

                var response = await client
                    .From<Reservation>()
                    .Get();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Reservations.Clear();
                    foreach (var res in response.Models)
                        Reservations.Add(res);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load reservations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPayrollsAsync()
        {
            if (SelectedReservation == null)
            {
                Payrolls.Clear();
                return;
            }

            try
            {
                var client = _client ?? await SupabaseService.GetClientAsync();

                var response = await client
                    .From<Payroll>()
                    .Select("*, workers(*)")
                    .Filter("reservation_id", Operator.Equals, SelectedReservation.Id)
                    .Get();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Payrolls.Clear();
                    foreach (var payroll in response.Models)
                        Payrolls.Add(payroll);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load payroll data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GeneratePayrollAsync()
        {
            if (SelectedReservation == null)
            {
                MessageBox.Show("Please select a reservation.", "Missing Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Payrolls.Count == 0)
            {
                MessageBox.Show("No payroll data found for the selected reservation.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                 PayrollPdfGenerator.Generate(
                     Payrolls.ToList(),
                    SelectedReservation.ReceiptNumber,
                    SelectedReservation.EventDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate payroll report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}
