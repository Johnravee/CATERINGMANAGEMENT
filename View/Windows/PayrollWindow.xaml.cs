using CATERINGMANAGEMENT.ViewModels.PayrollVM;
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
using System.Windows.Shapes;

namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for PayrollWindow.xaml
    /// </summary>
    public partial class PayrollWindow : Window
    {
        private readonly PayrollWindowViewModel _viewmodel = new();

        public PayrollWindow()
        {
            InitializeComponent();
            DataContext = _viewmodel;
        }
    }
}
