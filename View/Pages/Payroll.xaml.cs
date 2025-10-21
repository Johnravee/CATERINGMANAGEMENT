using CATERINGMANAGEMENT.ViewModels.PayrollVM;
using System.Windows.Controls;


namespace CATERINGMANAGEMENT.View.Pages
{
    /// <summary>
    /// Interaction logic for Payroll.xaml
    /// </summary>
    public partial class Payroll : Page
    {
     
        public Payroll()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            var viewModel = new PayrollViewModel();
            DataContext = viewModel;

         
        }
    }
}
