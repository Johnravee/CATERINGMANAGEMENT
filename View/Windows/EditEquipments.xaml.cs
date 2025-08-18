using CATERINGMANAGEMENT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for EditEquipments.xaml
    /// </summary>
    public partial class EditEquipments : Window
    {
        public Equipments Equipments { get; set; }
        public EditEquipments(Equipments equipments)
        {
            InitializeComponent();
            Equipments = equipments ?? throw new ArgumentNullException(nameof(equipments));
            DataContext = Equipments;
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Close dialog with "OK" result
            this.DialogResult = true;
            this.Close();
        }

        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only digits
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

    }
}
