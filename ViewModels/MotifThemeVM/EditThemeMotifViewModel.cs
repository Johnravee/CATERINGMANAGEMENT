/*
 * FILE: EditThemeMotifViewModel.cs
 * PURPOSE: Handles editing of Theme & Motif entity with data binding,
 *           validation, and Supabase update via ThemeMotifService.
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.MotifThemeVM
{
    public class EditThemeMotifViewModel : BaseViewModel
    {
        private string _name = string.Empty;
        private Package _selectedPackage;
        private readonly ThemeMotif _existingItem;
        private readonly ThemeMotifService _themeMotifService = new();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public Package SelectedPackage
        {
            get => _selectedPackage;
            set { _selectedPackage = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Package> Packages { get; set; } = new();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ThemeMotif ResultThemeMotif { get; private set; }

        public event Action<bool>? RequestClose;

        public EditThemeMotifViewModel(ThemeMotif existingItem)
        {
            _existingItem = existingItem ?? throw new ArgumentNullException(nameof(existingItem));
            _selectedPackage = new Package();

            Name = existingItem.Name ?? string.Empty;

            ResultThemeMotif = new ThemeMotif
            {
                Id = existingItem.Id,
                CreatedAt = existingItem.CreatedAt
            };

            SaveCommand = new RelayCommand(async () => await ExecuteSave());
            CancelCommand = new RelayCommand(ExecuteCancel);

            _ = LoadPackages();
        }

        private async Task LoadPackages()
        {
            try
            {
                AppLogger.Info("Loading packages for EditThemeMotifViewModel...");
                var _packageService = new PackageService();
                var packages = await _packageService.GetAllPackagesAsync(); // ✅ FIXED

                Packages.Clear();
                foreach (var pkg in packages)
                    Packages.Add(pkg);

                SelectedPackage = Packages.FirstOrDefault(p => p.Id == _existingItem.PackageId);
                AppLogger.Success($"Loaded {Packages.Count} packages.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load packages for Theme & Motif editing.");
                ShowMessage("Failed to load packages. Please try again.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ShowMessage("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedPackage == null)
            {
                ShowMessage("Please select a package.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                AppLogger.Info($"Updating ThemeMotif: ID={_existingItem.Id}, Name={Name}");

                var updatedMotif = new NewThemeMotif
                {
                    Id = _existingItem.Id,
                    Name = Name.Trim(),
                    PackageId = SelectedPackage.Id,
                    CreatedAt = _existingItem.CreatedAt
                };

                var result = await _themeMotifService.UpdateThemeMotifAsync(updatedMotif);

                if (result != null)
                {
                    AppLogger.Success($"ThemeMotif '{Name}' updated successfully.");
                    ShowMessage("Theme & Motif updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    RequestClose?.Invoke(true);
                }
                else
                {
                    ShowMessage("Failed to update Theme & Motif.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Error updating ThemeMotif: {Name}");
                ShowMessage("Error updating Theme & Motif. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            AppLogger.Info("EditThemeMotifViewModel: Cancel pressed.");
            RequestClose?.Invoke(false);
        }
    }
}
