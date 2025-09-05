using CATERINGMANAGEMENT.ViewModel;

using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    /// <summary>
    /// Interaction logic for ReservationListPage.xaml
    /// </summary>
    public partial class ReservationListPage : Page
    {
        public ReservationListPage()
        {
            InitializeComponent();
            AuthGuard.RequireAuthentication(this);
            DataContext = new ReservationListViewModel();

            Loaded += async (_, _) =>
            {
                if (DataContext is ReservationListViewModel vm)
                    await vm.LoadReservations();
            };
        }

       
    }
}
