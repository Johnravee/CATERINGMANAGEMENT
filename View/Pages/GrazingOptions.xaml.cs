using CATERINGMANAGEMENT.ViewModels.GrazingVM;
using System.Windows.Controls;


namespace CATERINGMANAGEMENT.View.Pages
{
    /// <summary>
    /// Interaction logic for GrazingOptions.xaml
    /// </summary>
    public partial class GrazingOptions : Page
    {
        private readonly GrazingViewModel _viewModel;
        public GrazingOptions()
        {
            InitializeComponent();
            _viewModel = new GrazingViewModel();
            DataContext = _viewModel;
        }
    }
}
