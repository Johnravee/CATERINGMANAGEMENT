using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using LiveChartsCore;
using LiveChartsCore.Painting; // Needed for SolidColorPaint
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;               // Needed for SKColors
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class EquipmentsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Equipments> _equipments = new();

        public int TotalCount { get; set; }
        public int GoodConditionCount { get; set; }
        public int NeedsRepairCount { get; set; }

        // Pie chart series
        public ObservableCollection<ISeries> TotalItemsSeries { get; set; } = new();
        public ObservableCollection<ISeries> GoodConditionSeries { get; set; } = new();
        public ObservableCollection<ISeries> NeedsRepairSeries { get; set; } = new();

        public ObservableCollection<Equipments> Equipments
        {
            get => _equipments;
            set { _equipments = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public async Task LoadEquipments()
        {
            try
            {
                IsLoading = true;

                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Equipments>()
                    .Order(e => e.Id, Ordering.Ascending)
                    .Get();

                if (response.Models != null && response.Models.Count > 0)
                {
                    Equipments = new ObservableCollection<Equipments>(response.Models);

                    // Calculate counts
                    TotalCount = Equipments.Count;
                    GoodConditionCount = Equipments.Count(e => e.Condition == "Good");
                    NeedsRepairCount = Equipments.Count(e => e.Condition == "Needs Repair");

                    // Update PieSeries with SolidColorPaint
                    TotalItemsSeries.Clear();
                    TotalItemsSeries.Add(new PieSeries<int>
                    {
                        Values = new int[] { TotalCount },
                        Fill = new SolidColorPaint(SKColors.MediumPurple),
                        InnerRadius = 15 
                    });

                    GoodConditionSeries.Clear();
                    GoodConditionSeries.Add(new PieSeries<int>
                    {
                        Values = new int[] { GoodConditionCount },
                        Fill = new SolidColorPaint(SKColors.Green),
                        InnerRadius = 15
                    });

                    NeedsRepairSeries.Clear();
                    NeedsRepairSeries.Add(new PieSeries<int>
                    {
                        Values = new int[] { NeedsRepairCount },
                        Fill = new SolidColorPaint(SKColors.Red),
                        InnerRadius = 15
                    });

                    // Notify UI
                    OnPropertyChanged(nameof(TotalCount));
                    OnPropertyChanged(nameof(GoodConditionCount));
                    OnPropertyChanged(nameof(NeedsRepairCount));
                }
                else
                {
                    Equipments.Clear();
                    TotalCount = GoodConditionCount = NeedsRepairCount = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading equipments: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
