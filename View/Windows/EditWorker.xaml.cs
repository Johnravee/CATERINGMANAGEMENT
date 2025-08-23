using CATERINGMANAGEMENT.Models;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for EditWorker.xaml
    /// </summary>
    public partial class EditWorker : Window
    {
        public Worker? Worker { get; set; }

        public EditWorker(Worker existingWorker)
        {
            InitializeComponent();
            Worker = existingWorker ?? throw new ArgumentNullException(nameof(existingWorker));
            DataContext = Worker;

        }

        /// <summary>
        /// Handles the "Update" button click event, performing validation before saving.
        /// </summary>
        private void UpdateWorker_Click(object sender, RoutedEventArgs e)
        {
            // Validate Name
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate Role
            if (string.IsNullOrWhiteSpace(RoleTextBox.Text))
            {
                MessageBox.Show("Role is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate Contact
            if (string.IsNullOrWhiteSpace(ContactTextBox.Text))
            {
                MessageBox.Show("Contact number is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate Email using a regular expression
            string email = EmailTextBox.Text;
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase))
            {
                MessageBox.Show("A valid email address is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate Salary
            if (!string.IsNullOrWhiteSpace(SalaryTextBox.Text) && !decimal.TryParse(SalaryTextBox.Text, out _))
            {
                MessageBox.Show("Salary must be a valid number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Handles the "Cancel" button click event.
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
