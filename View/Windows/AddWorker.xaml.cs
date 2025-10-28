using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.View.Pages;
using CATERINGMANAGEMENT.ViewModels.WorkerVM;
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
    /// Interaction logic for AddWorker.xaml
    /// </summary>
    public partial class AddWorker : Window
    {
        
       
        public AddWorker()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            var viewModel = new AddWorkerViewModel();
            DataContext = viewModel;
        }

       
    }
}
