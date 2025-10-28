using System.Windows;
using CATERINGMANAGEMENT.ViewModels.AuthVM;
using CATERINGMANAGEMENT.View;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class ResetPasswordWindow : Window
    {
        private readonly ResetPasswordViewModel _viewModel;

        public ResetPasswordWindow()
        {
            InitializeComponent();
            _viewModel = new ResetPasswordViewModel();
            _viewModel.RequestClose += OnRequestClose;
            DataContext = _viewModel;
        }

        public ResetPasswordWindow(string accessToken, string? refreshToken)
        {
            InitializeComponent();
            _viewModel = new ResetPasswordViewModel();
            _viewModel.InitializeTokens(accessToken, refreshToken);
            _viewModel.RequestClose += OnRequestClose;
            DataContext = _viewModel;
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
