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
                AppLogger.Info($"Loading menu orders for reservation ID {Reservation.Id}");

                var orders = await _reservationService.GetReservationMenuOrdersAsync(Reservation.Id);

                App.Current.Dispatcher.Invoke(() =>
                {
                    ReservationMenuOrders.Clear();
                    foreach (var order in orders)
                    {
                        ReservationMenuOrders.Add(order);
                    }
                });

                AppLogger.Success($"Loaded {orders.Count} menu orders for reservation ID {Reservation.Id}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load menu orders: {ex.Message}", showToUser: true);
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
                AppLogger.Info($"Updating reservation ID {Reservation.Id}");

                var updated = await _reservationService.UpdateReservationAsync(Reservation);

                if (updated != null)
                {
                    await LoadReservationMenuOrdersAsync();
                    AppLogger.Success($"Reservation ID {Reservation.Id} updated successfully.");
                    MessageBox.Show("Reservation updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AppLogger.Error($"Reservation update returned null for ID {Reservation.Id}", showToUser: true);
                    MessageBox.Show("Failed to update reservation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error updating reservation: {ex.Message}", showToUser: true);
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
                AppLogger.Info($"Generating contract for reservation ID {Reservation.Id}");

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

                AppLogger.Success($"Contract generated and emailed for reservation ID {Reservation.Id}");

            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error generating/sending contract: {ex.Message}", showToUser: true);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
