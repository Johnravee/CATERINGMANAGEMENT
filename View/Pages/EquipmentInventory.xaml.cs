using System.Windows.Controls;
using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.ViewModels.EquipmentsVM;


namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class EquipmentsInventory : Page
    {
        private readonly EquipmentViewModel _viewModel;

        public EquipmentsInventory()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            _viewModel = new EquipmentViewModel();
            DataContext = _viewModel;

            Loaded += async (_, __) =>
            {
                await _viewModel.LoadPage(1);
            };
        }
    }
}
