using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.GrazingVM
{
    internal class AddGrazingViewModel : BaseViewModel
    {
        private readonly GrazingService _grazingService = new();

        private string _name = string.Empty;
        private string _category = string.Empty;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddGrazingViewModel()
        {
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            CancelCommand = new RelayCommand(() => CloseWindow());
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Category))
            {
                MessageBox.Show("Both Name and Category are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var grazing = new GrazingTable
                {
                    Name = Name.Trim(),
                    Category = Category.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                var inserted = await _grazingService.InsertGrazingAsync(grazing);

                if (inserted != null)
                {
                    AppLogger.Success($"Grazing option '{Name}' added successfully.");
                    MessageBox.Show("Grazing option added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    AppLogger.Error($"Failed to add grazing option '{Name}'.");
                    MessageBox.Show("Failed to add grazing option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error adding grazing option");
                MessageBox.Show($"Error saving grazing option:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
