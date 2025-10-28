/*
 * FILE: EquipmentViewModel.cs
 * PURPOSE: Acts as the main ViewModel for the Equipment module.
 *          Handles equipment loading, pagination, searching, CRUD operations,
 *          exporting data to PDF/CSV, and real-time updates via Supabase.
 */

using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

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
        public int PageSize { get => _pageSize; set { _pageSize = value; OnPropertyChanged(); } }
        private int _pageSize = 20;
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

            // Initialize realtime subscription
            _ = Task.Run(SubscribeToRealtime);
        }
        #endregion

        #region Methods: Load & Search
        public async Task LoadPage(int pageNumber)
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var equipments = await _equipmentService.GetEquipmentsAsync(pageNumber, PageSize);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items = new ObservableCollection<Equipment>(equipments);
                    OnPropertyChanged(nameof(Items));
                });

                CurrentPage = pageNumber;

                await LoadEquipmentSummaryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error loading equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally { IsLoading = false; }
        }

        public async Task LoadEquipmentSummaryAsync()
        {
            try
            {
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
                AppLogger.Error($"Error loading equipment summary: {ex.Message}");
                MessageBox.Show($"❌ Error loading equipment summary:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    await LoadPage(1);
                    await RefreshEquipmentCountsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex.Message);
            }
            finally { IsLoading = false; }
        }

        private static async Task EditEquipment(Equipment item)
        {
            if (item == null) return;

            new EditEquipments(item).ShowDialog();

            await Task.CompletedTask; 
        }


        private void AddNewEquipment()
        {
           new EquipmentItemAdd().ShowDialog();
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

        #region Methods: Realtime Updates
        private async Task SubscribeToRealtime()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                // Subscribe to the equipments table (public schema)
                var channel = client.Realtime.Channel("realtime", "public", "equipments");

                // Generic handler for all events, just logs raw payloads
                channel.AddPostgresChangeHandler(ListenType.All, (sender, change) =>
                {
                    Debug.WriteLine("Realtime event change: " + change.Event);
                    Debug.WriteLine("Realtime event change payload: " + change.Payload);
                });

                // Insert handler
                channel.AddPostgresChangeHandler(ListenType.Inserts, (sender, change) =>
                {
                    var inserted = change.Model<Equipment>();
                    if (inserted == null)
                    {
                        Debug.WriteLine("[Realtime Insert] Failed to deserialize inserted record.");
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var existing = Items.FirstOrDefault(e => e.Id == inserted.Id);
                        if (existing == null)
                        {
                            Items.Insert(0, inserted);
                            await RefreshEquipmentCountsAsync();
                            AppLogger.Info($"Realtime Insert: Added equipment ID {inserted.Id}");
                        }
                        else
                        {
                            var index = Items.IndexOf(existing);
                            Items[index] = inserted;
                            AppLogger.Info($"Realtime Insert (update existing): Updated equipment ID {inserted.Id}");
                        }
                    });
                });

                // Update handler
                channel.AddPostgresChangeHandler(ListenType.Updates, (sender, change) =>
                {
                    var updated = change.Model<Equipment>();
                    if (updated == null)
                    {
                        Debug.WriteLine("[Realtime Update] Failed to deserialize updated record.");
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var existing = Items.FirstOrDefault(e => e.Id == updated.Id);
                        if (existing != null)
                        {
                            var index = Items.IndexOf(existing);
                            Items[index] = updated;
                            await RefreshEquipmentCountsAsync();
                            AppLogger.Info($"Realtime Update: Updated equipment ID {updated.Id}");
                        }
                        else
                        {
                            Items.Insert(0, updated);
                            AppLogger.Info($"Realtime Update: Inserted missing equipment ID {updated.Id}");
                        }
                    });
                });

                var result = await channel.Subscribe();
                AppLogger.Success($"Subscribed to realtime equipment updates: {result}");
                Debug.WriteLine($"Subscribed to realtime equipment updates: {result}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error subscribing to realtime equipment updates");
            }
        }

        private async Task RefreshEquipmentCountsAsync()
        {
            try
            {
                // Invalidate the cached
                _equipmentService.InvalidateAllEquipmentCaches();


                var counts = await _equipmentService.GetEquipmentSummaryAsync();

                if (counts != null)
                {
                    TotalCount = counts.TotalCount;
                    DamagedCount = counts.DamagedCount;
                    GoodConditionCount = counts.GoodCount;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error refreshing reservation counts");
            }
        }

        #endregion



    }
}
