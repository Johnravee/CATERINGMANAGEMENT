using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels
{
    internal class AddPackageViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private decimal? _ratings;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public decimal? Ratings
        {
            get => _ratings;
            set { _ratings = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddPackageViewModel()
        {
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
                var client = await SupabaseService.GetClientAsync();

                var package = new Package
                {
                    Name = this.Name,
                    Ratings = this.Ratings,
                    CreatedAt = DateTime.UtcNow
                };

                var response = await client
                    .From<Package>()
                    .Insert(package);

                MessageBox.Show("Package added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving package:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow(bool success = false)
        {
            var win = Application.Current.Windows.OfType<CATERINGMANAGEMENT.View.Windows.AddPackage>()
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
