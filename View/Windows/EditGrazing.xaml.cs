using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.GrazingVM;
using System.Windows;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditGrazing : Window
    {
        public GrazingTable? UpdatedGrazing { get; private set; }

        public EditGrazing(GrazingTable item)
        {
            InitializeComponent();

            var vm = new EditGrazingViewModel(item);
            DataContext = vm;
        }
    }
}
