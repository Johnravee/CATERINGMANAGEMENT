using System.Threading.Tasks;
using System.Windows;
using CATERINGMANAGEMENT.ViewModels;

namespace CATERINGMANAGEMENT.View.Windows
{
    public partial class SplashScreenWindow : Window
    {
        private readonly SplashScreenViewModel _vm = new();
        public SplashScreenWindow()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        public async Task RunAsync(Func<Task> initialize)
        {
            await _vm.RunAsync(initialize);
        }
    }
}
