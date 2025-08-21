using CATERINGMANAGEMENT.Models;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for EditKitchenItem.xaml
    /// </summary>
    public partial class EditKitchenItem : Window
    {
        // Property to hold the edited Kitchen item
        public Kitchen? KitchenItem { get; set; }

        // Constructor takes the existing item
        public EditKitchenItem(Kitchen existingItem)
        {
            InitializeComponent();

            if (existingItem != null)
            {
                KitchenItem = new Kitchen
                {
                    Id = existingItem.Id,
                    ItemName = existingItem.ItemName,
                    Unit = existingItem.Unit,
                    Quantity = existingItem.Quantity,
                    CreatedAt = existingItem.CreatedAt,
                    UpdatedAt = existingItem.UpdatedAt
                };

                // Populate fields
                ItemNameTextBox.Text = KitchenItem.ItemName;
                UnitTextBox.Text = KitchenItem.Unit;
                QuantityTextBox.Text = KitchenItem.Quantity.ToString();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ItemNameTextBox.Text))
            {
                MessageBox.Show("Item name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text, out decimal qty))
            {
                MessageBox.Show("Quantity must be a number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            KitchenItem.ItemName = ItemNameTextBox.Text.Trim();
            KitchenItem.Unit = UnitTextBox.Text.Trim();
            KitchenItem.Quantity = qty;
            KitchenItem.UpdatedAt = DateTime.UtcNow;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Only allow numeric input for Quantity
        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]*(\.[0-9]*)?$");
        }
    }
}
