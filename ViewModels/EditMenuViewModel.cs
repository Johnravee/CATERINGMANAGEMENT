using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class EditMenuViewModel : INotifyPropertyChanged
    {
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

        public MenuOption ResultMenu { get; private set; }

        public event Action<bool>? RequestClose;

        public EditMenuViewModel(MenuOption existingItem)
        {
            Name = existingItem.Name;
            Category = existingItem.Category;
            ResultMenu = new MenuOption
            {
                Id = existingItem.Id,
                CreatedAt = existingItem.CreatedAt
            };

            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private async void ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Category))
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
                    CreatedAt = ResultMenu.CreatedAt
                };

                var response = await client
                    .From<MenuOption>()
                    .Where(x => x.Id == updateData.Id)
                    .Update(updateData);

                if (response.Models != null && response.Models.Count > 0)
                {
                    MessageBox.Show("Menu item updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    RequestClose?.Invoke(true);
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

        private void ExecuteCancel()
        {
            RequestClose?.Invoke(false);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
