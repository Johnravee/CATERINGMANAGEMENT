using CATERINGMANAGEMENT.ViewModels;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class Workers : Page
    {
        private readonly WorkersViewModel _viewModel;
        public Workers()
        {
            InitializeComponent();
            _viewModel = new WorkersViewModel();
            DataContext = _viewModel;
            Loaded += async (_, __) =>
            {
                if (DataContext is WorkersViewModel vm)
                {
                    await vm.LoadItems();
                }
            };
        }
    }
}
