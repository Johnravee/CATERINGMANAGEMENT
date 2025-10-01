using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class EquipmentViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Equipment> _equipmentItems = new();
        private ObservableCollection<Equipment> _filteredEquipmentItems = new();

        private const int PageSize = 20;

        public ObservableCollection<Equipment> Items
        {
            get => _filteredEquipmentItems;
            set { _filteredEquipmentItems = value; OnPropertyChanged(); }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); }
        }

        private int _damagedCount;
        public int DamagedCount
        {
            get => _damagedCount;
            set { _damagedCount = value; OnPropertyChanged(); }
        }

        private int _goodConditionCount;
        public int GoodConditionCount
        {
            get => _goodConditionCount;
            set { _goodConditionCount = value; OnPropertyChanged(); }
        }
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
           

            NextPageCommand = new RelayCommand(async () => await NextPage(), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await PrevPage(), () => CurrentPage > 1);
        }

        public async Task LoadItems()
        {
            IsLoading = true;
            try
            {
                _equipmentItems.Clear();
                Items.Clear();
                await LoadPage(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading equipment items:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

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

               

                ApplySearchFilter();
                await LoadEquipmentSummary();
                UpdatePagination();

                CurrentPage = pageNumber;
            }
            catch (Exception ex)
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


        // Search query
        private async void ApplySearchFilter()
        {
            var query = _searchText?.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(query))
            {
                Items = new ObservableCollection<Equipment>(_equipmentItems);
            }
            else
            {
                try
                {
                    IsLoading = true;
                    var client = await SupabaseService.GetClientAsync();

                    var response = await client
                        .From<Equipment>()
                        .Filter(x => x.ItemName, Operator.ILike, $"%{query}%")
                        .Get();

                    if (response.Models != null)
                        Items = new ObservableCollection<Equipment>(response.Models);
                    else
                        Items = new ObservableCollection<Equipment>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error searching equipment:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public async Task LoadEquipmentSummary()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var response = await client
                    .From<EquipmentSummary>()
                    .Get();

                if (response.Models != null && response.Models.Count > 0)
                {
                    var summary = response.Models[0];
                    TotalCount = summary.TotalCount;
                    DamagedCount = summary.DamagedCount;
                    GoodConditionCount = summary.GoodCount;

                    OnPropertyChanged(nameof(TotalCount));
                    OnPropertyChanged(nameof(DamagedCount));
                    OnPropertyChanged(nameof(GoodConditionCount));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading equipment summary:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePagination()
        {

            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(TotalPages));
        }


        private async Task DeleteEquipment(Equipment item)
        {
            if (item == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete {item.ItemName}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client
                    .From<Equipment>()
                    .Where(e => e.Id == item.Id)
                    .Delete();

                _equipmentItems.Remove(item);
                ApplySearchFilter();
                await LoadEquipmentSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                    var response = await client
                        .From<Equipment>()
                        .Where(e => e.Id == editWindow.Equipments.Id)
                        .Update(editWindow.Equipments);

                    if (response.Models != null && response.Models.Count > 0)
                    {
                        var index = _equipmentItems.IndexOf(item);
                        if (index >= 0)
                            _equipmentItems[index] = response.Models[0];

                        ApplySearchFilter();
                        await LoadEquipmentSummary();
                        MessageBox.Show("Equipment item updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

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
                    await LoadEquipmentSummary();
                    MessageBox.Show("Equipment item added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding equipment item:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
