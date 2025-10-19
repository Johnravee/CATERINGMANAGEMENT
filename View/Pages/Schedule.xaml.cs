using CATERINGMANAGEMENT.ViewModels.SchedulingVM;
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
            //AuthGuard.RequireAuthentication(this);
            DataContext = new SchedulingViewModel();

           
        }
    }
}
