using System.Windows;
using CATERINGMANAGEMENT.ViewModels;

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
