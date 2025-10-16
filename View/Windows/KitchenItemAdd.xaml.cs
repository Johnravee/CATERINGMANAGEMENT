using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.KitchenVM;
using System;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class KitchenItemAdd : Window
    {
        public Kitchen? KitchenItem { get; set; }

        public KitchenItemAdd(KitchenViewModel parentVM)
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            var viewModel = new AddKitchenItemViewModel(parentVM);
            DataContext = viewModel;
        }
    }
}
