using System;
using System.Windows;
using System.Windows.Controls;
using CATERINGMANAGEMENT.Services;
using Supabase.Gotrue;
using CATERINGMANAGEMENT.View;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class ResetPasswordWindow : Window
    {
        private readonly string? _accessToken;
        private readonly string? _refreshToken;

        // Local references resolved after XAML is loaded
        private PasswordBox? _passwordBoxNew;
        private PasswordBox? _passwordBoxConfirm;

        public ResetPasswordWindow()
        {
            InitializeComponent();
            _passwordBoxNew = FindName("PasswordBoxNew") as PasswordBox;
            _passwordBoxConfirm = FindName("PasswordBoxConfirm") as PasswordBox;
        }

        public ResetPasswordWindow(string accessToken, string? refreshToken)
        {
            InitializeComponent();

            _accessToken = accessToken;
            _refreshToken = refreshToken;

            // Resolve named controls from XAML after it's loaded
            _passwordBoxNew = FindName("PasswordBoxNew") as PasswordBox;
            _passwordBoxConfirm = FindName("PasswordBoxConfirm") as PasswordBox;
        }

        private async void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    MessageBox.Show("Invalid or missing recovery token. Please request a new reset link.");
                    Close();
                    return;
                }

                var newPassword = _passwordBoxNew?.Password ?? string.Empty;
                var confirm = _passwordBoxConfirm?.Password ?? string.Empty;
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    MessageBox.Show("Password must be at least 6 characters.");
                    return;
                }
                if (newPassword != confirm)
                {
                    MessageBox.Show("Passwords do not match.");
                    return;
                }

                var client = await SupabaseService.GetClientAsync();
                if (client == null)
                {
                    MessageBox.Show("Unable to connect to authentication service.");
                    return;
                }

                // Establish session from recovery token and update password
                var session = await client.Auth.SetSession(_accessToken!, _refreshToken ?? string.Empty);
                if (session?.User == null)
                {
                    MessageBox.Show("Invalid or expired recovery link. Please request a new one.");
                    return;
                }

                // Set app-level session so any guards see a logged-in user during this flow
                SessionService.SetSession(session);

                await client.Auth.Update(new UserAttributes { Password = newPassword });

                // Sign out after successful update to force re-login with the new password
                await client.Auth.SignOut();
                SessionService.ClearSession();

                MessageBox.Show("Password updated successfully. Please log in with your new password.");

                try
                {
                    var login = new LoginView();
                    login.Show();
                }
                catch { }

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update password: {ex.Message}");
            }
        }
    }
}
