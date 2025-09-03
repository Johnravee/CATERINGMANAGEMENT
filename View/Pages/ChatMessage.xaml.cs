using CATERINGMANAGEMENT.ViewModels;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class ChatMessage : Page
    {
        public ProfileViewModel ViewModel { get; set; }

        public ChatMessage()
        {
            InitializeComponent();
            ViewModel = new ProfileViewModel();
            DataContext = ViewModel;

            _ = ViewModel.LoadProfiles(); // Load profiles on page load
        }
    }
}
