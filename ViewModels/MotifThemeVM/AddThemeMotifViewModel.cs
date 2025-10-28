/*
 * FILE: AddThemeMotifViewModel.cs
 * PURPOSE: Handles the creation of new Theme & Motif records within the system.
 * RESPONSIBILITIES:
 *   • Load and display available packages from Supabase for selection.
 *   • Validate user input for Theme & Motif creation.
 *   • Insert new Theme & Motif data into Supabase.
 *   • Provide feedback via MessageBoxes and internal logging.
 *   • Manage Add window closing behavior on success or cancel.
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.MotifThemeVM
{
    internal class AddThemeMotifViewModel : BaseViewModel
    {
        #region Services
        private readonly PackageService _packageService= new();
        private readonly ThemeMotifService _thememotifService = new();
        #endregion

        #region === Private Fields ===

        private string _name = string.Empty;
        private long? _selectedPackageId;
        private ObservableCollection<Package> _packages = new();


        #endregion

        #region === Public Properties ===

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public long? SelectedPackageId
        {
            get => _selectedPackageId;
            set { _selectedPackageId = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Package> Packages
        {
            get => _packages;
            set { _packages = value; OnPropertyChanged(); }
        }

        #endregion

        #region === Commands ===

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region === Constructor ===

        public AddThemeMotifViewModel()
        {
            _packageService = new PackageService();
            _thememotifService = new ThemeMotifService();
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            CancelCommand = new RelayCommand(() => CloseWindow());

            _ = LoadPackages(); // fire and forget
        }

        #endregion

        #region === Load Packages ===

        private async Task LoadPackages()
        {
            try
            {
                AppLogger.Info("Loading available packages for Theme & Motif creation...");
                var response = await _packageService.GetAllPackagesAsync();

                Packages = new ObservableCollection<Package>(response);

                AppLogger.Info($"Successfully loaded {Packages.Count} packages.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error loading packages: {ex.Message}");
                ShowMessage($"Error loading packages:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region === Save Logic ===

        private async Task SaveAsync()
        {
            AppLogger.Info("Attempting to save new Theme & Motif entry...");

            // Validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                AppLogger.Info("Validation failed: Name is empty.");
                ShowMessage("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedPackageId == null || SelectedPackageId <= 0)
            {
                AppLogger.Info("Validation failed: No package selected.");
                ShowMessage("Please select a package.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save process
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var motif = new NewThemeMotif
                {
                    Name = Name,
                    PackageId = SelectedPackageId,
                    CreatedAt = DateTime.UtcNow
                };

                var response = _thememotifService.InsertThemeMotifAsync(motif);

                AppLogger.Info($"Theme & Motif '{Name}' added successfully (Package ID: {SelectedPackageId}).");
                ShowMessage("Theme & Motif added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error saving Theme & Motif: {ex.Message}");
                ShowMessage($"Error saving Theme & Motif:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region === Window Handling ===
        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = true;
                    window.Close();
                    break;
                }
            }
        }


        #endregion
    }
}
