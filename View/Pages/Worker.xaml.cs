using CATERINGMANAGEMENT.ViewModels.WorkerVM;
using CATERINGMANAGEMENT.Helpers;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class Workers : Page
    {
        public Workers()
        {
            InitializeComponent();
            if (!AuthGuard.RequireAuthentication(this))
                return;
           var viewModel = new WorkerViewModel();
            DataContext = viewModel;
           
        }
    }
}
