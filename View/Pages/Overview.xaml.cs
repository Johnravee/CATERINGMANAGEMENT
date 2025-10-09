using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.ViewModels;
using System.Threading.Tasks;
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

            
            DataContext = _viewModel;

        }

      

    }
}
