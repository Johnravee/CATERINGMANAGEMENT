using CATERINGMANAGEMENT.ViewModels.ReservationVM;
using System.Windows;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class ChecklistBuilder : Window
    {
        public ChecklistBuilder()
        {
            InitializeComponent();
            var viewModel = new ChecklistBuilderViewModel();
            DataContext = viewModel;
        }
    }
}
