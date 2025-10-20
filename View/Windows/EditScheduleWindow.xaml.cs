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
        public EditScheduleWindow(GroupedScheduleView groupedSchedule, SchedulingViewModel parentViewModel)
        {
            InitializeComponent();
            DataContext = new EditScheduleViewModel(groupedSchedule, parentViewModel);
        }
    }
}
