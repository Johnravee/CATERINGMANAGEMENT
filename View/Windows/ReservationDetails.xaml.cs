using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.ReservationVM;
using System.Windows;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class ReservationDetails : Window
    {
        private readonly ReservationDetailsViewModel _viewModel;
        public ReservationDetails(Reservation reservation)
        {
            InitializeComponent();
            _viewModel = new ReservationDetailsViewModel(reservation);
            DataContext = _viewModel;
        }

       
    }
}
