using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.View.Pages;
using CATERINGMANAGEMENT.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {
        private readonly DashboardViewModel _viewModel;
        public Dashboard()
        {
            InitializeComponent();
            AuthGuard.RequireAuthentication(this);
            MainFrame.Navigate(new Overview());
            _viewModel = new DashboardViewModel();
            DataContext = _viewModel;
            Loaded += Dashboard_Loaded;
        }
        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            bool hasProfile = await ProfileGuard.HasProfileAsync();

            if (!hasProfile)
            {
                MessageBox.Show(
                            "You don't have a profile yet. Redirecting to profile setup...",
                            "Redirecting",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);



                new AddProfile().Show();
                this.Close(); 
                return;
            }

            // Navigate only if user has a profile
            MainFrame.Navigate(new Overview());
        }


        private void HandleDashboard_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
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

        private void BtnSchedule_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Schedule());
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

        private void MaximizeRestoreAppBtnHandler(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                MaximizeIcon.Text = "❐"; // Change icon to "restore" shape
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaximizeIcon.Text = "□"; // Back to maximize shape
            }
        }

        private void BtnChat_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ChatMessage());
        }
    }
}
