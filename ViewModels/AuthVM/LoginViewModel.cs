using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;

namespace CATERINGMANAGEMENT.ViewModels.AuthVM
{
    internal class LoginViewModel : INotifyPropertyChanged
    {
        private string _email;
        private string _password;
        private bool _showPassword;

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public bool ShowPassword
        {
            get => _showPassword;
            set { _showPassword = value; OnPropertyChanged(); }
        }

        public bool IsNotLoading => !IsLoading;
        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsNotLoading));
                }
            }
        }

        public ICommand LoginCommand { get; }
        private readonly Window _currentWindow;
        public LoginViewModel(Window currentWindow)
        {
            LoginCommand = new RelayCommand(async () => await LoginAsync());
            _currentWindow = currentWindow;
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Please fill in both fields.");
                return;
            }

            try
            {
                IsLoading = true;

                var user = await AuthService.LoginAsync(Email, Password);

                if (user != null)
                {
                    var dashboard = new Dashboard();
                    dashboard.Show();
                    _currentWindow.Close();
                }
                else
                {
                    MessageBox.Show("Login failed or unauthorized. Only admins can log in here.");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
