using CATERINGMANAGEMENT.Models;
using System;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class KitchenItemAdd : Window
    {
        public Kitchen KitchenItem { get; set; }

        public KitchenItemAdd()
        {
            InitializeComponent();
        }

       

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ItemNameTextBox.Text))
            {
                MessageBox.Show("Item Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(UnitTextBox.Text))
            {
                MessageBox.Show("Unit is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text, out decimal qty))
            {
                MessageBox.Show("Quantity must be a valid number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (KitchenItem == null)
                KitchenItem = new Kitchen();

            KitchenItem.ItemName = ItemNameTextBox.Text.Trim();
            KitchenItem.Unit = UnitTextBox.Text.Trim();
            KitchenItem.Quantity = qty;
            KitchenItem.UpdatedAt = DateTime.UtcNow;

            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        // Optional: only allow numbers in Quantity textbox
        private void QuantityTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !decimal.TryParse(e.Text, out _);
        }
    }
}
