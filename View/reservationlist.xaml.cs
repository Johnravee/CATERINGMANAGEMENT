using CATERINGMANAGEMENT.ViewModel;
using System.Windows;

namespace CATERINGMANAGEMENT.View
{
    public partial class reservationlist : Window
    {
        public reservationlist()
        {
            InitializeComponent();
            DataContext = new ReservationListViewModel();

            Loaded += async (_, _) =>
            {
                if (DataContext is ReservationListViewModel vm)
                    await vm.LoadReservations();
            };
        }
    }
}
