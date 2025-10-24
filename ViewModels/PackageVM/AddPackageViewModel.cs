using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.PackageVM
{
    internal class AddPackageViewModel : BaseViewModel
    {
        private readonly PackageService _packageService = new();

        private string _name = string.Empty;
        private decimal? _ratings;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddPackageViewModel()
        {
            _packageService = new PackageService();

            SaveCommand = new RelayCommand(async () => await SaveAsync());
            CancelCommand = new RelayCommand(() => CloseWindow());
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var package = new Package
                {
                    Name = Name,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _packageService.InsertPackageAsync(package);

                if (result != null)
                {
                    MessageBox.Show("Package added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reset fields
                    Name = string.Empty;


                    CloseWindow();
                }
                else
                {
                    MessageBox.Show("Failed to add package.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving package:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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


    }
}
