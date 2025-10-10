using System.Windows;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.PackageVM;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditPackage : Window
    {
        public Package UpdatedPackage { get; private set; }

        public EditPackage(Package item)
        {
            InitializeComponent();

            var vm = new EditPackageViewModel(item);
            vm.RequestClose += Vm_RequestClose;
            DataContext = vm;
        }

        private void Vm_RequestClose(bool isSaved)
        {
            if (isSaved && DataContext is EditPackageViewModel vm)
            {
                UpdatedPackage = vm.ResultPackage;
                DialogResult = true;
            }
            else
            {
                DialogResult = false;
            }

            Close();
        }
    }
}
