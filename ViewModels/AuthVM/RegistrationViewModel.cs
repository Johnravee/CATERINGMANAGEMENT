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
        private string _confirmEmail = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;

        private bool _hasUpper;
        private bool _hasLower;
        private bool _hasDigit;
        private bool _hasSpecial;
        private bool _hasMinLength;

        private bool _isLoading;

        #endregion

        #region === Public Properties ===

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string ConfirmEmail
        {
            get => _confirmEmail;
            set { _confirmEmail = value; OnPropertyChanged(); }
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

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoading)); }
        }
        public bool IsNotLoading => !IsLoading;

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
                IsLoading = true;
                AppLogger.Info("Attempting to register new admin...");

                var email = Email?.Trim() ?? string.Empty;
                var confirmEmail = ConfirmEmail?.Trim() ?? string.Empty;

                // Validation
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(Password))
                {
                    AppLogger.Info("Validation failed: Missing email or password.");
                    MessageBox.Show("Email and password are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ValidationHelper.IsValidEmail(email))
                {
                    AppLogger.Info("Validation failed: Invalid email format.");
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.Equals(email, confirmEmail, StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Info("Validation failed: Emails do not match.");
                    MessageBox.Show("Emails do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Password != ConfirmPassword)
                {
                    AppLogger.Info("Validation failed: Passwords do not match.");
                    MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Centralized strict password validation
                var (ok, err) = ValidationHelper.ValidatePassword(Password, minLength: 8, requireUpper: true, requireLower: true, requireDigit: true, requireSpecial: true);
                if (!ok)
                {
                    AppLogger.Info($"Validation failed: {err}");
                    MessageBox.Show(err!, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Registration via Supabase signup (sends confirmation email)
                var success = await AdminRegistrationService.RegisterAdminAsync(email, Password);

                if (success)
                {
                    AppLogger.Success($"Sign-up successful: {email}");
                    MessageBox.Show("Signup successful. Please check your email to confirm your account.", "Check your email", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Close current window to return to login
                    Application.Current.Windows[^1]?.Close();

                    // Clear fields
                    Email = string.Empty;
                    ConfirmEmail = string.Empty;
                    Password = string.Empty;
                    ConfirmPassword = string.Empty;
                }
                else
                {
                    AppLogger.Error($"Signup failed for: {email}", showToUser: false);
                    MessageBox.Show("Signup failed. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"An unexpected error occurred during admin sign-up.{ex.Message}");
            }
            finally
            {
                IsLoading = false;
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
