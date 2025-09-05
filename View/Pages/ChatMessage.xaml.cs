using CATERINGMANAGEMENT.Helpers;
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
            AuthGuard.RequireAuthentication(this);
            ViewModel = new ProfileViewModel();
            DataContext = ViewModel;

            _ = ViewModel.LoadProfiles(); 
        }
    }
}
