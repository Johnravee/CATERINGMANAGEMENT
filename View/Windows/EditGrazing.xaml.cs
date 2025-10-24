using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.GrazingVM;
using System.Windows;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditGrazing : Window
    {

        public EditGrazing(GrazingTable item)
        {
            InitializeComponent();

            var viewModel = new EditGrazingViewModel(item);
            DataContext = viewModel;
        }
    }
}
