using System.Windows.Controls;
using CATERINGMANAGEMENT.ViewModels;


namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class EquipmentsInventory : Page
    {
        private readonly EquipmentsViewModel _viewModel;

        public EquipmentsInventory()
        {
            InitializeComponent();
            _viewModel = new EquipmentsViewModel();
            DataContext = _viewModel;

            Loaded += async (_, __) =>
            {
                await _viewModel.LoadEquipments();
            };
        }

     
    }
}
