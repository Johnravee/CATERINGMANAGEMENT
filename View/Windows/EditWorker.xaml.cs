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

            if (existingWorker != null)
            {
                Worker = new Worker
                {
                    Id = existingWorker.Id,
                    Name = existingWorker.Name,
                    Role = existingWorker.Role,
                    Email = existingWorker.Email,
                    Contact = existingWorker.Contact,
                    Salary = existingWorker.Salary,
                    HireDate = existingWorker.HireDate,
                    Status = existingWorker.Status
                };

  
                NameTextBox.Text = Worker.Name;
                RoleTextBox.Text = Worker.Role;
                ContactTextBox.Text = Worker.Contact;
                EmailTextBox.Text = Worker.Email;
                SalaryTextBox.Text = Worker.Salary?.ToString();
                HireDatePicker.SelectedDate = Worker.HireDate;

              
                foreach (ComboBoxItem item in StatusComboBox.Items)
                {
                    if (item.Content.ToString() == Worker.Status)
                    {
                        StatusComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
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


            Worker.Name = NameTextBox.Text;
            Worker.Role = RoleTextBox.Text;
            Worker.Contact = ContactTextBox.Text;
            Worker.Email = email;
            Worker.Salary = decimal.TryParse(SalaryTextBox.Text, out decimal salary) ? salary : null;
            Worker.HireDate = HireDatePicker.SelectedDate;
            Worker.Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

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
