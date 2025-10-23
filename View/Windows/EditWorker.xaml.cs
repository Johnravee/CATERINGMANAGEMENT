using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.WorkerVM;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for EditWorker.xaml
    /// </summary>
    public partial class EditWorker : Window
    {
      

        public EditWorker(Worker existingWorker)
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            var viewModel = new EditWorkerViewModel(existingWorker);
            DataContext = viewModel;

        }

    }
}
