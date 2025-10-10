using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.PackageVM
{
    public class EditPackageViewModel : INotifyPropertyChanged
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

        public Package ResultPackage { get; private set; }

        public event Action<bool>? RequestClose;

        public EditPackageViewModel(Package existingItem)
        {
            // Populate initial values
            Name = existingItem.Name;
            Ratings = existingItem.Ratings;
            ResultPackage = new Package
            {
                Id = existingItem.Id,
                CreatedAt = existingItem.CreatedAt
            };

            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private async void ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var client = await SupabaseService.GetClientAsync();

                var updateData = new Package
                {
                    Id = ResultPackage.Id,
                    Name = Name,
                    Ratings = Ratings,
                    CreatedAt = ResultPackage.CreatedAt
                };

                var response = await client
                    .From<Package>()
                    .Where(x => x.Id == updateData.Id)
                    .Update(updateData);

                if (response.Models != null && response.Models.Count > 0)
                {
                    MessageBox.Show("Package updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    RequestClose?.Invoke(true);
                }
                else
                {
                    MessageBox.Show("No package was updated.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating package:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
