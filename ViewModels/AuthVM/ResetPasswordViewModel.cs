using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.ViewModels;
using Supabase.Gotrue;

namespace CATERINGMANAGEMENT.ViewModels.AuthVM
{
    public class ResetPasswordViewModel : BaseViewModel
    {
        private string? _accessToken;
        private string? _refreshToken;

        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _isBusy;

        public string NewPassword
        {
            get => _newPassword;
            set { _newPassword = value; OnPropertyChanged(); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand MinimizeCommand { get; }
        public ICommand ExitCommand { get; }

        public event Action? RequestClose;

        public ResetPasswordViewModel()
        {
            ConfirmCommand = new RelayCommand(async () => await ConfirmAsync(), () => !IsBusy);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(), () => !IsBusy);
            MinimizeCommand = new RelayCommand<Window>(w => { if (w != null) w.WindowState = WindowState.Minimized; });
            ExitCommand = new RelayCommand<Window>(w => { Application.Current.Shutdown(); });
        }

        public void InitializeTokens(string accessToken, string? refreshToken)
        {
            _accessToken = accessToken;
            _refreshToken = refreshToken;
        }

        private async Task ConfirmAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;

                if (string.IsNullOrWhiteSpace(_accessToken))
                {
                    AppLogger.Error("Missing recovery token", showToUser: true);
                    RequestClose?.Invoke();
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
                {
                    ShowMessage("Password must be at least 8 characters.", "Validation");
                    return;
                }
                if (NewPassword != ConfirmPassword)
                {
                    ShowMessage("Passwords do not match.", "Validation");
                    return;
                }

                var client = await SupabaseService.GetClientAsync();
                if (client == null)
                {
                    AppLogger.Error("Unable to connect to authentication service.", showToUser: true);
                    return;
                }

                var session = await client.Auth.SetSession(_accessToken!, _refreshToken ?? string.Empty);
                if (session?.User == null)
                {
                    ShowMessage("Invalid or expired recovery link. Please request a new one.", "Reset Password");
                    return;
                }

                SessionService.SetSession(session);
                await client.Auth.Update(new UserAttributes { Password = NewPassword });
                await client.Auth.SignOut();
                SessionService.ClearSession();

                AppLogger.Success("Password updated successfully.");
                ShowMessage("Password updated successfully. Please log in with your new password.", "Reset Password");

                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to update password");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
