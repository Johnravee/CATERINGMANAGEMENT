using System.Windows;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditThemeMotif : Window
    {
        public ThemeMotif UpdatedThemeMotif { get; private set; }

        public EditThemeMotif(ThemeMotif item)
        {
            InitializeComponent();

            var vm = new EditThemeMotifViewModel(item);
            vm.RequestClose += Vm_RequestClose;
            DataContext = vm;
        }

        private void Vm_RequestClose(bool isSaved)
        {
            if (isSaved && DataContext is EditThemeMotifViewModel vm)
            {
                UpdatedThemeMotif = vm.ResultThemeMotif;
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
