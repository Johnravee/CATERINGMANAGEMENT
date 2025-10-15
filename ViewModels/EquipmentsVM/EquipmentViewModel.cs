using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.EquipmentsVM
{
    public class EquipmentViewModel : INotifyPropertyChanged
    {
        private readonly EquipmentService _equipmentService = new();

        public ObservableCollection<Equipment> Items { get; set; } = new();

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

        private const int PageSize = 20;
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

        public ICommand DeleteEquipmentCommand { get; }
        public ICommand EditEquipmentCommand { get; }
        public ICommand AddEquipmentCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportCsvCommand { get; }

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

        public async Task LoadPage(int pageNumber)
        {
            IsLoading = true;

            try
            {
                Items.Clear();
                var equipments = await _equipmentService.GetEquipmentsAsync(pageNumber, PageSize);
                foreach (var item in equipments)
                    Items.Add(item);

                CurrentPage = pageNumber;

                var summary = await _equipmentService.GetEquipmentSummaryAsync();
                if (summary != null)
                {
                    TotalCount = summary.TotalCount;
                    DamagedCount = summary.DamagedCount;
                    GoodConditionCount = summary.GoodCount;
                    TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error loading data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
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

                Items = new ObservableCollection<Equipment>(response.Models ?? new List<Equipment>());
                OnPropertyChanged(nameof(Items));
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error during search: {ex.Message}");
                MessageBox.Show($"Error filtering data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteEquipment(Equipment item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete {item.ItemName}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                bool deleted = await _equipmentService.DeleteEquipmentAsync(item.Id ?? 0);
                if (deleted)
                {
                    Items.Remove(item);
                    await LoadPage(CurrentPage);
                    AppLogger.Success($"Deleted equipment: {item.ItemName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task EditEquipment(Equipment item)
        {
            if (item == null) return;

            var editWindow = new EditEquipments(item);
            if (editWindow.ShowDialog() == true && editWindow.Equipments != null)
            {
                var updated = await _equipmentService.UpdateEquipmentAsync(editWindow.Equipments);
                if (updated != null)
                {
                    var index = Items.IndexOf(item);
                    if (index >= 0)
                        Items[index] = updated;

                    MessageBox.Show("Equipment updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void AddNewEquipment()
        {
            var addWindow = new EquipmentItemAdd();
            if (addWindow.ShowDialog() == true && addWindow.NewEquipment != null)
            {
                _ = InsertEquipmentItem(addWindow.NewEquipment);
            }
        }

        private async Task InsertEquipmentItem(Equipment item)
        {
            if (item == null) return;

            IsLoading = true;

            try
            {
                var inserted = await _equipmentService.InsertEquipmentAsync(item);
                if (inserted != null)
                {
                    Items.Add(inserted);
                    await LoadPage(CurrentPage);
                    MessageBox.Show("New equipment added!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<List<Equipment>> FetchAllEquipments()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Equipment>()
                    .Order(x => x.ItemName, Supabase.Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models ?? new List<Equipment>();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error fetching all equipment: {ex.Message}");
                return new List<Equipment>();
            }
        }

        private async Task ExportAsPdf()
        {
            try
            {
                IsLoading = true;
                var equipments = await FetchAllEquipments();
                if (equipments.Count == 0)
                {
                    MessageBox.Show("No equipment data available to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DataGridToPdf.DataGridToPDF(equipments, "Equipments_Inventory_Report", "Id", "BaseUrl", "RequestClientOptions", "TableName", "PrimaryKey", "UpdatedAt", "CreatedAt");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error exporting to PDF: {ex.Message}");
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportAsCsv()
        {
            try
            {
                IsLoading = true;
                var equipments = await FetchAllEquipments();
                if (equipments.Count == 0)
                {
                    MessageBox.Show("No equipment data available to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DatagridToCsv.ExportToCsv(equipments, "EquipmentsInventory.csv", "Id", "BaseUrl", "RequestClientOptions", "TableName", "PrimaryKey", "UpdatedAt", "CreatedAt");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error exporting to CSV: {ex.Message}");
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
