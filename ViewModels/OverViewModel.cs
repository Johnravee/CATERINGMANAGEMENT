
using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class OverviewViewModel : INotifyPropertyChanged
    {
        // ============ Data Collections =============
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

        private ObservableCollection<Reservation> _upcomingReservation = new();
        public ObservableCollection<Reservation> UpcomingReservation
        {
            get => _upcomingReservation;
            set { _upcomingReservation = value; OnPropertyChanged(); }
        }

        // ============ Chart Properties =============
        public ISeries[] ReservationSeries { get; private set; } = Array.Empty<ISeries>();
        public Axis[] XAxes { get; private set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; private set; } = Array.Empty<Axis>();

        public ISeries[] EventTypeSeries { get; private set; } = Array.Empty<ISeries>();

        // ============ Internal for Event Types =============
        private List<Reservation> _allReservations = new();
        private Dictionary<string, int> _eventTypeDistribution = new();

        // ============ Constructor =============
        public OverviewViewModel()
        {
            _ = LoadDashboardCountersAsync();
            _ = LoadMonthlyReservationsAsync();
            _ = LoadEventTypeDistributionAsync();
            _ = LoadUpcomingReservationsAsync();
        }

        // ============ Data Load Methods =============
        private async Task LoadDashboardCountersAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<DashboardCounters>().Get();
                var counters = response.Models.FirstOrDefault();
                if (counters != null)
                    DashboardCounters = counters;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading counters: {ex.Message}");
            }
        }

        private async Task LoadUpcomingReservationsAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Reservation>()
                    .Select("id, receipt_number, event_date, venue")
                    .Where(r => r.Status == "completed")
                    .Get();

                if (response.Models != null)
                    UpcomingReservation = new ObservableCollection<Reservation>(response.Models);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading upcoming reservations: {ex.Message}");
            }
        }

        private async Task LoadMonthlyReservationsAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<MonthlyReservationSummary>().Get();

                if (response?.Models == null)
                    return;

                MonthlyReservationSummaries.Clear();
                foreach (var item in response.Models)
                    MonthlyReservationSummaries.Add(item);

                var years = MonthlyReservationSummaries
                    .Select(r => r.ReservationYear)
                    .Distinct()
                    .OrderByDescending(y => y);

                AvailableYears = new ObservableCollection<int>(years);
                SelectedYear = AvailableYears.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading monthly reservations: {ex.Message}");
            }
        }

        private void FilterByYear()
        {
            if (MonthlyReservationSummaries.Count == 0)
                return;

            _filteredSummaries = new ObservableCollection<MonthlyReservationSummary>(
                MonthlyReservationSummaries
                    .Where(r => r.ReservationYear == SelectedYear)
                    .OrderBy(r => r.ReservationMonth)
            );
            SetupReservationChart();
        }

        private void SetupReservationChart()
        {
            if (_filteredSummaries == null || _filteredSummaries.Count == 0)
                return;

            var labels = _filteredSummaries
                .Select(r => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(r.ReservationMonth))
                .ToArray();

            var values = _filteredSummaries
                .Select(r => (double)r.TotalReservations)
                .ToArray();

            ReservationSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = values,
                    Fill = new SolidColorPaint(SKColor.Parse("#5B6AC9")),
                    Name = $"Reservations {SelectedYear}",
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
                }
            };

            XAxes = new Axis[]
            {
                new Axis { Labels = labels, LabelsRotation = 45, TextSize = 13, Name = "Month" }
            };

            YAxes = new Axis[]
            {
                new Axis { Name = "Reservations", TextSize = 13 }
            };

            OnPropertyChanged(nameof(ReservationSeries));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
        }

        private async Task LoadEventTypeDistributionAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Reservation>()
                    .Select("*, package:package_id(name)")
                    .Get();

                _allReservations = response.Models;

                if (_allReservations == null || _allReservations.Count == 0)
                    return;

                _eventTypeDistribution = _allReservations
                    .Where(r => r.Package != null)
                    .GroupBy(r => r.Package!.Name)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Build the EventTypeSeries for UI
                EventTypeSeries = _eventTypeDistribution.Select(kv =>
                    new PieSeries<double>
                    {
                        Name = kv.Key,
                        Values = new double[] { kv.Value },
                        Fill = new SolidColorPaint(SKColor.Parse("#5B6AC9")),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point => kv.Key
                    }
                ).ToArray();

                OnPropertyChanged(nameof(EventTypeSeries));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading event types: {ex.Message}");
            }
        }

        public Dictionary<string, int> GetEventTypeDistribution()
        {
            return _eventTypeDistribution ?? new Dictionary<string, int>();
        }

        // ============ PDF Export Command =============
        public ICommand GeneratePdfCommand => new RelayCommand(GenerateDashboardPdf);

        private void GenerateDashboardPdf()
        {
            try
            {
                DashboardPdfReport.Generate(
                    DashboardCounters,
                    _filteredSummaries,
                    UpcomingReservation,
                    GetEventTypeDistribution()
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate PDF: {ex.Message}");
            }
        }

        // ============ Chart UI Elements (set from View) =============
        public System.Windows.FrameworkElement ReservationChartElement { get; set; }
        public System.Windows.FrameworkElement EventTypeChartElement { get; set; }

        // ============ INotifyPropertyChanged =============
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
