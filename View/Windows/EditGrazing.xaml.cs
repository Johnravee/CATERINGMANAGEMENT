using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.GrazingVM;
using System.Windows;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditGrazing : Window
    {
        public GrazingTable UpdatedGrazing { get; private set; }

        public EditGrazing(GrazingTable item)
        {
            InitializeComponent();

            var vm = new EditGrazingViewModel(item);
            vm.RequestClose += Vm_RequestClose;
            DataContext = vm;
        }

        private void Vm_RequestClose(bool isSaved)
        {
            if (isSaved && DataContext is EditGrazingViewModel vm)
            {
                UpdatedGrazing = vm.ResultGrazing;
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
