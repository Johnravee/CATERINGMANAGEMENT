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
            UriProtocolRegistrar.EnsureRegistered("oshdy", exePath);

            // Removed AuthGuard.PreventAccessIfAuthenticated(this); to avoid pop-ups during reset.
            _viewModel = new LoginViewModel(this);
            DataContext = _viewModel;

            PasswordBox.PasswordChanged += (s, e) =>
            {
                _viewModel.Password = PasswordBox.Password;
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

        private void ForgotPassword_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // Prefer the new request window so user can confirm the email
                var requestWin = new ResetPasswordRequestWindow();
                requestWin.Owner = this;
                requestWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

    }
}
