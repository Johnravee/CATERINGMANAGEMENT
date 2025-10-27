using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.SchedulingVM;
using CATERINGMANAGEMENT.Helpers;
using System.Windows;


namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for AssignWorker.xaml
    /// </summary>
    public partial class AssignWorker : Window
    {
        public AssignWorker(SchedulingViewModel parentVM)
        {
            InitializeComponent();
            if (!AuthGuard.RequireAuthentication(this))
                return;
            var viewModel = new AssignWorkersViewModel(parentVM);
            DataContext = viewModel;
        }

      
    }
}
