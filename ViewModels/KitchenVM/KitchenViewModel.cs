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
        private readonly KitchenService _kitchenService = new();
        private ObservableCollection<Kitchen> _kitchenItems = new();
        private ObservableCollection<Kitchen> _filteredKitchenItems = new();

        private const int PageSize = 20;

        public ObservableCollection<Kitchen> Items
        {
            get => _filteredKitchenItems;
            set { _filteredKitchenItems = value; OnPropertyChanged(); }
        }

        public int TotalCount { get; set; }
        public int LowStockCount { get; set; }
        public int NormalStockCount { get; set; }

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
                _ = ApplySearchFilter();
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

        public ICommand DeleteKitchenItemCommand { get; set; }
        public ICommand EditKitchenItemCommand { get; set; }
        public ICommand AddKitchenItemCommand { get; set; }
        public ICommand NextPageCommand { get; set; }
        public ICommand PrevPageCommand { get; set; }
        public ICommand ExportPdfCommand { get; set; }
        public ICommand ExportCsvCommand { get; set; }

        public KitchenViewModel()
        {
            DeleteKitchenItemCommand = new RelayCommand<Kitchen>(async (k) => await DeleteKitchenItem(k));
            EditKitchenItemCommand = new RelayCommand<Kitchen>((k) => EditKitchenItem(k));
            AddKitchenItemCommand = new RelayCommand(() => AddNewKitchenItem());
            NextPageCommand = new RelayCommand(async () => await NextPage(), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await PrevPage(), () => CurrentPage > 1);
            ExportPdfCommand = new RelayCommand(async () => await ExportAsPdf());
            ExportCsvCommand = new RelayCommand(async () => await ExportAsCsv());
        }

        public async Task LoadItems()
        {
            IsLoading = true;
            try
            {
                _kitchenItems.Clear();
                Items.Clear();
                await LoadPage(1);
            }
            catch (Exception ex)
            {
                ShowMessage($"Error loading kitchen items:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var list = await _kitchenService.GetKitchenPageAsync(pageNumber, PageSize);
                _kitchenItems = new ObservableCollection<Kitchen>(list);
                await ApplySearchFilter();
                await LoadKitchenSummary();
                CurrentPage = pageNumber;
                UpdatePagination();
            }
            catch (Exception ex)
            {
                ShowMessage($"Error loading page:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async Task ApplySearchFilter()
        {
            IsLoading = true;
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    Items = new ObservableCollection<Kitchen>(_kitchenItems);
                }
                else
                {
                    var results = await _kitchenService.SearchKitchenItemsAsync(SearchText);
                    Items = new ObservableCollection<Kitchen>(results);
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Search error:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadKitchenSummary()
        {
            var summary = await _kitchenService.GetKitchenSummaryAsync();
            if (summary != null)
            {
                TotalCount = summary.TotalCount;
                NormalStockCount = summary.NormalCount;
                LowStockCount = summary.LowCount;

                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(NormalStockCount));
                OnPropertyChanged(nameof(LowStockCount));
            }
        }

        private void UpdatePagination()
        {
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(TotalPages));
        }

        private async Task DeleteKitchenItem(Kitchen item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete {item.ItemName}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            bool success = await _kitchenService.DeleteKitchenItemAsync(item.Id);

            if (success)
            {
                _kitchenItems.Remove(item);
                await ApplySearchFilter();
                await LoadKitchenSummary();
            }
        }

        private void EditKitchenItem(Kitchen item)
        {
            if (item == null) return;

            var editWindow = new EditKitchenItem(item, this);
            editWindow.ShowDialog();
        }

        private void AddNewKitchenItem()
        {
            var addWindow = new KitchenItemAdd(this);
            addWindow.ShowDialog();
        }

      

        private async Task ExportAsPdf()
        {
            IsLoading = true;
            try
            {
                var data = await _kitchenService.GetAllKitchenItemsAsync();
                if (data == null || data.Count == 0)
                {
                    ShowMessage("No data to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DataGridToPdf.DataGridToPDF(data, "Kitchen_Inventory_Report", "Id", "BaseUrl", "RequestClientOptions", "TableName", "PrimaryKey", "UpdatedAt", "CreatedAt");
            }
            catch (Exception ex)
            {
                ShowMessage($"Export PDF failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportAsCsv()
        {
            IsLoading = true;
            try
            {
                var data = await _kitchenService.GetAllKitchenItemsAsync();
                if (data == null || data.Count == 0)
                {
                    ShowMessage("No data to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DatagridToCsv.ExportToCsv(data, "Kitchen_Inventory_Report", "Id", "BaseUrl", "RequestClientOptions", "TableName", "PrimaryKey", "UpdatedAt", "CreatedAt");
            }
            catch (Exception ex)
            {
                ShowMessage($"Export CSV failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
