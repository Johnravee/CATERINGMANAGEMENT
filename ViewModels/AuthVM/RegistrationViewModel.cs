/*
 * FILE: RegistrationViewModel.cs
 * PURPOSE: Handles admin registration logic and validation in the authentication module.
 * RESPONSIBILITIES:
 *   • Validate email and password inputs.
 *   • Interact with AdminRegistrationService for registration.
 *   • Provide UI feedback and handle exceptions safely.
 *   • Log validation errors, process results, and unexpected exceptions.
 */

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Helpers;
using System.Linq;

namespace CATERINGMANAGEMENT.ViewModels.AuthVM
{
    internal class RegistrationViewModel : INotifyPropertyChanged
    {
        #region === Private Fields ===

        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;

        private bool _hasUpper;
        private bool _hasLower;
        private bool _hasDigit;
        private bool _hasSpecial;
        private bool _hasMinLength;

        #endregion

        #region === Public Properties ===

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                EvaluatePasswordCriteria();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(); }
        }

        public bool HasUpper { get => _hasUpper; private set { _hasUpper = value; OnPropertyChanged(); } }
        public bool HasLower { get => _hasLower; private set { _hasLower = value; OnPropertyChanged(); } }
        public bool HasDigit { get => _hasDigit; private set { _hasDigit = value; OnPropertyChanged(); } }
        public bool HasSpecial { get => _hasSpecial; private set { _hasSpecial = value; OnPropertyChanged(); } }
        public bool HasMinLength { get => _hasMinLength; private set { _hasMinLength = value; OnPropertyChanged(); } }

        #endregion

        #region === Commands ===

        public ICommand RegisterAdminCommand { get; }

        #endregion

        #region === Constructor ===

        public RegistrationViewModel()
        {
            RegisterAdminCommand = new RelayCommand(async () => await RegisterAdminAsync());
        }

        #endregion

        #region === Registration Logic ===

        private async Task RegisterAdminAsync()
        {
            try
            {
                AppLogger.Info("Attempting to register new admin...");

                // Validation
                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    AppLogger.Info("Validation failed: Missing email or password.");
                    MessageBox.Show("Email and password are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Password != ConfirmPassword)
                {
                    AppLogger.Info("Validation failed: Passwords do not match.");
                    MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Password.Length < 8)
                {
                    AppLogger.Info("Validation failed: Password too short.");
                    MessageBox.Show("Password must be at least 8 characters long.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ValidationHelper.IsValidEmail(Email))
                {
                    AppLogger.Info("Validation failed: Invalid email format.");
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Registration
                var success = await AdminRegistrationService.RegisterAdminAsync(Email, Password);

                if (success)
                {
                    AppLogger.Success($"Admin registered successfully: {Email}");
                    MessageBox.Show("Admin registered successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Clear fields
                    Email = string.Empty;
                    Password = string.Empty;
                    ConfirmPassword = string.Empty;

                    // Close current window
                    Application.Current.Windows[^1]?.Close();
                }
                else
                {
                    AppLogger.Error($"Registration failed for admin: {Email}", showToUser: false);
                    MessageBox.Show("Registration failed. Please check the logs.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"An unexpected error occurred during admin registration.{ex.Message}");
            }
        }

        private void EvaluatePasswordCriteria()
        {
            var p = _password ?? string.Empty;
            HasMinLength = p.Length >= 8;
            HasUpper = p.Any(char.IsUpper);
            HasLower = p.Any(char.IsLower);
            HasDigit = p.Any(char.IsDigit);
            HasSpecial = p.Any(c => !char.IsLetterOrDigit(c));
        }

        #endregion

        #region === INotifyPropertyChanged ===

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
    }
}
