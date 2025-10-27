/*
 * FILE: OverviewViewModel.cs
 * PURPOSE: Acts as the main ViewModel for the Overview/Dashboard page.
 *          Responsibilities:
 *          - Load and expose dashboard counters, monthly reservations, upcoming reservations, and event type distribution.
 *          - Provide filtered monthly data for charts.
 *          - Generate PDF report of dashboard data.
 *          - Expose chart data and series for LiveCharts.
 *          - Manage loading state for UI overlays.
 */

using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.DashboardVM
{
    public class OverviewViewModel : BaseViewModel
    {
        #region Services
        private readonly OverviewService _service = new();
        #endregion

        #region Loading State
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }
        #endregion

        #region Chart Controls
        public FrameworkElement ReservationChartElement { get; set; }
        public FrameworkElement EventTypeChartElement { get; set; }
        #endregion

        #region Data Collections
        public ObservableCollection<MonthlyReservationSummary> MonthlyReservationSummaries { get; set; } = new();
        private ObservableCollection<MonthlyReservationSummary> _filteredSummaries = new();

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

        private DashboardCounters _dashboardCounters = new();
        public DashboardCounters DashboardCounters
        {
            get => _dashboardCounters;
            set { _dashboardCounters = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Reservation> _upcomingReservations = new();
        public ObservableCollection<Reservation> UpcomingReservations
        {
            get => _upcomingReservations;
            set { _upcomingReservations = value; OnPropertyChanged(); }
        }
        #endregion

        #region Charts
        public ISeries[] ReservationSeries { get; private set; } = Array.Empty<ISeries>();
        public Axis[] XAxes { get; private set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; private set; } = Array.Empty<Axis>();

        public ISeries[] EventTypeSeries { get; private set; } = Array.Empty<ISeries>();

        private List<Reservation> _allReservations = new();
        private Dictionary<string, int> _eventTypeDistribution = new();

        // Distinct colors for pie slices and bars
        private static readonly SKColor[] PieSliceColors = new[]
        {
            SKColor.Parse("#5B6AC9"), // indigo
            SKColor.Parse("#FF6B6B"), // coral red
            SKColor.Parse("#4ECDC4"), // teal
            SKColor.Parse("#C7F464"), // lime
            SKColor.Parse("#FFA07A"), // light salmon
            SKColor.Parse("#FFD93D"), // yellow
            SKColor.Parse("#6A0572"), // purple
            SKColor.Parse("#2E8B57"), // sea green
            SKColor.Parse("#FF8C00"), // dark orange
            SKColor.Parse("#20B2AA")  // light sea green
        };
        #endregion

        #region Constructor
        public OverviewViewModel()
        {
            _ = LoadAllDataAsync();
        }
        #endregion

        #region Data Loading
        private async Task LoadAllDataAsync()
        {
            try
            {
                IsLoading = true;

                await LoadDashboardCountersAsync();
                await LoadMonthlyReservationsAsync();
                await LoadUpcomingReservationsAsync();
                await LoadEventTypeDistributionAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadDashboardCountersAsync()
        {
            try
            {
                var counters = await _service.GetDashboardCountersAsync();
                if (counters != null)
                {
                    DashboardCounters = counters;
                    AppLogger.Info("Dashboard counters loaded.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load dashboard counters.");
            }
        }

        private async Task LoadMonthlyReservationsAsync()
        {
            try
            {
                var summaries = await _service.GetMonthlyReservationSummariesAsync();
                MonthlyReservationSummaries = new ObservableCollection<MonthlyReservationSummary>(summaries);

                var years = MonthlyReservationSummaries.Select(r => r.ReservationYear).Distinct().OrderByDescending(y => y);
                AvailableYears = new ObservableCollection<int>(years);
                SelectedYear = AvailableYears.FirstOrDefault();

                AppLogger.Info("Monthly reservations loaded.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load monthly reservations.");
            }
        }

        private async Task LoadUpcomingReservationsAsync()
        {
            try
            {
                var upcoming = await _service.GetUpcomingReservationsAsync();
                UpcomingReservations = new ObservableCollection<Reservation>(upcoming);
                AppLogger.Info("Upcoming reservations loaded.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load upcoming reservations.");
            }
        }

        private async Task LoadEventTypeDistributionAsync()
        {
            try
            {
                _allReservations = await _service.GetAllReservationsWithPackageAsync();
                _eventTypeDistribution = _service.GetEventTypeDistribution(_allReservations);

                var ordered = _eventTypeDistribution
                    .OrderByDescending(kv => kv.Value)
                    .ToList();

                EventTypeSeries = ordered
                    .Select((kv, index) => new PieSeries<double>
                    {
                        Name = kv.Key,
                        Values = new double[] { kv.Value },
                        Fill = new SolidColorPaint(PieSliceColors[index % PieSliceColors.Length]),
                        Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point => kv.Key
                    })
                    .ToArray();

                OnPropertyChanged(nameof(EventTypeSeries));
                AppLogger.Info("Event type distribution loaded.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load event type distribution.");
            }
        }
        #endregion

        #region Filter & Chart Setup
        private void FilterByYear()
        {
            if (MonthlyReservationSummaries.Count == 0) return;

            _filteredSummaries = new ObservableCollection<MonthlyReservationSummary>(
                MonthlyReservationSummaries.Where(r => r.ReservationYear == SelectedYear).OrderBy(r => r.ReservationMonth)
            );

            SetupReservationChart();
        }

        private void SetupReservationChart()
        {
            if (_filteredSummaries.Count == 0) return;

            var labels = _filteredSummaries.Select(r => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(r.ReservationMonth)).ToArray();
            var values = _filteredSummaries.Select(r => (double)r.TotalReservations).ToArray();

            // Create one series per bar to allow distinct colors per bar.
            var coloredSeries = new List<ISeries>();
            for (int i = 0; i < values.Length; i++)
            {
                var seriesValues = new double?[values.Length];
                seriesValues[i] = values[i];

                coloredSeries.Add(new ColumnSeries<double?>
                {
                    Values = seriesValues,
                    Fill = new SolidColorPaint(PieSliceColors[i % PieSliceColors.Length]),
                    Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 1 },
                    Name = string.Empty,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
                });
            }

            ReservationSeries = coloredSeries.ToArray();

            XAxes = new Axis[] { new Axis { Labels = labels, LabelsRotation = 45, TextSize = 13, Name = "Month" } };
            YAxes = new Axis[] { new Axis { Name = "Reservations", TextSize = 13 } };

            OnPropertyChanged(nameof(ReservationSeries));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
        }
        #endregion

        #region PDF Export
        public ICommand GeneratePdfCommand => new RelayCommand(GenerateDashboardPdf);

        private void GenerateDashboardPdf()
        {
            try
            {
                DashboardPdfReport.Generate(DashboardCounters, _filteredSummaries, UpcomingReservations, _eventTypeDistribution);
                AppLogger.Success("Dashboard PDF generated successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to generate dashboard PDF.");
            }
        }
        #endregion
    }
}
