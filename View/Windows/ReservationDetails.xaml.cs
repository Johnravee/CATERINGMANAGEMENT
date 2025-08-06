using System.Diagnostics;
using System.IO;
using System.Windows;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using Microsoft.Win32;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class ReservationDetails : Window
    {
        private Reservation _reservation;

        public ReservationDetails(Reservation reservation)
        {
            InitializeComponent();
            _reservation = reservation;
            DataContext = reservation;
        }

        [Obsolete]
        private void GenerateContractButton_Click(object sender, RoutedEventArgs e)
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
                    // 1. Generate PDF at chosen path
                    ContractPdfGenerator.Generate(_reservation, saveDialog.FileName);

                    // 2. Ask if user wants to print
                    var result = MessageBox.Show("PDF saved successfully. Do you want to print it now?",
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
                    MessageBox.Show($"Failed to save or print the contract.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
