/*
 * FILE: EditKitchenItemViewModel.cs
 * PURPOSE: Handles the logic for editing existing kitchen inventory items.
 *          Connected to the EditKitchenItem window and updates data through KitchenService.
 * 
 * RESPONSIBILITIES:
 *  - Expose kitchen item properties for editing
 *  - Validate user input
 *  - Save updates via KitchenService
 *  - Refresh parent KitchenViewModel after successful update
 *  - Close the editing window upon completion
 */

using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services.Data;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.KitchenVM
{
    public class EditKitchenItemViewModel : BaseViewModel
    {
        #region Fields & Services
        private readonly KitchenService _kitchenService;
        #endregion

        #region Properties
        public Kitchen KitchenItem { get; }

        private string _itemName;
        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(); }
        }

        private string _quantity;
        public string Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); }
        }

        private string _unit;
        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        #endregion

        #region Constructor
        public EditKitchenItemViewModel(Kitchen item)
        {
            KitchenItem = item ?? throw new ArgumentNullException(nameof(item));

            _itemName = item.ItemName ?? string.Empty;
            _quantity = item.Quantity.ToString();
            _unit = item.Unit ?? string.Empty;

            _kitchenService = new KitchenService();

            SaveCommand = new RelayCommand(async () => await SaveAsync());
        }
        #endregion

        #region Methods: Save
        private async Task SaveAsync()
        {
            try
            {
                //  Basic validation
                if (string.IsNullOrWhiteSpace(ItemName))
                {
                    AppLogger.Info("Validation failed: Item name is empty.");
                    ShowMessage("Item name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(Quantity, out decimal qty))
                {
                    AppLogger.Info($"Validation failed: Quantity '{Quantity}' is not a valid number.");
                    ShowMessage("Quantity must be a valid number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //  Apply changes to model
                KitchenItem.ItemName = ItemName;
                KitchenItem.Quantity = qty;
                KitchenItem.Unit = Unit;
                KitchenItem.UpdatedAt = DateTime.UtcNow;

                AppLogger.Info($"Updating kitchen item: {KitchenItem.Id} - {KitchenItem.ItemName}");

                var updated = await _kitchenService.UpdateKitchenItemAsync(KitchenItem);

                if (updated != null)
                {
                    AppLogger.Success($"Kitchen item updated successfully: {updated.Id} - {updated.ItemName}");
                    ShowMessage("Kitchen item updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    AppLogger.Error("UpdateKitchenItemAsync returned null.");
                    ShowMessage("Failed to update kitchen item.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error while updating kitchen item.");
                ShowMessage($"Unexpected error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Methods: Window Management
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
