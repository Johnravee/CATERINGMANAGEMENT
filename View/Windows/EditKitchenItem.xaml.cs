using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.KitchenVM;
using System.Windows;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditKitchenItem : Window
    {
        public EditKitchenItem(Kitchen existingItem)
        {
            InitializeComponent();

            var viewModel = new EditKitchenItemViewModel(existingItem);
            DataContext = viewModel;
        }
    }
}
