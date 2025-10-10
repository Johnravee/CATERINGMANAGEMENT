using CATERINGMANAGEMENT.ViewModels.WorkerVM;
using System.Windows;


namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for AssignWorker.xaml
    /// </summary>
    public partial class AssignWorker : Window
    {
        private readonly AssignWorkersViewModel _vm;
        public AssignWorker()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            _vm = new AssignWorkersViewModel();
            DataContext = _vm;
        }

      
    }
}
