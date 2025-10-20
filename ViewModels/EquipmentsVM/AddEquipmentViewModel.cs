/*
 * FILE: AddEquipmentViewModel.cs
 * PURPOSE: Handles the logic for adding new equipment items in the Equipment module.
 *           Connected to the EquipmentItemAdd window and interacts with EquipmentService for database operations.
 */

using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.EquipmentsVM
{
    public class AddEquipmentViewModel : BaseViewModel
    {
        private string _itemName = string.Empty;
        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(); }
        }

        private string _quantity = string.Empty;
        public string Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); }
        }

        private string _condition = string.Empty;
        public string Condition
        {
            get => _condition;
            set { _condition = value; OnPropertyChanged(); }
        }

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }


        private readonly EquipmentService _equipmentService;
        private readonly EquipmentViewModel _parentViewModel;

        public AddEquipmentViewModel(EquipmentViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));
            _equipmentService = new EquipmentService();

            SaveCommand = new RelayCommand(async () => await SaveAsync());
 
        }

        private async Task SaveAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ItemName))
                {
                    AppLogger.Info("Validation failed: Item name is empty.");
                    ShowMessage("Item name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(Quantity, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal qty))
                {
                    AppLogger.Info($"Validation failed: Quantity '{Quantity}' is not valid.");
                    ShowMessage("Quantity must be a valid number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newItem = new Equipment
                {
                    ItemName = ItemName.Trim(),
                    Quantity = qty,
                    Condition = string.IsNullOrWhiteSpace(Condition) ? "Good" : Condition.Trim(),
                    Notes = Notes.Trim(),
                    UpdatedAt = DateTime.UtcNow
                };

                AppLogger.Info("Attempting to insert new equipment item...");

                var inserted = await _equipmentService.InsertEquipmentAsync(newItem);

                if (inserted != null)
                {
                    AppLogger.Success($"Inserted equipment: {inserted.Id} - {inserted.ItemName}");
                    ShowMessage("Equipment item added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await _parentViewModel.LoadPage(1);
                    CloseWindow();
                }
                else
                {
                    AppLogger.Error("InsertEquipmentAsync returned null.");
                    ShowMessage("Failed to add equipment item.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error while adding equipment item.");
                ShowMessage($"Unexpected error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ Restrict input to numeric and dot
        public static void HandleQuantityInput(TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = false;
                    window.Close();
                    break;
                }
            }
        }
    }
}
