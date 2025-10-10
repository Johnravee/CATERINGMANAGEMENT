using CATERINGMANAGEMENT.ViewModels.FeedbackVM;
using System.Windows.Controls;


namespace CATERINGMANAGEMENT.View.Pages
{
    /// <summary>
    /// Interaction logic for Feedback.xaml
    /// </summary>
    public partial class Feedback : Page
    {
        private readonly FeedbackViewModel _viewModel;
        public Feedback()
        {
            InitializeComponent();
            _viewModel = new FeedbackViewModel();
            DataContext = _viewModel;
        }
    }
}
