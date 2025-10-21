/*
 * FILE: EquipmentViewModel.cs
 * PURPOSE: Acts as the main ViewModel for the Equipment module.
 *          Handles equipment loading, pagination, searching, CRUD operations,
 *          and exporting data to PDF/CSV.
 * 
 * RESPONSIBILITIES:
 *  - Expose properties for equipment list, summary counts, and pagination
 *  - Debounced search filtering
 *  - Insert, update, delete operations via EquipmentService
 *  - Export equipment data to PDF or CSV
 *  - Open related windows (Add/Edit)
 */

using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.EquipmentsVM
{
    public class EquipmentViewModel : BaseViewModel
    {
        #region Fields & Services
        private readonly EquipmentService _equipmentService = new();
        private CancellationTokenSource? _searchDebounceToken;
        #endregion

        #region Properties
        public ObservableCollection<Equipment> Items { get; set; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); _ = ApplySearchFilterDebounced(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private int _currentPage = 1;
        public int CurrentPage { get => _currentPage; set { _currentPage = value; OnPropertyChanged(); } }

        private int _totalPages = 1;
        public int TotalPages { get => _totalPages; set { _totalPages = value; OnPropertyChanged(); } }

        private int _totalCount;
        public int TotalCount { get => _totalCount; set { _totalCount = value; OnPropertyChanged(); } }

        private int _damagedCount;
        public int DamagedCount { get => _damagedCount; set { _damagedCount = value; OnPropertyChanged(); } }

        private int _goodConditionCount;
        public int GoodConditionCount { get => _goodConditionCount; set { _goodConditionCount = value; OnPropertyChanged(); } }
        #endregion

        #region Commands
        public ICommand DeleteEquipmentCommand { get; }
        public ICommand EditEquipmentCommand { get; }
        public ICommand AddEquipmentCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportCsvCommand { get; }
        #endregion

        #region Constructor
        public EquipmentViewModel()
        {
            DeleteEquipmentCommand = new RelayCommand<Equipment>(async e => await DeleteEquipment(e));
            EditEquipmentCommand = new RelayCommand<Equipment>(async e => await EditEquipment(e));
            AddEquipmentCommand = new RelayCommand(AddNewEquipment);
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
                var listTask = _equipmentService.GetEquipmentsAsync(pageNumber, 20);
                var summaryTask = _equipmentService.GetEquipmentSummaryAsync();

                await Task.WhenAll(listTask, summaryTask);

                var equipments = listTask.Result;
                var summary = summaryTask.Result;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items = new ObservableCollection<Equipment>(equipments);
                    OnPropertyChanged(nameof(Items));
                });

                CurrentPage = pageNumber;

                if (summary != null)
                {
                    TotalCount = summary.TotalCount;
                    DamagedCount = summary.DamagedCount;
                    GoodConditionCount = summary.GoodCount;
                    TotalPages = (int)Math.Ceiling((double)TotalCount / 20);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error loading equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadPage(CurrentPage);
                return;
            }

            IsLoading = true;
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Equipment>()
                    .Filter(x => x.ItemName, Supabase.Postgrest.Constants.Operator.ILike, $"%{SearchText}%")
                    .Get();

                var result = response.Models ?? new List<Equipment>();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items = new ObservableCollection<Equipment>(result);
                    OnPropertyChanged(nameof(Items));
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Search error: {ex.Message}");
                MessageBox.Show($"Error filtering data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }
        #endregion

        #region Methods: CRUD
        private async Task DeleteEquipment(Equipment item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show($"Delete {item.ItemName}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                if (await _equipmentService.DeleteEquipmentAsync(item.Id ?? 0))
                {
                    Items.Remove(item);
                    await LoadPage(CurrentPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally { IsLoading = false; }
        }

        private Task EditEquipment(Equipment item)
        {
            if (item == null)
                return Task.CompletedTask;

            new EditEquipments(item, this).ShowDialog();
            return Task.CompletedTask;
        }

        private void AddNewEquipment()
        {
            new EquipmentItemAdd(this).ShowDialog();
        }
        #endregion

        #region Methods: Export
        private async Task ExportAsPdf()
        {
            try
            {
                IsLoading = true;
                var equipments = await _equipmentService.GetEquipmentsAsync(1, int.MaxValue);
                if (equipments.Count == 0)
                {
                    MessageBox.Show("No equipment data to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DataGridToPdf.DataGridToPDF(equipments, "Equipments_Inventory_Report",
                    "Id", "BaseUrl", "RequestClientOptions", "TableName", "PrimaryKey", "UpdatedAt", "CreatedAt");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Export PDF error: {ex.Message}");
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        private async Task ExportAsCsv()
        {
            try
            {
                IsLoading = true;
                var equipments = await _equipmentService.GetEquipmentsAsync(1, int.MaxValue);
                if (equipments.Count == 0)
                {
                    MessageBox.Show("No equipment data to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DatagridToCsv.ExportToCsv(equipments, "EquipmentsInventory.csv",
                    "Id", "BaseUrl", "RequestClientOptions", "TableName", "PrimaryKey", "UpdatedAt", "CreatedAt");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Export CSV error: {ex.Message}");
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }
        #endregion
    }
}
