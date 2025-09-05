using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class ProfileChatList : Page
    {
        private readonly ProfileViewModel _viewModel;
        private readonly Action<Profile> _onUserSelected;

        public ProfileChatList()
        {
            InitializeComponent();
            AuthGuard.RequireAuthentication(this);
            _viewModel = new ProfileViewModel();
            DataContext = _viewModel;
          

            Loaded += async (_, __) => await _viewModel.LoadProfiles();
        }

     
    }
}
