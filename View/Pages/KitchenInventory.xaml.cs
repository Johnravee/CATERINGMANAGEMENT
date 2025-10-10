using CATERINGMANAGEMENT.ViewModels.KitchenVM;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class KitchenInventory : Page
    {
        private readonly KitchenViewModel _viewModel;

        public KitchenInventory()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            _viewModel = new KitchenViewModel();
            DataContext = _viewModel;

            Loaded += async (_, __) => { await _viewModel.LoadItems(); };
        }

       

    
    }
}
