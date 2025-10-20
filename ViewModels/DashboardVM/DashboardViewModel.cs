using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View;
using CATERINGMANAGEMENT.View.Windows;
using Supabase.Gotrue;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.DashboardVM
{
    class DashboardViewModel : INotifyPropertyChanged
    {
        private User? _currentUser;
        private string? _role;

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
        public ICommand SignOutCommand { get; }
        public DashboardViewModel()
        {
            // Get the current user from SessionService
            CurrentUser = SessionService.CurrentUser;

            // Extract the role from UserMetadata dictionary
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

        private async void SignOut()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.Auth.SignOut();

                // Clear the current user session
                SessionService.ClearSession();

                // Redirect to login view
                var loginView = new LoginView();
                loginView.Show();

                // Close the current dashboard window
                foreach (var window in System.Windows.Application.Current.Windows)
                {
                    if (window is Dashboard)
                    {
                        ((System.Windows.Window)window).Close();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle logout failure (log, show dialog, etc.)
                Console.WriteLine($"Sign out failed: {ex.Message}");
            }
        }




        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}