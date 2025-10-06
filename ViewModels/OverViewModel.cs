using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class OverviewViewModel : INotifyPropertyChanged
    {
        // Monthly Reservations
        public ObservableCollection<MonthlyReservationSummary> MonthlyReservationSummaries { get; set; } = new();

        private ObservableCollection<MonthlyReservationSummary> _filteredSummaries = new();

        public ISeries[] ReservationSeries { get; set; } = Array.Empty<ISeries>();
        public Axis[] XAxes { get; set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; set; } = Array.Empty<Axis>();

        // Doughnut Chart (Event Type Distribution)
        public ISeries[] EventTypeSeries { get; set; } = Array.Empty<ISeries>();
        public string[] EventTypeLabels { get; set; } = Array.Empty<string>();

        // Year Selection
        private ObservableCollection<int> _availableYears = new();
        public ObservableCollection<int> AvailableYears
        {
            get => _availableYears;
            set { _availableYears = value; OnPropertyChanged(); }
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    OnPropertyChanged();
                    FilterByYear();
                }
            }
        }

        public OverviewViewModel()
        {
            _ = LoadMonthlyReservationsAsync();
            _ = LoadEventTypeDistributionAsync();
        }

        // ===============================
        // MONTHLY RESERVATION CHART
        // ===============================
        private async Task LoadMonthlyReservationsAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<MonthlyReservationSummary>()
                    .Get();

                MonthlyReservationSummaries.Clear();
                foreach (var item in response.Models)
                    MonthlyReservationSummaries.Add(item);

                var years = MonthlyReservationSummaries
                    .Select(x => x.ReservationYear)
                    .Distinct()
                    .OrderByDescending(y => y);

                AvailableYears = new ObservableCollection<int>(years);
                SelectedYear = AvailableYears.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverviewViewModel] Error loading reservations: {ex.Message}");
            }
        }

        private void FilterByYear()
        {
            _filteredSummaries = new ObservableCollection<MonthlyReservationSummary>(
                MonthlyReservationSummaries
                    .Where(x => x.ReservationYear == SelectedYear)
                    .OrderBy(x => x.ReservationMonth)
            );

            SetupChart();
        }

        private void SetupChart()
        {
            if (_filteredSummaries.Count == 0) return;

            var labels = _filteredSummaries
                .Select(x => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x.ReservationMonth))
                .ToArray();

            var values = _filteredSummaries
                .Select(x => (double)x.TotalReservations)
                .ToArray();

            ReservationSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = values,
                    Fill = new SolidColorPaint(SKColor.Parse("#5B6AC9")),
                    Name = $"Reservations ({SelectedYear})",
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
                }
            };

            XAxes = new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 45,
                    TextSize = 13,
                    Name = "Month"
                }
            };

            YAxes = new[]
            {
                new Axis
                {
                    Name = "Total Reservations",
                    TextSize = 13
                }
            };

            OnPropertyChanged(nameof(ReservationSeries));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
        }

        // ===============================
        // EVENT TYPE DISTRIBUTION (DOUGHNUT)
        // ===============================
        private async Task LoadEventTypeDistributionAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var response = await client
                    .From<Reservation>()
                    .Select("*, package:package_id(name)") // assuming event type is from Package.Name
                    .Get();

                var reservations = response.Models;
                if (reservations.Count == 0) return;

                var eventTypeGroups = reservations
                    .Where(r => r.Package != null)
                    .GroupBy(r => r.Package!.Name)
                    .Select(g => new { EventType = g.Key, Count = g.Count() })
                    .ToList();

                var totalCount = eventTypeGroups.Sum(x => x.Count);

                // Material-inspired color palette
                SKColor[] palette =
                {
            SKColor.Parse("#5B6AC9"),
            SKColor.Parse("#7C83FD"),
            SKColor.Parse("#96BAFF"),
            SKColor.Parse("#B4F8C8"),
            SKColor.Parse("#FFB4B4"),
            SKColor.Parse("#FFD580")
        };

                // Create pie series (no InnerRadius = full pie)
                EventTypeSeries = eventTypeGroups
                    .Select((item, index) => new PieSeries<double>
                    {
                        Name = item.EventType,
                        Values = new double[] { item.Count },
                        Fill = new SolidColorPaint(palette[index % palette.Length]),
                        Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 3 },
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        // Label displays event type and percentage
                        DataLabelsFormatter = point =>
                        {
                            double percent = (point.Coordinate.PrimaryValue / totalCount) * 100;
                            return $"{item.EventType}";
                        }
                    })
                    .ToArray();

                OnPropertyChanged(nameof(EventTypeSeries));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverviewViewModel] Error loading event type distribution: {ex.Message}");
            }
        }



        // ===============================
        // INotifyPropertyChanged
        // ===============================
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
