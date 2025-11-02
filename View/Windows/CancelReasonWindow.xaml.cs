using System.Windows;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class CancelReasonWindow : Window
    {
        public string? Reason => ReasonBox.Text;

        public CancelReasonWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
