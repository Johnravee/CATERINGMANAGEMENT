using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using CATERINGMANAGEMENT.ViewModels.AuthVM;
using CATERINGMANAGEMENT.Helpers;
using System;
using System.Windows;

namespace CATERINGMANAGEMENT.View
{

    public partial class LoginView : Window
    {

        private readonly LoginViewModel _viewModel;
        public LoginView()
        {
            InitializeComponent();

            // Ensure custom URI protocol is registered when login opens
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            UriProtocolRegistrar.EnsureRegistered("cater", exePath);

            // Removed AuthGuard.PreventAccessIfAuthenticated(this); to avoid pop-ups during reset.
            _viewModel = new LoginViewModel(this);
            DataContext = _viewModel;

            PasswordBox.PasswordChanged += (s, e) =>
            {
                _viewModel.Password = PasswordBox.Password;
            };

            // Keep PasswordBox and PasswordTextBox in sync when toggling ShowPassword
            this.Loaded += (s, e) =>
            {
                // When user types in visible TextBox, update PasswordBox too
                PasswordTextBox.TextChanged += (s2, e2) =>
                {
                    if (_viewModel.ShowPassword)
                    {
                        if (PasswordBox.Password != PasswordTextBox.Text)
                            PasswordBox.Password = PasswordTextBox.Text;
                    }
                };
            };
        }
        private void ExitAppBtnHandler(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeAppBtnHandler(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new Registration();
            registerWindow.Show();
            this.Close();   
        }

        private async void ForgotPassword_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var currentEmail = _viewModel?.Email?.Trim() ?? EmailBox.Text?.Trim() ?? string.Empty;
                var requestWin = string.IsNullOrWhiteSpace(currentEmail)
                    ? new ResetPasswordRequestWindow()
                    : new ResetPasswordRequestWindow(currentEmail);

                // Show reset window and make it the main window, then close login
                requestWin.Show();
                Application.Current.MainWindow = requestWin;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

    }
}
