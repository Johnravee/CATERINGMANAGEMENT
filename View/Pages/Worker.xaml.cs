using CATERINGMANAGEMENT.ViewModels;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class Workers : Page
    {
        private readonly WorkerViewModel _viewModel;
        public Workers()
        {
            InitializeComponent();
            AuthGuard.RequireAuthentication(this);
            _viewModel = new WorkerViewModel();
            DataContext = _viewModel;
            Loaded += async (_, __) =>
            {
                if (DataContext is WorkerViewModel vm)
                {
                    await vm.LoadItems();
                }
            };
        }
    }
}
