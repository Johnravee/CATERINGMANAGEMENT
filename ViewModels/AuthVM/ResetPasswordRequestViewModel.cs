using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.ViewModels;
using System.Linq;

namespace CATERINGMANAGEMENT.ViewModels.AuthVM
{
    public class ResetPasswordRequestViewModel : BaseViewModel
    {
        private string _email = string.Empty;
        private bool _isBusy;
        private bool _isInCooldown;
        private int _cooldownSeconds;
        private DispatcherTimer? _cooldownTimer;

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSend));
                OnPropertyChanged(nameof(SendButtonText));
                // Ensure WPF re-queries command can-execute state
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsInCooldown
        {
            get => _isInCooldown;
            private set
            {
                if (_isInCooldown == value) return;
                _isInCooldown = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSend));
                OnPropertyChanged(nameof(SendButtonText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public int CooldownSeconds
        {
            get => _cooldownSeconds;
            private set
            {
                if (_cooldownSeconds == value) return;
                _cooldownSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SendButtonText));
            }
        }

        public bool CanSend => !IsBusy && !IsInCooldown;

        public string SendButtonText
            => IsBusy ? "Sending..." : IsInCooldown ? $"Resend in {CooldownSeconds}s" : "Send Link";

        public ICommand SendCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand MinimizeCommand { get; }
        public ICommand ExitCommand { get; }

        public event Action? RequestClose;

        public ResetPasswordRequestViewModel()
        {
            SendCommand = new RelayCommand(async () => await SendAsync(), () => CanSend);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(), () => !IsBusy);
            MinimizeCommand = new RelayCommand<Window>(w => { if (w != null) w.WindowState = WindowState.Minimized; });
            ExitCommand = new RelayCommand<Window>(w => { Application.Current.Shutdown(); });
        }

        private async Task SendAsync()
        {
            if (!CanSend) return;
            try
            {
                IsBusy = true; // keep loader on even during MessageBox

                var email = Email?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(email))
                {
                    ShowMessage("Please enter your email.", "Reset Password");
                    return;
                }

                if (!ValidationHelper.IsValidEmail(email))
                {
                    ShowMessage("Please enter a valid email address.", "Reset Password");
                    return;
                }

                var ok = await AuthService.RequestPasswordResetAsync(email);
                if (ok)
                {
                    AppLogger.Success("Password reset link requested.");
                    ShowMessage("Check your email for the reset link. If you don't see it, check your Spam/Junk folder.", "Reset Password");
                    StartCooldown(60); // prevent duplicate requests for 30s
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

        private void StartCooldown(int seconds)
        {
            IsInCooldown = true;
            CooldownSeconds = seconds;

            _cooldownTimer?.Stop();
            _cooldownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _cooldownTimer.Tick += (s, e) =>
            {
                if (CooldownSeconds > 0)
                {
                    CooldownSeconds--;
                }
                if (CooldownSeconds <= 0)
                {
                    _cooldownTimer?.Stop();
                    IsInCooldown = false;
                }
            };
            _cooldownTimer.Start();
        }
    }
}
