using System;
using System.Windows;
using CATERINGMANAGEMENT.Services;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class ResetPasswordRequestWindow : Window
    {
        public ResetPasswordRequestWindow()
        {
            InitializeComponent();
        }

        private async void OnSendLink(object sender, RoutedEventArgs e)
        {
            var email = EmailBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please enter your email.");
                return;
            }

            var ok = await AuthService.RequestPasswordResetAsync(email);
            if (ok)
            {
                MessageBox.Show("If that email exists, we sent a reset link.");
                Close();
            }
            else
            {
                MessageBox.Show("Failed to send reset link. Please try again.");
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
