using CATERINGMANAGEMENT.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
