using System.Windows;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.PackageVM;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditPackage : Window
    {

        public EditPackage(Package item)
        {
            InitializeComponent();
            var viewModel = new EditPackageViewModel(item);
            DataContext = viewModel;
        }

      
    }
}
