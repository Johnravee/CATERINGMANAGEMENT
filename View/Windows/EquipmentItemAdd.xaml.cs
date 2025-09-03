using CATERINGMANAGEMENT.Models;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EquipmentItemAdd : Window
    {
        public Equipment NewEquipment { get; private set; }

        public EquipmentItemAdd()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Validate Item Name
            string itemName = ItemNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(itemName))
            {
                MessageBox.Show("Item name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate Quantity
            string quantityText = QuantityTextBox.Text.Trim();
            if (!decimal.TryParse(quantityText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal quantity))
            {
                MessageBox.Show("Quantity must be a valid number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get Condition
            string condition = ((ComboBoxItem)ConditionComboBox.SelectedItem)?.Content?.ToString();

            // Get Notes
            string notes = NotesTextBox.Text.Trim();

            // Create Equipment object
            NewEquipment = new Equipment
            {
                ItemName = itemName,
                Quantity = quantity,
                Condition = condition,
                Notes = notes,
                UpdatedAt = DateTime.Now
            };

            this.DialogResult = true;
            this.Close();
        }

        // Allow only numbers and decimal point
        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]"); // Only digits and dot allowed
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
