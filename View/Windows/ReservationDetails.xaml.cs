
using System.Windows;
using CATERINGMANAGEMENT.Models;

namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for ReservationDetails.xaml
    /// </summary>
    public partial class ReservationDetails : Window
    {
        public ReservationDetails(Reservation reservation)
        {
            DataContext = reservation;
            InitializeComponent();
        }
    }
}
