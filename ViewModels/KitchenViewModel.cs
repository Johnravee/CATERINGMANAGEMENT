using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class KitchenViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Kitchen> _kitchenItems = new(); // master list
        private ObservableCollection<Kitchen> _filteredKitchenItems = new(); // filtered view

        public ObservableCollection<Kitchen> Items
        {
            get => _filteredKitchenItems;
            set { _filteredKitchenItems = value; OnPropertyChanged(); }
        }

        public int TotalCount { get; set; }
        public int LowStockCount { get; set; }
        public int NormalStockCount { get; set; }

        public ObservableCollection<ISeries> TotalItemsSeries { get; set; } = new();
        public ObservableCollection<ISeries> LowStockSeries { get; set; } = new();
        public ObservableCollection<ISeries> NormalStockSeries { get; set; } = new();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplySearchFilter();
            }
        }

        // Load from Supabase
        public async Task LoadItems()
        {
            IsLoading = true;
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Kitchen>()
                    .Get();

                System.Diagnostics.Debug.WriteLine($"[KitchenViewModel] Query returned {response.Models?.Count ?? 0} items");

                if (response.Models != null && response.Models.Count > 0)
                {
                    // ✅ Keep the master list
                    _kitchenItems = new ObservableCollection<Kitchen>(response.Models);

                    // Apply filtering + update counts
                    ApplySearchFilter();
                    UpdateCounts();
                }
                else
                {
                    _kitchenItems.Clear();
                    Items.Clear();
                    TotalCount = LowStockCount = NormalStockCount = 0;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading kitchen items:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Filter
        private void ApplySearchFilter()
        {
            var query = _searchText?.Trim().ToLower() ?? "";
            Items = string.IsNullOrWhiteSpace(query)
                ? new ObservableCollection<Kitchen>(_kitchenItems)
                : new ObservableCollection<Kitchen>(_kitchenItems.Where(i =>
                    (!string.IsNullOrEmpty(i.ItemName) && i.ItemName.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(i.Unit) && i.Unit.ToLower().Contains(query))
                ));
        }

        // Update stock counts (for charts)
        private void UpdateCounts()
        {
            TotalCount = _kitchenItems.Count;
            LowStockCount = _kitchenItems.Count(i => i.Quantity < 10); // Low stock threshold
            NormalStockCount = TotalCount - LowStockCount;

            // Avoid division by zero
            double total = TotalCount > 0 ? TotalCount : 1;

            // Calculate proportions
            double lowStockPercent = (double)LowStockCount / total;
            double normalStockPercent = (double)NormalStockCount / total;

            // Total Items Chart
            TotalItemsSeries.Clear();
            TotalItemsSeries.Add(new PieSeries<int>
            {
                Values = new int[] { TotalCount },
                Fill = new SolidColorPaint(SKColors.MediumPurple),
                InnerRadius = 15,

            });

            // Low Stock Chart (proportional)
            LowStockSeries.Clear();
            LowStockSeries.Add(new PieSeries<double>
            {
                Values = new double[] { lowStockPercent },
                Fill = new SolidColorPaint(SKColors.Red),
                InnerRadius = 15,
 
            });

            // Normal Stock Chart (proportional)
            NormalStockSeries.Clear();
            NormalStockSeries.Add(new PieSeries<double>
            {
                Values = new double[] { normalStockPercent },
                Fill = new SolidColorPaint(SKColors.Green),
                InnerRadius = 15,
            });

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(LowStockCount));
            OnPropertyChanged(nameof(NormalStockCount));
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
