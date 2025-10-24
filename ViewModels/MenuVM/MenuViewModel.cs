using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace CATERINGMANAGEMENT.ViewModels.MenuVM
{
    public class MenuViewModel : BaseViewModel
    {
        private readonly MenuService _menuService = new();

        private ObservableCollection<MenuOption> _items = new();
        public ObservableCollection<MenuOption> Items
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

        public ICommand AddMenuCommand { get; }
        public ICommand EditMenuCommand { get; }
        public ICommand DeleteMenuCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }

        public ICommand ExportPdfCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public MenuViewModel()
        {
            AddMenuCommand = new RelayCommand(InsertMenuItem);
            EditMenuCommand = new RelayCommand<MenuOption>(EditMenu);
            DeleteMenuCommand = new RelayCommand<MenuOption>(async (m) => await DeleteMenuAsync(m));
            NextPageCommand = new RelayCommand(async () => await NextPageAsync());
            PrevPageCommand = new RelayCommand(async () => await PrevPageAsync());

            ExportPdfCommand = new RelayCommand(async () => await _menuService.ExportMenusToPdfAsync());
            ExportCsvCommand = new RelayCommand(async () => await _menuService.ExportMenusToCsvAsync());

            _ = LoadItemsAsync();
            _ = SubscribeToRealtimeAsync();
        }

        public async Task LoadItemsAsync()
        {
            IsLoading = true;
            try
            {
                var (items, totalCount) = await _menuService.GetMenuPageAsync(CurrentPage);
                Items = new ObservableCollection<MenuOption>(items);
                TotalCount = totalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / 10));
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error loading menu items");
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
                    var searchResults = await MenuService.SearchMenusAsync(SearchText.Trim());
                    Items = new ObservableCollection<MenuOption>(searchResults);
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

        private static void InsertMenuItem()
        {
            new AddMenu().ShowDialog();
        }

        private static void EditMenu(MenuOption menu)
        {
            if (menu == null) return;
            new EditMenu(menu).ShowDialog();
        }

        private async Task DeleteMenuAsync(MenuOption menu)
        {
            if (menu == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete '{menu.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool success = await _menuService.DeleteMenuAsync(menu.Id);
                if (success)
                {
                    Items.Remove(menu);
                    TotalCount--;
                    TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / 10));
                    AppLogger.Success($"Deleted menu '{menu.Name}' successfully.");
                    ShowMessage("Deleted successfully", "Success");
                }
             
            }
            catch (Exception ex)
            { 
                
                if (ex.Message.Contains("23503") || ex.Message.Contains("foreign key constraint"))
                {
                    ShowMessage(
                        "Cannot delete this menu option because it is still referenced in existing reservations.",
                        "Delete Blocked"
                    );
                }
                else
                {
                    ShowMessage("An unexpected error occurred while deleting the menu.", "Error");
                }
            }

        }

        // Real-time subscription for inserts, updates, deletes
        private async Task SubscribeToRealtimeAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var channel = client.Realtime.Channel("realtime", "public", "menu_options");

                channel.AddPostgresChangeHandler(ListenType.Inserts, (sender, change) =>
                {
                    var inserted = change.Model<MenuOption>();
                    if (inserted == null) return;

                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        if (!Items.Any(m => m.Id == inserted.Id))
                        {
                            Items.Insert(0, inserted);
                             await RefreshMenuCount();
                            AppLogger.Info($"Realtime Insert: Added MenuOption ID {inserted.Id}");
                        }
                    });
                });

                channel.AddPostgresChangeHandler(ListenType.Updates, (sender, change) =>
                {
                    var updated = change.Model<MenuOption>();
                    if (updated == null) return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(m => m.Id == updated.Id);
                        if (existing != null)
                        {
                            var index = Items.IndexOf(existing);
                            Items[index] = updated;
                            AppLogger.Info($"Realtime Update: Updated MenuOption ID {updated.Id}");
                        }
                        else
                        {
                            Items.Insert(0, updated);
                            TotalCount++;
                            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / 10));
                            AppLogger.Info($"Realtime Update: Inserted missing MenuOption ID {updated.Id}");
                        }
                    });
                });

               

                var subscribeResult = await channel.Subscribe();
                AppLogger.Success($"Subscribed to realtime menu updates: {subscribeResult}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error subscribing to realtime menu updates");
            }
        }

        private async Task RefreshMenuCount()
        {
            try
            {
                int totalCount = await _menuService.GetTotalMenuOptionsCountAsync();
                TotalCount = totalCount;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error refreshing menu count");
            }
        }

    }
}
