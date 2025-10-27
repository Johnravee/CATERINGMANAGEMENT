using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.PackageVM
{
    public class EditPackageViewModel : BaseViewModel
    {
        private readonly PackageService _packageService;

        private string _name = string.Empty;
       
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

      
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public Package ResultPackage { get; private set; }


        public EditPackageViewModel(Package existingItem)
        {
            _packageService = new PackageService();

            // Populate initial values
            Name = existingItem.Name;
            ResultPackage = new Package
            {
                Id = existingItem.Id,
                CreatedAt = existingItem.CreatedAt
            };

            SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync());
            CancelCommand = new RelayCommand(CloseWindow);
        }

        private async Task ExecuteSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var updateData = new Package
                {
                    Id = ResultPackage.Id,
                    Name = Name,
                    CreatedAt = ResultPackage.CreatedAt
                };

                var result = await _packageService.UpdatePackageAsync(updateData);

                if (result != null)
                {
                    MessageBox.Show("Package updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
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
