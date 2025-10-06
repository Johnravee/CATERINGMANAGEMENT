using CATERINGMANAGEMENT.ViewModels;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class Overview : Page
    {
        private readonly OverviewViewModel _viewModel;

        public Overview()
        {
            InitializeComponent();

        
            _viewModel = new OverviewViewModel();

            
            DataContext = _viewModel;

        }

    }
}
