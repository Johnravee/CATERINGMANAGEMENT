using CATERINGMANAGEMENT.ViewModels.MotifThemeVM;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    /// <summary>
    /// Interaction logic for ThemeMotif.xaml
    /// </summary>
    public partial class ThemeMotif : Page
    {
        private readonly ThemeMotifViewModel _viewModel;
        public ThemeMotif()
        {
            InitializeComponent();
            _viewModel = new ThemeMotifViewModel();
            DataContext = _viewModel;
        }
    }
}
