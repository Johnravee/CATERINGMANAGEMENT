using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class PayslipWindow : Window
    {
        private readonly PayslipWindowViewModel _viewModel;

        public PayslipWindow()
        {
            InitializeComponent();
            _viewModel = new PayslipWindowViewModel();
            DataContext = _viewModel;
        }
    }
}
