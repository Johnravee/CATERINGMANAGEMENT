using System.Windows;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.MotifThemeVM;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditThemeMotif : Window
    {
      

        public EditThemeMotif(ThemeMotif item)
        {
            InitializeComponent();

            var viewModel = new EditThemeMotifViewModel(item);
            DataContext = viewModel;
        }

       
    }
}
