using CATERINGMANAGEMENT.View.Pages;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {
        public Dashboard()
        {
            InitializeComponent();
            MainFrame.Navigate(new Overview());
        }

        private void BtnOverview_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Overview());
        }

        private void BtnReservations_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReservationListPage());
        }

        private void BtnEquipment_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new EquipmentsInventory());
        }

        private void BtnKitchen_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new KitchenInventory());
        }

        private void BtnWorkers_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Workers());
        }


        private void ExitAppBtnHandler(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeAppBtnHandler(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
