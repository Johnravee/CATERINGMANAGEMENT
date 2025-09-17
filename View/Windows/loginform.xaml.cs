using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
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

namespace CATERINGMANAGEMENT.View
{

    public partial class LoginView : Window
    {

        private readonly LoginViewModel _viewModel;
        public LoginView()
        {
            InitializeComponent();
            //AuthGuard.PreventAccessIfAuthenticated(this);
            _viewModel = new LoginViewModel(this);
            DataContext = _viewModel;

            PasswordBox.PasswordChanged += (s, e) =>
            {
                _viewModel.Password = PasswordBox.Password;
            };
        }
        private void ExitAppBtnHandler(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeAppBtnHandler(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }


      

        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new Registration();
            registerWindow.Show();
            this.Close();   
        }

        
    }
}
