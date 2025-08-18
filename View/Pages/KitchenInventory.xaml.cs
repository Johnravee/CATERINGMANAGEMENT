using System.Windows.Controls;
using CATERINGMANAGEMENT.ViewModels;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class KitchenInventory : Page
    {
        private readonly KitchenViewModel _viewModel;

        public KitchenInventory()
        {
            InitializeComponent();
            _viewModel = new KitchenViewModel();
            DataContext = _viewModel;

            Loaded += async (_, __) => { await _viewModel.LoadItems(); };
        }
    }
}
