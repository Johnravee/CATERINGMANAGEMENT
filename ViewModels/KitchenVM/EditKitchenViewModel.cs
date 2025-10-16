using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services.Data;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.KitchenVM
{
    public class EditKitchenItemViewModel : BaseViewModel
    {
        public Kitchen KitchenItem { get; }
        private readonly KitchenViewModel _parentViewModel;

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

        public ICommand SaveCommand { get; }

        private readonly KitchenService _kitchenService;

        public EditKitchenItemViewModel(Kitchen item, KitchenViewModel parentViewModel)
        {
            KitchenItem = item ?? throw new ArgumentNullException(nameof(item));

            _itemName = item.ItemName ?? string.Empty;
            _quantity = item.Quantity.ToString();
            _unit = item.Unit ?? string.Empty;

            _kitchenService = new KitchenService();

            SaveCommand = new RelayCommand(async () => await SaveAsync());
            _parentViewModel = parentViewModel;
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

                if (!decimal.TryParse(Quantity, out decimal qty))
                {
                    AppLogger.Info($"Validation failed: Quantity '{Quantity}' is not a valid number.");
                    ShowMessage("Quantity must be a valid number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                KitchenItem.ItemName = ItemName;
                KitchenItem.Quantity = qty;
                KitchenItem.Unit = Unit;
                KitchenItem.UpdatedAt = DateTime.UtcNow;

                AppLogger.Info($"Attempting to update kitchen item: {KitchenItem.Id} - {KitchenItem.ItemName}");

                var updated = await _kitchenService.UpdateKitchenItemAsync(KitchenItem);

                if (updated != null)
                {
                    await _parentViewModel.LoadPage(1);
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
                AppLogger.Error(ex, "An error occurred while updating kitchen item.");
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
    }
}
