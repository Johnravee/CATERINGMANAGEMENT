/*
* FILE: ReservationDetailsViewModel.cs
* PURPOSE: Handles reservation details, including menu orders, updates, and contract PDF generation/emailing.
*
* RESPONSIBILITIES:
*  - Load reservation menu orders
*  - Update reservation data
*  - Generate contract PDF and send via email
*  - Expose UI commands for updating and generating contracts
*  - Manage loading state for UI
*/

using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Mailer;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.ReservationVM
{
    public class ReservationDetailsViewModel : INotifyPropertyChanged
    {
        #region Fields & Services
        private readonly EmailService _emailService;
        private readonly ContractMailer _contract_mailer;
        private readonly CancellationMailer _cancellation_mailer;
        private readonly ReservationService _reservationService = new();
        private readonly ReservationChecklistService _checklistService = new();
        
        #endregion

        #region Properties & Data
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Make Reservation settable so we can replace the object reference when server returns updated model.
        public Reservation Reservation { get; private set; }

        public ObservableCollection<ReservationMenuOrder> ReservationMenuOrders { get; } = new();
        public ObservableCollection<MenuOption> MenuOptions { get; } = new();

        private ReservationMenuOrder? _selectedMenuOrder;
        public ReservationMenuOrder? SelectedMenuOrder
        {
            get => _selectedMenuOrder;
            set { _selectedMenuOrder = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand GenerateContractCommand { get; }
        public ICommand UpdateReservationCommand { get; }
        public ICommand OpenChecklistCommand { get; }
        public ICommand CancelReservationCommand { get; }
        #endregion

        #region Constructor
        public ReservationDetailsViewModel(Reservation reservation)
        {
            Reservation = reservation ?? throw new ArgumentNullException(nameof(reservation));

            _emailService = new EmailService();
            _contract_mailer = new ContractMailer(_emailService);
            _cancellation_mailer = new CancellationMailer(_emailService);

            GenerateContractCommand = new RelayCommand(async () => await GenerateContractAsync());
            UpdateReservationCommand = new RelayCommand(async () => await UpdateReservationAsync());
            OpenChecklistCommand = new RelayCommand(() => OpenChecklist());
            CancelReservationCommand = new RelayCommand(async () => await CancelReservationAsync());

            Task.Run(LoadReservationMenuOrdersAsync);
        }
        #endregion

        #region PropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        #region Data Loading
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
                        ReservationMenuOrders.Add(order);
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
        #endregion

        #region Reservation Updates
        private async Task UpdateReservationAsync()
        {
            try
            {
                IsLoading = true;
                AppLogger.Info($"Updating reservation ID {Reservation.Id}");

                var updated = await _reservationService.UpdateReservationAsync(Reservation);

                if (updated != null)
                {
                    // Replace local reservation reference with the updated one so bindings update
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Reservation = updated;
                        OnPropertyChanged(nameof(Reservation));
                    });

                    await LoadReservationMenuOrdersAsync();
                    AppLogger.Success($"Reservation ID {Reservation.Id} updated successfully.");

                    // Show blocking messagebox (undo snackbar)
                    App.Current.Dispatcher.Invoke(() => MessageBox.Show("Reservation updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information));
                }
                else
                {
                    AppLogger.Error($"Reservation update returned null for ID {Reservation.Id}", showToUser: true);
                    App.Current.Dispatcher.Invoke(() => MessageBox.Show("Failed to update reservation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error updating reservation: {ex.Message}", showToUser: true);
                App.Current.Dispatcher.Invoke(() => MessageBox.Show("An error occurred while updating reservation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Cancel Reservation
        private async Task CancelReservationAsync()
        {
            if (Reservation == null) return;

            try
            {
                // Ask for confirmation first
                var confirm = MessageBox.Show($"Cancel reservation {Reservation.ReceiptNumber}?\nThis will mark the reservation as canceled and notify the reserving user.",
                    "Confirm Cancel",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                // Prompt for reason (not stored)
                var dlg = new CancelReasonWindow
                {
                    Owner = App.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                };

                var result = dlg.ShowDialog();
                if (result != true)
                {
                    // user cancelled providing a reason
                    return;
                }

                string reason = dlg.Reason?.Trim() ?? string.Empty;

                IsLoading = true;

                AppLogger.Info($"Cancelling reservation ID {Reservation.Id} with reason: {reason}");

                // Update status only (reason not stored)
                var updated = await _reservationService.UpdateReservationStatusAsync(Reservation.Id, "canceled");

                if (updated != null)
                {
                    // replace Reservation reference so bindings update
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Reservation = updated;
                        OnPropertyChanged(nameof(Reservation));
                    });

                    // Send email to the user who made the reservation
                    string userEmail = Reservation.Profile?.Email ?? string.Empty;
                    string userName = Reservation.Profile?.FullName ?? "Customer";

                    if (!string.IsNullOrWhiteSpace(userEmail))
                    {
                        bool sent = await _cancellation_mailer.SendCancellationEmailAsync(
                            userEmail,
                            userName,
                            Reservation,
                            reason
                        );

                        if (sent)
                            AppLogger.Success("Cancellation email sent to reserving user.");
                        else
                            AppLogger.Error("Failed to send cancellation email to reserving user.", showToUser: false);
                    }

                    // Show blocking messagebox and keep window open (undo snackbar)
                    App.Current.Dispatcher.Invoke(() => MessageBox.Show("Reservation canceled and the reserving user was notified.", "Canceled", MessageBoxButton.OK, MessageBoxImage.Information));
                }
                else
                {
                    AppLogger.Error($"Failed to cancel reservation ID {Reservation.Id}", showToUser: true);
                    App.Current.Dispatcher.Invoke(() => MessageBox.Show("Failed to cancel reservation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error cancelling reservation");
                App.Current.Dispatcher.Invoke(() => MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Contract Generation & Email
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
                    // Generate PDF synchronously (may take a moment)
                    ContractPdfGenerator.Generate(Reservation, saveDialog.FileName);

                    AppLogger.Info("Attempting to send contract email...");
                    bool sent = await _contract_mailer.SendContractEmailAsync(
                        Reservation.Profile?.Email ?? string.Empty,
                        Reservation.Profile?.FullName ?? "Client",
                        Reservation.EventDate.ToString("MMMM dd, yyyy"),
                        saveDialog.FileName
                    );

                    if (!sent)
                        throw new Exception("Failed to send the contract email.");

                    AppLogger.Success("Contract email sent successfully.");

                    // After successfully sending the contract, update status to contractsigning
                    if (!string.Equals(Reservation.Status, "contractsigning", StringComparison.OrdinalIgnoreCase))
                    {
                        // set status locally first
                        Reservation.Status = "contractsigning";

                        var updated = await _reservationService.UpdateReservationAsync(Reservation);

                        if (updated != null)
                        {
                            Reservation.Status = updated.Status;
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                // replace reference so UI updates bindings
                                Reservation = updated;
                                OnPropertyChanged(nameof(Reservation));

                                // Show blocking messagebox instead of snackbar
                                MessageBox.Show("Contract generated and status set to 'contractsigning'.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                    }
                });

                AppLogger.Success($"Contract generated, emailed, and status updated to 'contractsigning' for reservation ID {Reservation.Id}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error generating/sending contract: {ex.Message}", showToUser: true);
                App.Current.Dispatcher.Invoke(() => MessageBox.Show("Error generating or sending contract.", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Checklist
        private void OpenChecklist()
        {
            var win = new ChecklistBuilder
            {
                Owner = App.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            };
            win.ShowDialog();
        }
        #endregion

   
   
    }
}