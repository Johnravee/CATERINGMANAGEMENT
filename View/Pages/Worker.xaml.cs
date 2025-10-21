using CATERINGMANAGEMENT.ViewModels.WorkerVM;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class Workers : Page
    {
        public Workers()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
           var viewModel = new WorkerViewModel();
            DataContext = viewModel;
           
        }
    }
}
