using CATERINGMANAGEMENT.ViewModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            DataContext = new ReservationListViewModel();

            Loaded += async (_, _) =>
            {
                if (DataContext is ReservationListViewModel vm)
                    await vm.LoadReservations();
            };
        }
    }
}
