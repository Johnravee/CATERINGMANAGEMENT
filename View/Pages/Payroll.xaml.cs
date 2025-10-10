using CATERINGMANAGEMENT.ViewModels.PayrollVM;
using System.Windows.Controls;


namespace CATERINGMANAGEMENT.View.Pages
{
    /// <summary>
    /// Interaction logic for Payroll.xaml
    /// </summary>
    public partial class Payroll : Page
    {
        private readonly PayrollViewModel _viewModel;
        public Payroll()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            _viewModel = new PayrollViewModel();
            DataContext = _viewModel;

            Loaded += async (_, __) => { await _viewModel.LoadPage(); };
        }
    }
}
