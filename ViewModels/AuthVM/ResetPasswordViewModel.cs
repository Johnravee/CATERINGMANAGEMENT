using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.ViewModels;
using Supabase.Gotrue;
using System.Linq;

namespace CATERINGMANAGEMENT.ViewModels.AuthVM
{
    public class ResetPasswordViewModel : BaseViewModel
    {
        private string? _accessToken;
        private string? _refreshToken;

        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _isBusy;

        // Password criteria flags (for UI binding similar to registration)
        private bool _hasUpper;
        private bool _hasLower;
        private bool _hasDigit;
        private bool _hasSpecial;
        private bool _hasMinLength;

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                _newPassword = value;
                OnPropertyChanged();
                EvaluatePasswordCriteria();
            }
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

        // Expose criteria for visual hints (checkboxes/indicators) like registration
        public bool HasUpper { get => _hasUpper; private set { _hasUpper = value; OnPropertyChanged(); } }
        public bool HasLower { get => _hasLower; private set { _hasLower = value; OnPropertyChanged(); } }
        public bool HasDigit { get => _hasDigit; private set { _hasDigit = value; OnPropertyChanged(); } }
        public bool HasSpecial { get => _hasSpecial; private set { _hasSpecial = value; OnPropertyChanged(); } }
        public bool HasMinLength { get => _hasMinLength; private set { _hasMinLength = value; OnPropertyChanged(); } }

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

        private void EvaluatePasswordCriteria()
        {
            var p = _newPassword ?? string.Empty;
            HasMinLength = p.Length >= 8;
            HasUpper = p.Any(char.IsUpper);
            HasLower = p.Any(char.IsLower);
            HasDigit = p.Any(char.IsDigit);
            HasSpecial = p.Any(c => !char.IsLetterOrDigit(c));
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

                if (string.IsNullOrWhiteSpace(NewPassword))
                {
                    ShowMessage("Password is required.", "Validation");
                    return;
                }

                // Centralized strict password validation
                var (ok, err) = ValidationHelper.ValidatePassword(NewPassword, minLength: 8, requireUpper: true, requireLower: true, requireDigit: true, requireSpecial: true);
                if (!ok)
                {
                    ShowMessage(err!, "Validation");
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
