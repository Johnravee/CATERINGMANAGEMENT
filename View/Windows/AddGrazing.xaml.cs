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
using System.Windows.Shapes;

namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for AddGrazing.xaml
    /// </summary>
    public partial class AddGrazing : Window
    {
        private readonly AddGrazingViewModel _viewModel;
        public AddGrazing()
        {
            InitializeComponent();
            _viewModel = new AddGrazingViewModel();
            DataContext = _viewModel;
        }
    }
}
