using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.EquipmentsVM;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class EquipmentItemAdd : Window
    {

        public EquipmentItemAdd()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            var viewModel = new AddEquipmentViewModel();
            DataContext = viewModel;
        }

      
    }
}
