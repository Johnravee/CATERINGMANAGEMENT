using CATERINGMANAGEMENT.ViewModels.DashboardVM;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class Overview : Page
    {
        private readonly OverviewViewModel _viewModel;

        public Overview()
        {
            InitializeComponent();

            _viewModel = new OverviewViewModel();

            // Assign chart controls from XAML to ViewModel
            _viewModel.ReservationChartElement = ReservationChart;
            _viewModel.EventTypeChartElement = EventTypeChart;

            DataContext = _viewModel;
        }
    }
}
