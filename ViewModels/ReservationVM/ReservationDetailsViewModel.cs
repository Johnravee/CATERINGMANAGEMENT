using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Mailer;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.ReservationVM
{
    public class ReservationDetailsViewModel : INotifyPropertyChanged
    {
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public Reservation Reservation { get; }

        public ObservableCollection<ReservationMenuOrder> ReservationMenuOrders { get; } = new();
        public ObservableCollection<MenuOption> MenuOptions { get; } = new();

        private ReservationMenuOrder? _selectedMenuOrder;
        public ReservationMenuOrder? SelectedMenuOrder
        {
            get => _selectedMenuOrder;
            set
            {
                _selectedMenuOrder = value;
                OnPropertyChanged();
            }
        }

        public ICommand GenerateContractCommand { get; }
        public ICommand UpdateReservationCommand { get; }


        private readonly EmailService _emailService;
        private readonly ContractMailer _contractMailer;
        private readonly ReservationService _reservationService = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ReservationDetailsViewModel(Reservation reservation)
        {
            Reservation = reservation ?? throw new ArgumentNullException(nameof(reservation));

            _emailService = new EmailService();
            _contractMailer = new ContractMailer(_emailService);

            GenerateContractCommand = new RelayCommand(async () => await GenerateContractAsync());
            UpdateReservationCommand = new RelayCommand(async () => await UpdateReservationAsync());
            Task.Run(LoadReservationMenuOrdersAsync);
     
        }

        private async Task LoadReservationMenuOrdersAsync()
        {
            try
            {
                IsLoading = true;

                var orders = await _reservationService.GetReservationMenuOrdersAsync(Reservation.Id);

                App.Current.Dispatcher.Invoke(() =>
                {
                    ReservationMenuOrders.Clear();
                    foreach (var order in orders)
                    {
                        ReservationMenuOrders.Add(order);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load menu orders.\n\n{ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

       

        private async Task UpdateReservationAsync()
        {
            try
            {
                IsLoading = true;
                var updated = await _reservationService.UpdateReservationAsync(Reservation);

                if (updated != null)
                    await LoadReservationMenuOrdersAsync();
                    MessageBox.Show("Reservation updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update reservation.\n\n{ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GenerateContractAsync()
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Save Contract PDF",
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Contract_{Reservation.ReceiptNumber}.pdf"
            };

            if (saveDialog.ShowDialog() != true)
                return;

            try
            {
                IsLoading = true;

                await Task.Run(async () =>
                {
                    ContractPdfGenerator.Generate(Reservation, saveDialog.FileName);

                    bool sent = await _contractMailer.SendContractEmailAsync(
                        Reservation.Profile?.Email ?? string.Empty,
                        Reservation.Profile?.FullName ?? "Client",
                        Reservation.EventDate.ToString("MMMM dd, yyyy"),
                        saveDialog.FileName
                    );

                    if (!sent)
                        throw new Exception("Failed to send the contract email.");
                });

                MessageBox.Show("Contract generated and sent successfully!", "Success");

                var result = MessageBox.Show("Do you want to print the contract now?",
                    "Print Contract", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error occurred.\n\n{ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

       

       

          
        
    }
}
