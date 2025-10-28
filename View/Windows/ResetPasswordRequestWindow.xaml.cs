using System.Windows;
using CATERINGMANAGEMENT.ViewModels.AuthVM;
using CATERINGMANAGEMENT.View; // for LoginView

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class ResetPasswordRequestWindow : Window
    {
        private readonly ResetPasswordRequestViewModel _viewModel;

        public ResetPasswordRequestWindow()
        {
            InitializeComponent();
            _viewModel = new ResetPasswordRequestViewModel();
            _viewModel.RequestClose += OnRequestClose;
            DataContext = _viewModel;
        }

        // Overload to pre-fill the email
        public ResetPasswordRequestWindow(string email) : this()
        {
            _viewModel.Email = email?.Trim() ?? string.Empty;
        }

        private void OnRequestClose()
        {
            var login = new LoginView();
            login.Show();
            Application.Current.MainWindow = login;
            this.Close();
        }
    }
}
