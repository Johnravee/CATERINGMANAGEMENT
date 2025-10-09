using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
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
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class OverviewViewModel : INotifyPropertyChanged
    {
        // ===============================
        // PROPERTIES
        // ===============================

        public ObservableCollection<MonthlyReservationSummary> MonthlyReservationSummaries { get; set; } = new();
        private ObservableCollection<MonthlyReservationSummary> _filteredSummaries = new();

        public ISeries[] ReservationSeries { get; private set; } = Array.Empty<ISeries>();
        public Axis[] XAxes { get; private set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; private set; } = Array.Empty<Axis>();

        public ISeries[] EventTypeSeries { get; private set; } = Array.Empty<ISeries>();

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

        // ============= COUNTERS ================
        private DashboardCounters _dashboardCounters = new DashboardCounters();
        public DashboardCounters DashboardCounters
        {
            get => _dashboardCounters;
            set
            {
                _dashboardCounters = value;
                OnPropertyChanged();
            }
        }

        // ============= UPCOMING RESERVATION ================
        private ObservableCollection<Reservation> _upcomingreservation = new();
        public ObservableCollection<Reservation> UpcomingReservation
        {
            get => _upcomingreservation;
            set
            {
                _upcomingreservation = value;
                OnPropertyChanged();
            }
        }


  

        // ===============================
        // CONSTRUCTOR
        // ===============================
        public OverviewViewModel()
        {
 
            _ = LoadDashboardCountersAsync();
            _ = LoadMonthlyReservationsAsync();
            _ = LoadEventTypeDistributionAsync();
            _ = LoadUpcomingReservationsAsync();
        }

      



        // ===============================
        // LOAD DASHBOARD COUNTERS
        // ===============================
        private async Task LoadDashboardCountersAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<DashboardCounters>()
                    .Get();

                var counters = response.Models.FirstOrDefault();

                if (counters != null)
                {
                    DashboardCounters = counters;
                    OnPropertyChanged(nameof(DashboardCounters));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading DashboardCounter reservations: {ex.Message}");
            }
        }

        private async Task LoadUpcomingReservationsAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Reservation>()
                    .Select(@"id, receipt_number, event_date, venue")
                    .Where( x => x.Status == "completed")
                    .Get();

                if (response.Models is not null)
                {
                    UpcomingReservation = new ObservableCollection<Reservation>(response.Models);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading completed reservations: {ex.Message}");
            }
        }



        // ===============================
        // MONTHLY RESERVATION CHART
        // ===============================
        private async Task LoadMonthlyReservationsAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<MonthlyReservationSummary>().Get();

                if (response?.Models == null)
                {
                    Console.WriteLine("[OverviewViewModel] LoadMonthlyReservationsAsync: no data returned.");
                    return;
                }

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
                Console.WriteLine("[OverviewViewModel] Error in LoadMonthlyReservationsAsync (MonthlyReservationSummary): " + ex.Message);
            }
        }

        private void FilterByYear()
        {
            if (MonthlyReservationSummaries.Count == 0)
                return;

            _filteredSummaries = new ObservableCollection<MonthlyReservationSummary>(
                MonthlyReservationSummaries
                    .Where(x => x.ReservationYear == SelectedYear)
                    .OrderBy(x => x.ReservationMonth)
            );

            SetupReservationChart();
        }

        private void SetupReservationChart()
        {
            if (_filteredSummaries.Count == 0)
                return;

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
        // LOAD EVENT TYPE DISTRIBUTION
        // ===============================
        private async Task LoadEventTypeDistributionAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Reservation>()
                    .Select("*, package:package_id(name)")
                    .Get();

                var reservations = response.Models;

                if (reservations == null || reservations.Count == 0)
                {
                    Console.WriteLine("[OverviewViewModel] LoadEventTypeDistributionAsync: no reservations data.");
                    return;
                }

                var eventTypeGroups = reservations
                    .Where(r => r.Package != null)
                    .GroupBy(r => r.Package!.Name)
                    .Select(g => new { EventType = g.Key, Count = g.Count() })
                    .ToList();

                var totalCount = eventTypeGroups.Sum(x => x.Count);
                if (totalCount == 0) return;

                SKColor[] palette =
                {
                    SKColor.Parse("#5B6AC9"),
                    SKColor.Parse("#7C83FD"),
                    SKColor.Parse("#96BAFF"),
                    SKColor.Parse("#B4F8C8"),
                    SKColor.Parse("#FFB4B4"),
                    SKColor.Parse("#FFD580")
                };

                EventTypeSeries = eventTypeGroups
                    .Select((item, index) => new PieSeries<double>
                    {
                        Name = item.EventType,
                        Values = new double[] { item.Count },
                        Fill = new SolidColorPaint(palette[index % palette.Length]),
                        Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 3 },
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point => item.EventType
                    })
                    .ToArray();

                OnPropertyChanged(nameof(EventTypeSeries));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[OverviewViewModel] Error in LoadEventTypeDistributionAsync (Reservation view): " + ex.Message);
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
