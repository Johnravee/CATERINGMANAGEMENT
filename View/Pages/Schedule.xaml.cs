using CATERINGMANAGEMENT.ViewModels.SchedulingVM;
using CATERINGMANAGEMENT.Helpers;
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
            if (!AuthGuard.RequireAuthentication(this))
                return;

            DataContext = new SchedulingViewModel();

           
        }
    }
}
