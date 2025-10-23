using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.KitchenVM
{
    public class KitchenViewModel : BaseViewModel
    {
        #region Fields & Services
        private readonly KitchenService _kitchenService = new();
        private CancellationTokenSource? _searchDebounceToken;
        private const int PageSize = 20;
        #endregion

        #region Properties
        public ObservableCollection<Kitchen> Items { get; set; } = new();

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
                _ = ApplySearchFilterDebounced();
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

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); }
        }

        private int _lowStockCount;
        public int LowStockCount
        {
            get => _lowStockCount;
            set { _lowStockCount = value; OnPropertyChanged(); }
        }

        private int _normalStockCount;
        public int NormalStockCount
        {
            get => _normalStockCount;
            set { _normalStockCount = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand DeleteKitchenItemCommand { get; }
        public ICommand EditKitchenItemCommand { get; }
        public ICommand AddKitchenItemCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportCsvCommand { get; }
        #endregion

        #region Constructor
        public KitchenViewModel()
        {
            DeleteKitchenItemCommand = new RelayCommand<Kitchen>(async k => await DeleteKitchenItem(k));
            EditKitchenItemCommand = new RelayCommand<Kitchen>(EditKitchenItem);
            AddKitchenItemCommand = new RelayCommand(AddNewKitchenItem);
            NextPageCommand = new RelayCommand(async () => await LoadPage(CurrentPage + 1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await LoadPage(CurrentPage - 1), () => CurrentPage > 1);
            ExportPdfCommand = new RelayCommand(async () => await ExportAsPdf());
            ExportCsvCommand = new RelayCommand(async () => await ExportAsCsv());

            _ = LoadPage(1);
        }
        #endregion

        #region Methods: Load & Search
        public async Task LoadPage(int pageNumber)
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var listTask = _kitchenService.GetKitchenPageAsync(pageNumber, PageSize);
                var summaryTask = _kitchenService.GetKitchenSummaryAsync();
                await Task.WhenAll(listTask, summaryTask);

                var list = listTask.Result ?? new List<Kitchen>();
                var summary = summaryTask.Result;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items = new ObservableCollection<Kitchen>(list);
                    OnPropertyChanged(nameof(Items));
                });

                if (summary != null)
                {
                    TotalCount = summary.TotalCount;
                    NormalStockCount = summary.NormalCount;
                    LowStockCount = summary.LowCount;
                    TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                }

                CurrentPage = pageNumber;
            }
            catch (Exception ex)
            {
                ShowMessage($"Error loading kitchen items:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally { IsLoading = false; }
        }

        private async Task ApplySearchFilterDebounced()
        {
            _searchDebounceToken?.Cancel();
            var cts = new CancellationTokenSource();
            _searchDebounceToken = cts;

            try
            {
                await Task.Delay(400, cts.Token);
                await ApplySearchFilter();
            }
            catch (TaskCanceledException) { }
        }

        private async Task ApplySearchFilter()
        {
         

            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadPage(1);
                    return;
                }

                if (IsLoading) return;
                IsLoading = true;

                var result = await _kitchenService.SearchKitchenItemsAsync(SearchText);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items = new ObservableCollection<Kitchen>(result ?? new List<Kitchen>());
                    OnPropertyChanged(nameof(Items));
                });
            }
            catch (Exception ex)
            {
                ShowMessage($"Search failed:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally { IsLoading = false; }
        }
        #endregion

        #region Methods: Count Updater
        public async Task UpdateKitchenCounts()
        {
            try
            {
                var (total, low, normal) = await _kitchenService.GetKitchenCountsAsync();
                TotalCount = total;
                LowStockCount = low;
                NormalStockCount = normal;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error updating kitchen counts: {ex.Message}");
            }
        }
        #endregion

        #region Methods: CRUD
        private async Task DeleteKitchenItem(Kitchen item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show(
                $"Delete {item.ItemName}?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                if (await _kitchenService.DeleteKitchenItemAsync(item.Id))
                {
                    Items.Remove(item);
                    await LoadPage(CurrentPage);
                    await UpdateKitchenCounts(); // Call counter update
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Delete failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally { IsLoading = false; }
        }

        private void EditKitchenItem(Kitchen item)
        {
            if (item == null) return;
            new EditKitchenItem(item, this).ShowDialog();
        }

        private void AddNewKitchenItem()
        {
            new KitchenItemAdd(this).ShowDialog();
        }
        #endregion

        #region Methods: Export
        private async Task ExportAsPdf()
        {
            IsLoading = true;
            try
            {
                var data = await _kitchenService.GetKitchenPageAsync(1, int.MaxValue);
                if (data.Count == 0)
                {
                    ShowMessage("No data to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    data, "Kitchen_Inventory_Report",
                    "Id", "BaseUrl", "RequestClientOptions",
                    "TableName", "PrimaryKey", "UpdatedAt", "CreatedAt");
            }
            catch (Exception ex)
            {
                ShowMessage($"Export PDF failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally { IsLoading = false; }
        }

        private async Task ExportAsCsv()
        {
            IsLoading = true;
            try
            {
                var data = await _kitchenService.GetKitchenPageAsync(1, int.MaxValue);
                if (data.Count == 0)
                {
                    ShowMessage("No data to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    data, "Kitchen_Inventory_Report",
                    "Id", "BaseUrl", "RequestClientOptions",
                    "TableName", "PrimaryKey", "UpdatedAt", "CreatedAt");
            }
            catch (Exception ex)
            {
                ShowMessage($"Export CSV failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally { IsLoading = false; }
        }
        #endregion
    }
}
