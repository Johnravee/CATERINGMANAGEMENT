using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.ViewModels.SchedulingVM;
using System.Windows;


namespace CATERINGMANAGEMENT.View.Windows
{
    /// <summary>
    /// Interaction logic for EditScheduleWindow.xaml
    /// </summary>
    public partial class EditScheduleWindow : Window
    {
        public EditScheduleWindow(GroupSchedule groupSchedule, SchedulingViewModel parentViewModel)
        {
            InitializeComponent();
            var viewModel = new EditScheduleViewModel(groupSchedule, parentViewModel);
            DataContext = viewModel;
        }
    }
}
