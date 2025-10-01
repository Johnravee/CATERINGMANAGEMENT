using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.View.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for AddWorker.xaml
    /// </summary>
    public partial class AddWorker : Window
    {
        
        public Worker? NewWorker { get; private set; }
        public AddWorker()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
        }

        private void AddWorker_Click(object sender, RoutedEventArgs e)
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

            // Validate Email
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || !IsValidEmail(EmailTextBox.Text))
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

            // If all validations pass, create the new worker object
            NewWorker = new Worker
            {
                Name = NameTextBox.Text,
                Role = RoleTextBox.Text,
                Contact = ContactTextBox.Text,
                Email = EmailTextBox.Text,
                Salary = decimal.TryParse(SalaryTextBox.Text, out decimal salary) ? salary : 0,
                HireDate = HireDatePicker.SelectedDate ?? DateTime.UtcNow,
                Status = (StatusComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString(),
            };

            // Close the window with a success result
            this.DialogResult = true;
            this.Close();
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

    }
}
