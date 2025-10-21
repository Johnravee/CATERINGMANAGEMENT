/*
 * FILE: AddKitchenItemViewModel.cs
 * PURPOSE: Handles the logic for adding new kitchen inventory items in the Kitchen module.
 *          Connected to the AddKitchenItem window and interacts with KitchenService for database operations.
 * 
 * RESPONSIBILITIES:
 *  - Expose properties for new kitchen item entry
 *  - Validate user input
 *  - Insert new item via KitchenService
 *  - Refresh parent KitchenViewModel after successful insertion
 *  - Close the add item window upon completion
 */

using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services.Data;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.KitchenVM
{
    public class AddKitchenItemViewModel : BaseViewModel
    {
        #region Fields & Services
        private readonly KitchenService _kitchenService;
        private readonly KitchenViewModel _parentViewModel;
        #endregion

        #region Properties
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

        private string _unit = string.Empty;
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
        public AddKitchenItemViewModel(KitchenViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));
            _kitchenService = new KitchenService();

            SaveCommand = new RelayCommand(async () => await SaveAsync());
        }
        #endregion

        #region Methods: Save
        private async Task SaveAsync()
        {
            try
            {
                // ✅ Basic validation
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

                if (string.IsNullOrWhiteSpace(Unit))
                {
                    AppLogger.Info("Validation failed: Unit is empty.");
                    ShowMessage("Unit is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✅ Create new item
                var newItem = new Kitchen
                {
                    ItemName = ItemName.Trim(),
                    Quantity = qty,
                    Unit = Unit.Trim(),
                    UpdatedAt = DateTime.UtcNow
                };

                AppLogger.Info("Attempting to insert new kitchen item...");

                var inserted = await _kitchenService.InsertKitchenItemAsync(newItem);

                if (inserted != null)
                {
                    AppLogger.Success($"Inserted kitchen item: {inserted.Id} - {inserted.ItemName}");
                    ShowMessage("Kitchen item added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await _parentViewModel.LoadPage(1); // refresh list
                    CloseWindow();
                }
                else
                {
                    AppLogger.Error("InsertKitchenItemAsync returned null.");
                    ShowMessage("Failed to add kitchen item.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error while adding kitchen item.");
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
