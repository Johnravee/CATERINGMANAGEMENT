using System.Windows;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.MenuVM;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditMenu : Window
    {
        public MenuOption UpdatedMenu { get; private set; }

        public EditMenu(MenuOption item)
        {
            InitializeComponent();

            var vm = new EditMenuViewModel(item);
            vm.RequestClose += Vm_RequestClose;
            DataContext = vm;
        }

        private void Vm_RequestClose(bool isSaved)
        {
            if (isSaved && DataContext is EditMenuViewModel vm)
            {
                UpdatedMenu = vm.ResultMenu;
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
