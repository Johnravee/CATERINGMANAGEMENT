using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using Supabase.Postgrest.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.GrazingVM
{
    internal class AddGrazingViewModel : BaseModel
    {
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
                var client = await SupabaseService.GetClientAsync();

                var grazing = new GrazingTable
                {
                    Name = Name,
                    Category = Category,
                    CreatedAt = DateTime.UtcNow
                };

                var response = await client
                    .From<GrazingTable>()
                    .Insert(grazing);

                MessageBox.Show("Grazing option added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving grazing option:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow(bool success = false)
        {
            var win = Application.Current.Windows.OfType<AddGrazing>()
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
