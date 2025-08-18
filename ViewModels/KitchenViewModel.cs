using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

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

        // Commands
        public ICommand DeleteKitchenItemCommand { get; set; }
        public ICommand EditKitchenItemCommand { get; set; }
        public ICommand AddKitchenItemCommand { get; set; }

        public KitchenViewModel()
        {
            DeleteKitchenItemCommand = new RelayCommand<Kitchen>(async (k) => await DeleteKitchenItem(k));
            EditKitchenItemCommand = new RelayCommand<Kitchen>(async (k) => await EditKitchenItem(k));
            AddKitchenItemCommand = new RelayCommand(() => AddNewKitchenItem());
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

                if (response.Models != null && response.Models.Count > 0)
                {
                    _kitchenItems = new ObservableCollection<Kitchen>(response.Models);
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

        // Update counts and charts
        private void UpdateCounts()
        {
            TotalCount = _kitchenItems.Count;
            LowStockCount = _kitchenItems.Count(i => i.Quantity < 10);
            NormalStockCount = TotalCount - LowStockCount;

            double total = TotalCount > 0 ? TotalCount : 1;
            double lowStockPercent = (double)LowStockCount / total;
            double normalStockPercent = (double)NormalStockCount / total;

            TotalItemsSeries.Clear();
            TotalItemsSeries.Add(new PieSeries<int> { Values = new int[] { TotalCount }, Fill = new SolidColorPaint(SKColors.MediumPurple), InnerRadius = 15 });

            LowStockSeries.Clear();
            LowStockSeries.Add(new PieSeries<double> { Values = new double[] { lowStockPercent }, Fill = new SolidColorPaint(SKColors.Red), InnerRadius = 15 });

            NormalStockSeries.Clear();
            NormalStockSeries.Add(new PieSeries<double> { Values = new double[] { normalStockPercent }, Fill = new SolidColorPaint(SKColors.Green), InnerRadius = 15 });

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(LowStockCount));
            OnPropertyChanged(nameof(NormalStockCount));
        }

        // Delete Kitchen Item
        private async Task DeleteKitchenItem(Kitchen item)
        {
            if (item == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete {item.ItemName}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.From<Kitchen>().Where(k => k.Id == item.Id).Delete();

                _kitchenItems.Remove(item);
                ApplySearchFilter();
                UpdateCounts();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error deleting kitchen item:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Edit Kitchen Item
        private async Task EditKitchenItem(Kitchen item)
        {
            if (item == null) return;

            var editWindow = new EditKitchenItem(item); 
            bool? result = editWindow.ShowDialog();

            if (result == true && editWindow.KitchenItem != null)
            {
                try
                {
                    editWindow.KitchenItem.UpdatedAt = DateTime.UtcNow;

                    var client = await SupabaseService.GetClientAsync();
                    var response = await client.From<Kitchen>()
                        .Where(k => k.Id == editWindow.KitchenItem.Id)
                        .Update(editWindow.KitchenItem);

                    if (response.Models != null && response.Models.Count > 0)
                    {
                        var index = _kitchenItems.IndexOf(item);
                        if (index >= 0)
                            _kitchenItems[index] = response.Models[0];

                        ApplySearchFilter();
                        UpdateCounts();
                        MessageBox.Show("Kitchen item updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error updating kitchen item:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Add new kitchen item
        private void AddNewKitchenItem()
        {
            var addWindow = new KitchenItemAdd(); 
            bool? result = addWindow.ShowDialog();

            if (result == true && addWindow.KitchenItem != null)
            {
                InsertKitchenItem(addWindow.KitchenItem);
            }
        }

        private async void InsertKitchenItem(Kitchen item)
        {
            if (item == null) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<Kitchen>().Insert(item);

                if (response.Models != null && response.Models.Count > 0)
                {
                    _kitchenItems.Add(response.Models[0]);
                    ApplySearchFilter();
                    UpdateCounts();
                    MessageBox.Show("Kitchen item added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error adding kitchen item:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
