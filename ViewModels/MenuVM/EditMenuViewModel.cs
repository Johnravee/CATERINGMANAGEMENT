using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.MenuVM
{
    public class EditMenuViewModel : BaseViewModel
    {
        private string _name = string.Empty;
        private string _category = string.Empty;
        private string _status = "Available";  // default value

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

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public MenuOption ResultMenu { get; private set; }



        public EditMenuViewModel(MenuOption existingItem)
        {
            Name = existingItem.Name ?? string.Empty;
            Category = existingItem.Category ?? string.Empty;
            Status = existingItem.Status ?? "Available"; 

            ResultMenu = new MenuOption
            {
                Id = existingItem.Id,
                CreatedAt = existingItem.CreatedAt
            };

            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(CloseWindow);
        }

        private async void ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(Status))
            {
                MessageBox.Show("All fields are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var client = await SupabaseService.GetClientAsync();

                var updateData = new MenuOption
                {
                    Id = ResultMenu.Id,
                    Name = Name,
                    Category = Category,
                    Status = Status,
                    CreatedAt = ResultMenu.CreatedAt
                };

                var response = await client
                    .From<MenuOption>()
                    .Where(x => x.Id == updateData.Id)
                    .Update(updateData);

                if (response.Models != null && response.Models.Count > 0)
                {
                    MessageBox.Show("Menu item updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    MessageBox.Show("No item was updated.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating menu item:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
