using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
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
        public LoginView()
        {
            InitializeComponent();
        }
        private void ExitAppBtnHandler(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeAppBtnHandler(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }


        private async void handleLoginBtn(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                // Example: fetch reservations (assuming you created a model)
                var response = await client.From<Reservation>().Get(); 
                var data = response.Models;

                MessageBox.Show($"✅ Retrieved {data.Count} reservations");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error: {ex.Message}");
            }
        }

        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new Registration();
            registerWindow.Show();
            this.Close();   
        }
    }
}
