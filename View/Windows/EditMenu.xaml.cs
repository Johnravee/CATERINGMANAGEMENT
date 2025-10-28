using System.Windows;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.MenuVM;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditMenu : Window
    {
      

        public EditMenu(MenuOption item)
        {
            InitializeComponent();

            var viewModel = new EditMenuViewModel(item);
            DataContext = viewModel;
        }

       
    }
}
