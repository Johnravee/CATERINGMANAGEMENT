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

namespace CATERINGMANAGEMENT.ViewModels
{
    internal class AddThemeMotifViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private long? _selectedPackageId;

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

        private ObservableCollection<Package> _packages = new();
        public ObservableCollection<Package> Packages
        {
            get => _packages;
            set { _packages = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddThemeMotifViewModel()
        {
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            CancelCommand = new RelayCommand(() => CloseWindow());

            _ = LoadPackages();
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

                Packages = new ObservableCollection<Package>(response.Models);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading packages:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedPackageId == null || SelectedPackageId <= 0)
            {
                MessageBox.Show("Please select a package.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var client = await SupabaseService.GetClientAsync();

                var motif = new NewThemeMotif
                {
                    Name = this.Name,
                    PackageId = this.SelectedPackageId,
                   
                };
               

                var response = await client
                    .From<NewThemeMotif>()
                    .Insert(motif);

                MessageBox.Show("Theme & Motif added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving Theme & Motif:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow(bool success = false)
        {
            var win = Application.Current.Windows.OfType<CATERINGMANAGEMENT.View.Windows.AddThemeMotif>()
                .FirstOrDefault(w => w.DataContext == this);

            if (win != null)
            {
                if (success)
                    win.DialogResult = true;
                win.Close();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
