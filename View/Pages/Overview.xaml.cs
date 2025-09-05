using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using System.Collections.Generic;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class Overview : Page
    {
        public IEnumerable<ISeries> ReservationSeries { get; set; }
        public IEnumerable<Axis> XAxes { get; set; }
        public IEnumerable<Axis> YAxes { get; set; }

        public Overview()
        {
            InitializeComponent();
            AuthGuard.RequireAuthentication(this);
            // Sample data for bar chart
            ReservationSeries = new List<ISeries>
            {
                new ColumnSeries<int>
                {
                    Name = "Reservations",
                    Values = new[] { 10, 15, 8, 20, 12, 25, 18, 22, 17, 19, 14, 21 }
                }
            };

            XAxes = new List<Axis>
            {
                new Axis
                {
                    Labels = new[]
                    {
                        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
                    }
                }
            };

            YAxes = new List<Axis>
            {
                new Axis { Name = "Reservations Count" }
            };

            DataContext = this;
        }
    }
}
