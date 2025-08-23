
using CATERINGMANAGEMENT.ViewModels;
using System.Windows.Controls;


namespace CATERINGMANAGEMENT.View.Pages
{
    /// <summary>
    /// Interaction logic for Schedule.xaml
    /// </summary>
    public partial class Schedule : Page
    {
        public Schedule()
        {
            InitializeComponent();
            DataContext = new SchedulingViewModel();

            Loaded += async (_, __) =>
            {
                if (DataContext is SchedulingViewModel vm)
                {
                    await vm.LoadData();
                }
            };
        }
    }
}
