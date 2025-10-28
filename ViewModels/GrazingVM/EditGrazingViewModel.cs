using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.GrazingVM
{
    public class EditGrazingViewModel : BaseViewModel
    {
        private readonly GrazingService _grazingService = new();

        private string _name = string.Empty;
        private string _category = string.Empty;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public GrazingTable ResultGrazing { get; private set; }

        public EditGrazingViewModel(GrazingTable existingItem)
        {
            // Populate initial values
            Name = existingItem.Name;
            Category = existingItem.Category;
            ResultGrazing = new GrazingTable
            {
                Id = existingItem.Id,
                CreatedAt = existingItem.CreatedAt
            };

            SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync());
            CancelCommand = new RelayCommand(CloseWindow);
        }

        private async System.Threading.Tasks.Task ExecuteSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Category))
            {
                MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var updateData = new GrazingTable
                {
                    Id = ResultGrazing.Id,
                    Name = Name.Trim(),
                    Category = Category.Trim(),
                    CreatedAt = ResultGrazing.CreatedAt
                };

                var updated = await _grazingService.UpdateGrazingAsync(updateData);

                if (updated != null)
                {
                    AppLogger.Success($"Grazing item '{Name}' updated successfully.");
                    MessageBox.Show("Grazing item updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    AppLogger.Error($"No grazing item was updated for ID {ResultGrazing.Id}.");
                    MessageBox.Show("No item was updated.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error updating grazing item");
                MessageBox.Show($"Error updating grazing item:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
