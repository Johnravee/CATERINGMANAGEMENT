using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.MotifThemeVM
{
    public class EditThemeMotifViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private Package _selectedPackage;

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

        private readonly ThemeMotif _existingItem;

        public EditThemeMotifViewModel(ThemeMotif existingItem)
        {
            _existingItem = existingItem;

            Name = existingItem.Name ?? string.Empty;

            ResultThemeMotif = new ThemeMotif
            {
                Id = existingItem.Id,
                CreatedAt = existingItem.CreatedAt
            };

            SaveCommand = new RelayCommand(async () => await ExecuteSave());
            CancelCommand = new RelayCommand(ExecuteCancel);

            _ = LoadPackages(); // fire and forget
        }

        private async Task LoadPackages()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Package>()
                    .Select("*")
                    .Order(p => p.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                Packages.Clear();
                foreach (var pkg in response.Models)
                {
                    Packages.Add(pkg);
                }

                // Pre-select the correct package based on PackageId
                SelectedPackage = Packages.FirstOrDefault(p => p.Id == _existingItem.PackageId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load packages:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedPackage == null)
            {
                MessageBox.Show("Please select a package.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var client = await SupabaseService.GetClientAsync();

                var updateData = new NewThemeMotif
                {
                    Id = ResultThemeMotif.Id,
                    Name = Name,
                    PackageId = SelectedPackage.Id,
                    CreatedAt = ResultThemeMotif.CreatedAt
                };

                var response = await client
                    .From<NewThemeMotif>()
                    .Where(x => x.Id == updateData.Id)
                    .Update(updateData);

                if (response.Models != null && response.Models.Count > 0)
                {
                    MessageBox.Show("Theme & Motif updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    RequestClose?.Invoke(true);
                }
                else
                {
                    MessageBox.Show("No Theme & Motif was updated.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating Theme & Motif:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            RequestClose?.Invoke(false);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
