using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Ensure PropertyChangedEventArgs always receives a non-null property name
            var name = propertyName ?? string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected void ShowMessage(string message, string title = "Notification", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBox.Show(message, title, buttons, icon);
        }
    }
}
