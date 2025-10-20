using System.Windows;
using CATERINGMANAGEMENT.ViewModels.PackageVM;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class AddPackage : Window
    {
        public AddPackage()
        {
            InitializeComponent();
            DataContext = new AddPackageViewModel();
        }
    }
}
