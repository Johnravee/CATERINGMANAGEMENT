using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Mailer;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services; // added for EmailService
using CATERINGMANAGEMENT.Services.Data;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.ReservationVM
{
    public class AdminContractViewModel : INotifyPropertyChanged
    {
        private readonly PackageService _packageService = new();
        private readonly ThemeMotifService _themeService = new();
        private readonly GrazingService _grazingService = new();
        private readonly EmailService _emailService = new();
        private readonly ContractMailer _contractMailer;

        // Grazing is optional; include a 'None' option
        private static readonly GrazingTable GrazingNone = new() { Id = 0, Name = "None", Category = string.Empty, CreatedAt = DateTime.UtcNow };

        public AdminContractViewModel()
        {
            _contractMailer = new ContractMailer(_emailService);
            GenerateContractCommand = new RelayCommand(async () => await GenerateContractAsync());
            _ = LoadChoicesAsync();
        }

        // Data
        private string _clientName = string.Empty;
        public string ClientName { get => _clientName; set { _clientName = value; OnPropertyChanged(); } }

        private string _clientEmail = string.Empty;
        public string ClientEmail { get => _clientEmail; set { _clientEmail = value; OnPropertyChanged(); } }

        private string _clientContact = string.Empty;
        public string ClientContact { get => _clientContact; set { _clientContact = value; OnPropertyChanged(); } }

        private string _clientAddress = string.Empty;
        public string ClientAddress { get => _clientAddress; set { _clientAddress = value; OnPropertyChanged(); } }

        private string _celebrant = string.Empty;
        public string Celebrant { get => _celebrant; set { _celebrant = value; OnPropertyChanged(); } }

        private string _venue = string.Empty;
        public string Venue { get => _venue; set { _venue = value; OnPropertyChanged(); } }

        private string _location = string.Empty;
        public string Location { get => _location; set { _location = value; OnPropertyChanged(); } }

        private DateTime _eventDate = DateTime.Today.AddDays(7);
        public DateTime EventDate { get => _eventDate; set { _eventDate = value; OnPropertyChanged(); } }

        private string _eventTimeText = "18:00"; // HH:mm
        public string EventTimeText { get => _eventTimeText; set { _eventTimeText = value; OnPropertyChanged(); } }

        private long _adultsQty;
        public long AdultsQty { get => _adultsQty; set { _adultsQty = value; OnPropertyChanged(); } }

        private long _kidsQty;
        public long KidsQty { get => _kidsQty; set { _kidsQty = value; OnPropertyChanged(); } }

        public ObservableCollection<Package> Packages { get; } = new();
        public ObservableCollection<ThemeMotif> Themes { get; } = new();
        public ObservableCollection<GrazingTable> Grazings { get; } = new();

        private Package? _selectedPackage;
        public Package? SelectedPackage { get => _selectedPackage; set { _selectedPackage = value; OnPropertyChanged(); } }

        private ThemeMotif? _selectedTheme;
        public ThemeMotif? SelectedTheme { get => _selectedTheme; set { _selectedTheme = value; OnPropertyChanged(); } }

        private GrazingTable? _selectedGrazing;
        public GrazingTable? SelectedGrazing { get => _selectedGrazing; set { _selectedGrazing = value; OnPropertyChanged(); } }

        private bool _sendEmail = true;
        public bool SendEmail { get => _sendEmail; set { _sendEmail = value; OnPropertyChanged(); } }

        public ICommand GenerateContractCommand { get; }

        private async Task LoadChoicesAsync()
        {
            try
            {
                var pkgs = await _packageService.GetAllPackagesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Packages.Clear();
                    foreach (var p in pkgs) Packages.Add(p);
                    if (Packages.Count > 0) SelectedPackage = Packages[0];
                });

                // Load all themes & motifs
                var motifs = await _themeService.GetAllThemeMotifsAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Themes.Clear();
                    foreach (var t in motifs) Themes.Add(t);
                    if (Themes.Count > 0) SelectedTheme = Themes[0];
                });

                // Load all grazing options + add 'None' option as default
                var graz = await _grazingService.GetAllGrazingAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Grazings.Clear();
                    Grazings.Add(GrazingNone);
                    foreach (var g in graz) Grazings.Add(g);
                    SelectedGrazing = GrazingNone;
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load selections for contract generator");
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(ClientName)) { Show("Client name is required."); return false; }
            if (string.IsNullOrWhiteSpace(ClientEmail)) { Show("Client email is required."); return false; }
            if (!ValidationHelper.IsValidEmail(ClientEmail.Trim())) { Show("Please enter a valid email address."); return false; }
            if (string.IsNullOrWhiteSpace(ClientContact)) { Show("Contact number is required."); return false; }
            if (ClientContact.Any(c => !char.IsDigit(c))) { Show("Contact number must be digits only."); return false; }
            if (string.IsNullOrWhiteSpace(ClientAddress)) { Show("Address is required."); return false; }
            if (string.IsNullOrWhiteSpace(Celebrant)) { Show("Celebrant is required."); return false; }
            if (string.IsNullOrWhiteSpace(Venue)) { Show("Venue is required."); return false; }
            if (string.IsNullOrWhiteSpace(Location)) { Show("Location is required."); return false; }
            if (!TimeSpan.TryParseExact(EventTimeText, new[] { @"hh\:mm", @"h\:mm" }, null, out _)) { Show("Invalid time format. Use HH:mm."); return false; }
            if (SelectedPackage == null) { Show("Please select a package."); return false; }
            if (SelectedTheme == null) { Show("Please select a theme/motif."); return false; }

            if (AdultsQty < 0 || KidsQty < 0) { Show("Guest counts cannot be negative."); return false; }
            if ((AdultsQty + KidsQty) <= 0) { Show("Please provide at least one guest (adults or kids)."); return false; }
            return true;
        }

        private static string GenerateTempReceiptNumber()
        {
            var now = DateTime.Now;
            return $"TMP-{now:yyyyMMddHHmmss}";
        }

        private async Task GenerateContractAsync()
        {
            if (!ValidateInputs()) return;

            try
            {
                // Build an in-memory reservation object (no DB insert)
                var reservation = new Reservation
                {
                    Id = 0,
                    ReceiptNumber = GenerateTempReceiptNumber(),
                    Celebrant = this.Celebrant ?? string.Empty,
                    Venue = this.Venue ?? string.Empty,
                    Location = this.Location ?? string.Empty,
                    EventDate = this.EventDate.Date,
                    EventTime = TimeSpan.Parse(EventTimeText),
                    AdultsQty = this.AdultsQty,
                    KidsQty = this.KidsQty,
                    Status = "contractsigning",
                    CreatedAt = DateTime.UtcNow,
                    Profile = new Profile
                    {
                        FullName = this.ClientName ?? string.Empty,
                        Email = this.ClientEmail?.Trim() ?? string.Empty,
                        ContactNumber = this.ClientContact ?? string.Empty,
                        Address = this.ClientAddress ?? string.Empty
                    },
                    Package = SelectedPackage,
                    ThemeMotif = SelectedTheme,
                    Grazing = (SelectedGrazing != null && SelectedGrazing.Id != 0) ? SelectedGrazing : null
                };

                // Choose save path
                var sfd = new SaveFileDialog
                {
                    Title = "Save Contract PDF",
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"Contract_{reservation.ReceiptNumber}.pdf"
                };

                if (sfd.ShowDialog() != true) return;

                // Generate PDF using the same generator as Reservation Details (no custom template path)
                ContractPdfGenerator.Generate(reservation, sfd.FileName);

                // Optionally send email
                if (SendEmail && !string.IsNullOrWhiteSpace(reservation.Profile?.Email))
                {
                    bool sent = await _contractMailer.SendContractEmailAsync(
                        reservation.Profile!.Email!,
                        reservation.Profile!.FullName ?? "Client",
                        reservation.EventDate.ToString("MMMM dd, yyyy"),
                        sfd.FileName
                    );

                    if (sent) AppLogger.Success("Contract emailed to client.");
                    else AppLogger.Error("Failed to send contract email.", showToUser: true);
                }

                MessageBox.Show("Contract generated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (FileNotFoundException fex)
            {
                AppLogger.Error(fex, fex.Message, showToUser: true);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error generating contract", showToUser: true);
            }
        }

        private void Show(string message)
        {
            MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
