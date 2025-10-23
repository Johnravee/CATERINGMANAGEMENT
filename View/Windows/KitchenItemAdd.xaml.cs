using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.KitchenVM;
using System;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class KitchenItemAdd : Window
    {
        public KitchenItemAdd()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            var viewModel = new AddKitchenItemViewModel();
            DataContext = viewModel;
        }
    }
}
