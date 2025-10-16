using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services.Data;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.KitchenVM
{
    public class AddKitchenItemViewModel : BaseViewModel
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

        private string _unit = string.Empty;
        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }

        private readonly KitchenService _kitchenService;
        private readonly KitchenViewModel _parentViewModel;

        public AddKitchenItemViewModel(KitchenViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));
            _kitchenService = new KitchenService();
            SaveCommand = new RelayCommand(async () => await SaveAsync());
        }

        private async Task SaveAsync()
        {
            try
            {
                // Validate inputs
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
                    await _parentViewModel.LoadPage(1);
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
                AppLogger.Error(ex, "An error occurred while adding kitchen item.");
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
