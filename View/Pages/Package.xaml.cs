using CATERINGMANAGEMENT.ViewModels.PackageVM;
using System.Windows.Controls;


namespace CATERINGMANAGEMENT.View.Pages
{
    /// <summary>
    /// Interaction logic for Package.xaml
    /// </summary>
    public partial class Package : Page
    {
        private PackageViewModel viewModel;
        public Package()
        {
            InitializeComponent();
            viewModel = new PackageViewModel();
            this.DataContext = viewModel;
        }
    }
}
