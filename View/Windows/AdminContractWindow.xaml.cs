using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CATERINGMANAGEMENT.ViewModels.ReservationVM;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class AdminContractWindow : Window
    {
        private readonly AdminContractViewModel _vm;
        private static readonly Regex _digitsOnly = new Regex("^[0-9]+$");

        public AdminContractWindow()
        {
            InitializeComponent();
            _vm = new AdminContractViewModel();
            DataContext = _vm;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Strict numeric input for contact field
        private void Contact_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!_digitsOnly.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void Contact_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string))!;
                if (!string.IsNullOrEmpty(text))
                {
                    var filtered = new string(text.Where(char.IsDigit).ToArray());
                    if (filtered.Length == 0)
                    {
                        e.CancelCommand();
                    }
                    else if (filtered != text)
                    {
                        Clipboard.SetText(filtered);
                    }
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
