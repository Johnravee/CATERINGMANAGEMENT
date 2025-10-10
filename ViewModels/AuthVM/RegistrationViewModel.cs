using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Helpers;

namespace CATERINGMANAGEMENT.ViewModels.AuthVM
{
    internal class RegistrationViewModel : INotifyPropertyChanged
    {
        // Bindable properties
        private string _email;
        private string _password;
        private string _confirmPassword;

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand RegisterAdminCommand { get; }

        // Constructor
        public RegistrationViewModel()
        {
            RegisterAdminCommand = new RelayCommand(async () => await RegisterAdminAsync());
        }

        // Registration logic
        private async Task RegisterAdminAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Email and password are required.");
                return;
            }


            if (Password != ConfirmPassword)
            {
                MessageBox.Show("Passwords do not match.");
                return;
            }

            if( Password.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters long.");
                return;
            }

            if (!ValidationHelper.IsValidEmail(Email))
            {
                MessageBox.Show("Please enter a valid email address.");
                return;
            }

            var success = await AdminRegistrationService.RegisterAdminAsync(Email, Password);

            if (success)
            {
                Email = string.Empty;
                Password = string.Empty;
                ConfirmPassword = string.Empty;

                MessageBox.Show("Admin registered successfully.");
                Application.Current.Windows[^1]?.Close(); 
            }
            else
            {
                MessageBox.Show("Registration failed. Please check the logs.");
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
