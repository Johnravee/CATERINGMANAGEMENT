using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace CATERINGMANAGEMENT.ViewModels.GrazingVM
{
    public class GrazingViewModel : BaseViewModel
    {
        private readonly GrazingService _grazingService = new();

        private ObservableCollection<GrazingTable> _items = new();
        public ObservableCollection<GrazingTable> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(); }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); }
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
                _ = ApplySearchFilterAsync();
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

        public ICommand AddGrazingCommand { get; }
        public ICommand EditGrazingCommand { get; }
        public ICommand DeleteGrazingCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public GrazingViewModel()
        {
            AddGrazingCommand = new RelayCommand(InsertGrazingItem);
            EditGrazingCommand = new RelayCommand<GrazingTable>(EditGrazing);
            DeleteGrazingCommand = new RelayCommand<GrazingTable>(async (g) => await DeleteGrazingAsync(g));
            NextPageCommand = new RelayCommand(async () => await NextPageAsync());
            PrevPageCommand = new RelayCommand(async () => await PrevPageAsync());

            ExportPdfCommand = new RelayCommand(async () => await ExportGrazingPdfAsync());
            ExportCsvCommand = new RelayCommand(async () => await ExportGrazingCsvAsync());

            _ = LoadItemsAsync();
            _ = SubscribeToRealtimeAsync();
        }

        public async Task LoadItemsAsync()
        {
            IsLoading = true;
            try
            {
                var (items, totalCount) = await _grazingService.GetGrazingPageAsync(CurrentPage);
                Items = new ObservableCollection<GrazingTable>(items);
                TotalCount = totalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / 10));
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error loading grazing items");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ApplySearchFilterAsync()
        {
            IsLoading = true;
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadItemsAsync();
                }
                else
                {
                    var searchResults = await GrazingService.SearchGrazingAsync(SearchText.Trim());
                    Items = new ObservableCollection<GrazingTable>(searchResults);
                    TotalPages = 1;
                    CurrentPage = 1;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error applying search filter");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadItemsAsync();
            }
        }

        private async Task PrevPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadItemsAsync();
            }
        }

        private static void InsertGrazingItem()
        {
            new AddGrazing().ShowDialog();
        }

        private static void EditGrazing(GrazingTable grazing)
        {
            if (grazing == null) return;
            new EditGrazing(grazing).ShowDialog();
        }

        private async Task DeleteGrazingAsync(GrazingTable grazing)
        {
            if (grazing == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete '{grazing.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool success = await _grazingService.DeleteGrazingAsync(grazing.Id);
                if (success)
                {
                    Items.Remove(grazing);
                    TotalCount--;
                    TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / 10));
                    AppLogger.Success($"Deleted grazing '{grazing.Name}' successfully.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error deleting grazing item");
            }
        }

        private async Task SubscribeToRealtimeAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var channel = client.Realtime.Channel("realtime", "public", "grazing");

                channel.AddPostgresChangeHandler(ListenType.Inserts, (sender, change) =>
                {
                    var inserted = change.Model<GrazingTable>();
                    if (inserted == null) return;

                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        if (!Items.Any(g => g.Id == inserted.Id))
                        {
                            Items.Insert(0, inserted);
                            await RefreshGrazingCount();
                            AppLogger.Info($"Realtime Insert: Added Grazing ID {inserted.Id}");
                        }
                    });
                });

                channel.AddPostgresChangeHandler(ListenType.Updates, (sender, change) =>
                {
                    var updated = change.Model<GrazingTable>();
                    if (updated == null) return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(g => g.Id == updated.Id);
                        if (existing != null)
                        {
                            var index = Items.IndexOf(existing);
                            Items[index] = updated;
                            AppLogger.Info($"Realtime Update: Updated Grazing ID {updated.Id}");
                        }
                        else
                        {
                            Items.Insert(0, updated);
                            TotalCount++;
                            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / 10));
                            AppLogger.Info($"Realtime Update: Inserted missing Grazing ID {updated.Id}");
                        }
                    });
                });

                var subscribeResult = await channel.Subscribe();
                AppLogger.Success($"Subscribed to realtime grazing updates: {subscribeResult}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error subscribing to realtime grazing updates");
            }
        }

        private async Task RefreshGrazingCount()
        {
            try
            {
                int totalCount = await _grazingService.GetTotalGrazingOptionsCountAsync();
                TotalCount = totalCount;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error refreshing grazing count");
            }
        }

        private async Task ExportGrazingPdfAsync()
        {
            IsLoading = true;
            try
            {
                await _grazingService.ExportGrazingToPdfAsync();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting grazing to PDF");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportGrazingCsvAsync()
        {
            IsLoading = true;
            try
            {
                await _grazingService.ExportGrazingToCsvAsync();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting grazing to CSV");
            }
            finally
            {
                IsLoading = false;
            }
        }

    }
}
