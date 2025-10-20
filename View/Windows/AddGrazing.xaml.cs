using CATERINGMANAGEMENT.ViewModels.GrazingVM;
using System.Windows;


namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for AddGrazing.xaml
    /// </summary>
    public partial class AddGrazing : Window
    {
        private readonly AddGrazingViewModel _viewModel;
        public AddGrazing()
        {
            InitializeComponent();
            _viewModel = new AddGrazingViewModel();
            DataContext = _viewModel;
        }
    }
}
