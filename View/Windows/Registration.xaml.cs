using CATERINGMANAGEMENT.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for Registration.xaml
    /// </summary>
    public partial class Registration : Window
    {
        private readonly RegistrationViewModel _viewModel;
        public Registration()
        {
            InitializeComponent();
            _viewModel = new RegistrationViewModel();
            DataContext = _viewModel;
            
            PasswordBox.PasswordChanged += (s, e) =>
            {
                _viewModel.Password = PasswordBox.Password;
            };

            ConfirmPasswordBox.PasswordChanged += (s, e) =>
            {
                _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            };
        }

        private void MinimizeAppBtnHandler(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ExitAppBtnHandler(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginView();
            loginWindow.Show();
            this.Close();
        }

    }
}
