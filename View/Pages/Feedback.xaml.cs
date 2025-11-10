using CATERINGMANAGEMENT.ViewModels.FeedbackVM;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class Feedback : Page
    {
        private readonly FeedbackViewModel _viewModel;
        public Feedback()
        {
            InitializeComponent();
            _viewModel = new FeedbackViewModel();
            DataContext = _viewModel;
        }

        // Persist ShowOnWebsite toggle via ViewModel commands
        private void ToggleButton_ApprovalChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton tb) return;
            if (tb.DataContext is not CATERINGMANAGEMENT.Models.Feedback model) return;

            if (tb.IsChecked == true)
            {
                if (_viewModel.ShowOnWebsiteCommand.CanExecute(model))
                    _viewModel.ShowOnWebsiteCommand.Execute(model);
            }
            else
            {
                if (_viewModel.HideFromWebsiteCommand.CanExecute(model))
                    _viewModel.HideFromWebsiteCommand.Execute(model);
            }
        }
    }
}
