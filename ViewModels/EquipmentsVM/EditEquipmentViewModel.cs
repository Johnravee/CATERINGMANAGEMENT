/*
 * FILE: EditEquipmentViewModel.cs
 * PURPOSE: Handles editing of existing equipment records.
 *          Connected to the EditEquipments window and updates data via EquipmentService.
 * 
 * RESPONSIBILITIES:
 *  - Expose editable fields for Equipment
 *  - Validate user input before saving
 *  - Update equipment via EquipmentService
 *  - Refresh parent EquipmentViewModel if needed
 *  - Close window after successful save
 */

using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services.Data;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.EquipmentsVM
{
    public class EditEquipmentViewModel : BaseViewModel
    {
        #region Fields & Services
        public Equipment EquipmentItem { get; }
        private readonly EquipmentViewModel _parentViewModel;
        private readonly EquipmentService _equipmentService;
        #endregion

        #region Properties
        private string _itemName;
        public string ItemName { get => _itemName; set { _itemName = value; OnPropertyChanged(); } }

        private string _quantity;
        public string Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(); } }

        private string _condition;
        public string Condition { get => _condition; set { _condition = value; OnPropertyChanged(); } }

        private string _notes;
        public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        #endregion

        #region Constructor
        public EditEquipmentViewModel(Equipment item, EquipmentViewModel parentViewModel)
        {
            EquipmentItem = item ?? throw new ArgumentNullException(nameof(item));
            _equipmentService = new EquipmentService();
            _parentViewModel = parentViewModel;

            // Initialize editable fields
            _itemName = item.ItemName ?? string.Empty;
            _quantity = item.Quantity?.ToString() ?? "0";
            _condition = item.Condition ?? "Good";
            _notes = item.Notes ?? string.Empty;

            SaveCommand = new RelayCommand(async () => await SaveAsync());
        }
        #endregion

        #region Methods: Save & Close
        private async Task SaveAsync()
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(ItemName))
                {
                    ShowMessage("Item name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(Quantity, out int qty) || qty < 0)
                {
                    ShowMessage("Quantity must be a valid non-negative number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Apply changes
                EquipmentItem.ItemName = ItemName;
                EquipmentItem.Quantity = qty;
                EquipmentItem.Condition = Condition;
                EquipmentItem.Notes = Notes;
                EquipmentItem.UpdatedAt = DateTime.UtcNow;

                var updated = await _equipmentService.UpdateEquipmentAsync(EquipmentItem);

                if (updated != null)
                {
                    ShowMessage("✅ Equipment updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    ShowMessage("Failed to update equipment.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Unexpected error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        #endregion
    }
}
