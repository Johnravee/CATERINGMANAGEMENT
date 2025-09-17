using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Mailer;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class ReservationDetails : Window
    {
        private Reservation _reservation;
        public ICommand UpdateReservationCommand { get; }

        public ReservationDetails(Reservation reservation, ICommand updateReservationCommand)
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            _reservation = reservation;
            UpdateReservationCommand = updateReservationCommand;
            DataContext = reservation;
        }

        [Obsolete]
        private async void GenerateContractButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Save Contract PDF",
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Contract_{_reservation.ReceiptNumber}.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // Show loader
                    LoaderDialogHost.IsOpen = true;

                    await Task.Run(async () =>
                    {
                        // 1. Generate the contract PDF
                        ContractPdfGenerator.Generate(_reservation, saveDialog.FileName);

                        // 2. Send the contract via email
                        var emailService = new EmailService();
                        var contractMailer = new ContractMailer(emailService);

                        bool sent = await contractMailer.SendContractEmailAsync(
                            recipientEmail: _reservation.Profile?.Email ?? string.Empty,
                            recipientName: _reservation.Profile?.FullName ?? "Client",
                            eventDate: _reservation.EventDate.ToString("MMMM dd, yyyy"),
                            attachmentPath: saveDialog.FileName
                        );

                        if (!sent)
                        {
                            throw new Exception("Failed to send the contract email.");
                        }
                    });

                    LoaderDialogHost.IsOpen = false;

                    MessageBox.Show("Contract generated and sent successfully.",
                                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Ask if user wants to print
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
                catch (IOException ex)
                {
                    LoaderDialogHost.IsOpen = false;
                    MessageBox.Show($"Failed to save or print the contract.\n\n{ex.Message}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    LoaderDialogHost.IsOpen = false;
                    MessageBox.Show($"Unexpected error occurred.\n\n{ex.Message}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void ExitAppBtnHandler(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void MinimizeAppBtnHandler(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
