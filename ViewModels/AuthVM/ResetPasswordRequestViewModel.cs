using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.ViewModels;

namespace CATERINGMANAGEMENT.ViewModels.AuthVM
{
    public class ResetPasswordRequestViewModel : BaseViewModel
    {
        private string _email = string.Empty;
        private bool _isBusy;

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand SendCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand MinimizeCommand { get; }
        public ICommand ExitCommand { get; }

        public event Action? RequestClose;

        public ResetPasswordRequestViewModel()
        {
            SendCommand = new RelayCommand(async () => await SendAsync(), () => !IsBusy);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(), () => !IsBusy);
            MinimizeCommand = new RelayCommand<Window>(w => { if (w != null) w.WindowState = WindowState.Minimized; });
            ExitCommand = new RelayCommand<Window>(w => { Application.Current.Shutdown(); });
        }

        private async Task SendAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;

                var email = Email?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(email))
                {
                    ShowMessage("Please enter your email.", "Reset Password");
                    return;
                }

                var ok = await AuthService.RequestPasswordResetAsync(email);
                if (ok)
                {
                    AppLogger.Success("Password reset link requested.");
                    ShowMessage("If that email exists, we sent a reset link.", "Reset Password");

                    // Close the window after a successful request
                    RequestClose?.Invoke();
                }
                else
                {
                    AppLogger.Error("Failed to send reset link.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Password reset request failed");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
