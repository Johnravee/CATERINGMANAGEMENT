using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.EquipmentsVM;
using System.Windows;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EditEquipments : Window
    {
        public EditEquipments(Equipment equipment)
        {
            InitializeComponent();
            var viewModel = new EditEquipmentViewModel(equipment);
            DataContext = viewModel;
        }
    }
}
