using CATERINGMANAGEMENT.ViewModels.DashboardVM;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    /// <summary>
    /// Interaction logic for Overview.xaml
    /// PURPOSE: Connects OverviewViewModel to Overview.xaml, enabling data binding
    ///          for dashboard counters, charts, and upcoming reservations.
    /// RESPONSIBILITY: Initializes components, sets DataContext, and passes
    ///                 chart references to ViewModel.
    /// </summary>
    public partial class Overview : Page
    {
        private readonly OverviewViewModel _viewModel;

        public Overview()
        {
            InitializeComponent();

            // Initialize ViewModel
            _viewModel = new OverviewViewModel
            {
                // Assign chart controls from XAML to ViewModel
                ReservationChartElement = ReservationChart,
                EventTypeChartElement = EventTypeChart
            };

            // Set DataContext for data binding
            DataContext = _viewModel;
        }
    }
}
