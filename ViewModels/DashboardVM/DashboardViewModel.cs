/*
 * FILE: DashboardViewModel.cs
 * PURPOSE: ViewModel for the Dashboard/Main window.
 *          Responsibilities:
 *          - Expose the current user and role information.
 *          - Handle user sign-out with session clearing.
 *          - Provide commands for UI interactions.
 *          - Log important events and errors.
 */

using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View;
using Supabase.Gotrue;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.DashboardVM
{
    class DashboardViewModel : BaseViewModel
    {
        #region Fields
        private User? _currentUser;
        private string? _role;
        #endregion

        #region Properties
        public User? CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }

        public string? AdminRole
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand SignOutCommand { get; }
        #endregion

        #region Constructor
        public DashboardViewModel()
        {
            // Initialize current user from session
            CurrentUser = SessionService.CurrentUser;

            // Extract role from user metadata
            if (CurrentUser?.UserMetadata != null &&
                CurrentUser.UserMetadata.TryGetValue("role", out var roleObj))
            {
                AdminRole = roleObj?.ToString();
            }
            else
            {
                AdminRole = "No role assigned";
            }

            SignOutCommand = new RelayCommand(SignOut);
        }
        #endregion

        #region Methods
        private async void SignOut()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.Auth.SignOut();

                // Clear session
                SessionService.ClearSession();
                AppLogger.Info("User signed out successfully.");

                // Open login view
                var loginView = new LoginView();
                loginView.Show();

                // Close current dashboard window
                foreach (var window in System.Windows.Application.Current.Windows)
                {
                    if (window is View.Windows.Dashboard dashboard)
                    {
                        dashboard.Close();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Sign out failed.");
            }
        }
        #endregion
    }
}
