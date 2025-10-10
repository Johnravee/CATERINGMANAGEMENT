using CATERINGMANAGEMENT.ViewModels.MenuVM;
using System.Windows.Controls;


namespace CATERINGMANAGEMENT.View.Pages
{
    /// <summary>
    /// Interaction logic for Menu.xaml
    /// </summary>
    public partial class Menu : Page
    {
        private readonly MenuViewModel _viewModel;
        public Menu()
        {
            InitializeComponent();
            _viewModel = new MenuViewModel();
            DataContext = _viewModel;
        }
    }
}
