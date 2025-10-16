using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.KitchenVM;
using System.Windows;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditKitchenItem : Window
    {
        public Kitchen? KitchenItem { get; private set; }

        public EditKitchenItem(Kitchen existingItem, KitchenViewModel parentVM)
        {
            InitializeComponent();

            var viewModel = new EditKitchenItemViewModel(existingItem, parentVM);
            DataContext = viewModel;
        }
    }
}
