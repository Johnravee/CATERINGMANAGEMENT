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
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class EquipmentViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Equipment> _equipmentItems = new(); // master list
        private ObservableCollection<Equipment> _filteredEquipmentItems = new(); // filtered view

        private const int PageSize = 20;
        private int _currentOffset = 0;

        public ObservableCollection<Equipment> Items
        {
            get => _filteredEquipmentItems;
            set { _filteredEquipmentItems = value; OnPropertyChanged(); }
        }

        public int TotalCount { get; set; }
        public int DamagedCount { get; set; }
        public int GoodConditionCount { get; set; }

        public ObservableCollection<ISeries> TotalItemsSeries { get; set; } = new();
        public ObservableCollection<ISeries> DamagedSeries { get; set; } = new();
        public ObservableCollection<ISeries> GoodConditionSeries { get; set; } = new();

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

        // Pagination properties
        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand DeleteEquipmentCommand { get; set; }
        public ICommand EditEquipmentCommand { get; set; }
        public ICommand AddEquipmentCommand { get; set; }
        public ICommand LoadMoreCommand { get; set; }
        public ICommand NextPageCommand { get; set; }
        public ICommand PrevPageCommand { get; set; }

        public EquipmentViewModel()
        {
            DeleteEquipmentCommand = new RelayCommand<Equipment>(async (e) => await DeleteEquipment(e));
            EditEquipmentCommand = new RelayCommand<Equipment>(async (e) => await EditEquipment(e));
            AddEquipmentCommand = new RelayCommand(() => AddNewEquipment());
            LoadMoreCommand = new RelayCommand(async () => await LoadMoreItems());

            NextPageCommand = new RelayCommand(async () => await NextPage(), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await PrevPage(), () => CurrentPage > 1);
        }

        // Load first page
        public async Task LoadItems()
        {
            IsLoading = true;
            _currentOffset = 0;
            try
            {
                _equipmentItems.Clear();
                Items.Clear();

                await LoadPage(1);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading equipment items:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Load specific page
        public async Task LoadPage(int pageNumber)
        {
            IsLoading = true;
            try
            {
                var client = await SupabaseService.GetClientAsync();

                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client
                    .From<Equipment>()
                    .Range(from, to)
                    .Order(x => x.UpdatedAt, Ordering.Descending)
                    .Get();

                _equipmentItems.Clear();
                if (response.Models != null)
                {
                    foreach (var item in response.Models)
                        _equipmentItems.Add(item);
                }

                TotalCount = TotalCount = await client
                    .From<Equipment>()
                    .Select("id")
                    .Count(CountType.Exact);

                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);


                ApplySearchFilter();
                UpdateCounts();


                CurrentPage = pageNumber;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading page:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task NextPage()
        {
            if (CurrentPage < TotalPages)
                await LoadPage(CurrentPage + 1);
        }

        private async Task PrevPage()
        {
            if (CurrentPage > 1)
                await LoadPage(CurrentPage - 1);
        }

        // Load next batch (for infinite scroll)
        private async Task LoadMoreItems()
        {
            await LoadPage(CurrentPage + 1);
        }

        // Filter
        private void ApplySearchFilter()
        {
            var query = _searchText?.Trim().ToLower() ?? "";
            Items = string.IsNullOrWhiteSpace(query)
                ? new ObservableCollection<Equipment>(_equipmentItems)
                : new ObservableCollection<Equipment>(_equipmentItems.Where(i =>
                    (!string.IsNullOrEmpty(i.ItemName) && i.ItemName.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(i.Condition) && i.Condition.ToLower().Contains(query))
                ));
        }

        // Update counts and charts
        private void UpdateCounts()
        {
            TotalCount = _equipmentItems.Count;
            DamagedCount = _equipmentItems.Count(i => i.Condition == "Damaged");
            GoodConditionCount = TotalCount - DamagedCount;

            double total = TotalCount > 0 ? TotalCount : 1;
            double damagedPercent = (double)DamagedCount / total;
            double goodPercent = (double)GoodConditionCount / total;

            TotalItemsSeries.Clear();
            TotalItemsSeries.Add(new PieSeries<int> { Values = new int[] { TotalCount }, Fill = new SolidColorPaint(SKColors.MediumPurple), InnerRadius = 15 });

            DamagedSeries.Clear();
            DamagedSeries.Add(new PieSeries<double> { Values = new double[] { damagedPercent }, Fill = new SolidColorPaint(SKColors.Red), InnerRadius = 15 });

            GoodConditionSeries.Clear();
            GoodConditionSeries.Add(new PieSeries<double> { Values = new double[] { goodPercent }, Fill = new SolidColorPaint(SKColors.Green), InnerRadius = 15 });

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(DamagedCount));
            OnPropertyChanged(nameof(GoodConditionCount));
        }

    

        // Delete Equipment Item
        private async Task DeleteEquipment(Equipment item)
        {
            if (item == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete {item.ItemName}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.From<Equipment>().Where(e => e.Id == item.Id).Delete();

                _equipmentItems.Remove(item);
                ApplySearchFilter();
                UpdateCounts();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error deleting equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Edit Equipment Item
        private async Task EditEquipment(Equipment item)
        {
            if (item == null) return;

            var editWindow = new EditEquipments(item);
            bool? result = editWindow.ShowDialog();

            if (result == true && editWindow.Equipments != null)
            {
                try
                {
                    editWindow.Equipments.UpdatedAt = DateTime.UtcNow;

                    var client = await SupabaseService.GetClientAsync();
                    var response = await client.From<Equipment>()
                        .Where(e => e.Id == editWindow.Equipments.Id)
                        .Update(editWindow.Equipments);

                    if (response.Models != null && response.Models.Count > 0)
                    {
                        var index = _equipmentItems.IndexOf(item);
                        if (index >= 0)
                            _equipmentItems[index] = response.Models[0];

                        ApplySearchFilter();
                        UpdateCounts();
                        MessageBox.Show("Equipment item updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error updating equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Add new equipment item
        private void AddNewEquipment()
        {
            var addWindow = new EquipmentItemAdd();
            bool? result = addWindow.ShowDialog();

            if (result == true && addWindow.NewEquipment != null)
            {
                InsertEquipmentItem(addWindow.NewEquipment);
            }
        }

        private async void InsertEquipmentItem(Equipment item)
        {
            if (item == null) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<Equipment>().Insert(item);

                if (response.Models != null && response.Models.Count > 0)
                {
                    _equipmentItems.Add(response.Models[0]);
                    ApplySearchFilter();
                    UpdateCounts();
                    MessageBox.Show("Equipment item added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error adding equipment item:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
