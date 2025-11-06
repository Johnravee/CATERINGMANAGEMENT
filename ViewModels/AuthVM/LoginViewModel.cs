using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using System;

namespace CATERINGMANAGEMENT.ViewModels.AuthVM
{
    internal class LoginViewModel : INotifyPropertyChanged
    {
        private string _email;
        private string _password;
        private bool _showPassword;

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

        public bool ShowPassword
        {
            get => _showPassword;
            set { _showPassword = value; OnPropertyChanged(); }
        }

        // Dynamic app/branding name from environment
        public string AppName { get; } =
            Environment.GetEnvironmentVariable("APP_NAME")?.Trim()
            ?? Environment.GetEnvironmentVariable("BRAND_NAME")?.Trim()
            ?? "CaterMate Management";

        public bool IsNotLoading => !IsLoading;
        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsNotLoading));
                }
            }
        }

        public ICommand LoginCommand { get; }
        private readonly Window _currentWindow;
        public LoginViewModel(Window currentWindow)
        {
            LoginCommand = new RelayCommand(async () => await LoginAsync());
            _currentWindow = currentWindow;
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Please fill in both fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                var result = await AuthService.LoginAsync(Email, Password);

                if (result.User != null)
                {
                    var dashboard = new Dashboard();
                    dashboard.Show();
                    _currentWindow.Close();
                    return;
                }

                // Prefer our own copy for known errors; only use result.Message for UnknownError
                string message = result.Error switch
                {
                    LoginErrorCode.UnverifiedEmail => "Your email is not verified. Please check your inbox for the confirmation link.",
                    LoginErrorCode.NotAdmin => "Your account does not have admin access. Only admins can log in here.",
                    LoginErrorCode.InvalidCredentials => "The credentials entered are incorrect. Please verify your email and password and try again.",
                    LoginErrorCode.NetworkError => "Network error. Please check your internet connection and try again.",
                    _ => string.IsNullOrWhiteSpace(result.Message) ? "Login failed due to an unexpected error." : result.Message!
                };

                MessageBox.Show(message, "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
