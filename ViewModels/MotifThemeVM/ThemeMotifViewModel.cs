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

namespace CATERINGMANAGEMENT.ViewModels.MotifThemeVM
{
    public class ThemeMotifViewModel : BaseViewModel
    {
        private readonly ThemeMotifService _themeMotifService = new();

        private ObservableCollection<ThemeMotif> _items = new();
        public ObservableCollection<ThemeMotif> Items
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

        public ICommand AddThemeMotifCommand { get; }
        public ICommand EditThemeMotifCommand { get; }
        public ICommand DeleteThemeMotifCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public ThemeMotifViewModel()
        {
            AddThemeMotifCommand = new RelayCommand(InsertThemeMotif);
            EditThemeMotifCommand = new RelayCommand<ThemeMotif>(EditThemeMotif);
            DeleteThemeMotifCommand = new RelayCommand<ThemeMotif>(async (m) => await DeleteThemeMotifAsync(m));
            NextPageCommand = new RelayCommand(async () => await NextPageAsync());
            PrevPageCommand = new RelayCommand(async () => await PrevPageAsync());

            ExportPdfCommand = new RelayCommand(async () => await _themeMotifService.ExportThemeMotifsToPdfAsync());
            ExportCsvCommand = new RelayCommand(async () => await _themeMotifService.ExportThemeMotifsToCsvAsync());

            _ = LoadItemsAsync();
            _ = SubscribeToRealtimeAsync();
        }

        public async Task LoadItemsAsync()
        {
            IsLoading = true;
            try
            {
                var (items, totalCount) = await _themeMotifService.GetThemeMotifPageAsync(CurrentPage);
                Items = new ObservableCollection<ThemeMotif>(items);
                TotalCount = totalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / 10));
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error loading ThemeMotif items");
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
                    var searchResults = await _themeMotifService.SearchThemeMotifsAsync(SearchText.Trim());
                    Items = new ObservableCollection<ThemeMotif>(searchResults);
                    TotalPages = 1;
                    CurrentPage = 1;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error applying ThemeMotif search filter");
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

        private static void InsertThemeMotif()
        {
            new AddThemeMotif().ShowDialog();
        }

        private static void EditThemeMotif(ThemeMotif motif)
        {
            if (motif == null) return;
            new EditThemeMotif(motif).ShowDialog();
        }

        private async Task DeleteThemeMotifAsync(ThemeMotif motif)
        {
            if (motif == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete '{motif.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool success = await _themeMotifService.DeleteThemeMotifAsync(motif.Id);
                if (success)
                {
                    Items.Remove(motif);
                    await RefreshThemeMotifCount();
                    TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / 10));
                    AppLogger.Success($"Deleted ThemeMotif '{motif.Name}' successfully.");
                    ShowMessage("Deleted successfully", "Success");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("23503") || ex.Message.Contains("foreign key constraint"))
                {
                    ShowMessage(
                        "Cannot delete this Theme & Motif because it is still referenced in existing records.",
                        "Delete Blocked"
                    );
                }
                else
                {
                    ShowMessage("An unexpected error occurred while deleting the Theme & Motif.", "Error");
                }
            }
        }

        private async Task SubscribeToRealtimeAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var channel = client.Realtime.Channel("realtime", "public", "thememotif");

                // ✅ Handle INSERT
                channel.AddPostgresChangeHandler(ListenType.Inserts, async (sender, change) =>
                {
                    var inserted = change.Model<ThemeMotif>();
                    if (inserted == null) return;

                    var fullInserted = await _themeMotifService.GetThemeMotifByIdAsync(inserted.Id);
                    if (fullInserted == null) return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!Items.Any(m => m.Id == fullInserted.Id))
                        {
                            Items.Insert(0, fullInserted);
                            AppLogger.Info($"Realtime Insert: Added ThemeMotif ID {fullInserted.Id}");
                        }
                    });

                    await RefreshThemeMotifCount();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / 10));
                    });
                });

                // ✅ Handle UPDATE
                channel.AddPostgresChangeHandler(ListenType.Updates, async (sender, change) =>
                {
                    var updated = change.Model<ThemeMotif>();
                    if (updated == null) return;

                    var fullUpdated = await _themeMotifService.GetThemeMotifByIdAsync(updated.Id);
                    if (fullUpdated == null) return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(m => m.Id == fullUpdated.Id);
                        if (existing != null)
                        {
                            var index = Items.IndexOf(existing);
                            Items[index] = fullUpdated;
                            AppLogger.Info($"Realtime Update: Updated ThemeMotif ID {fullUpdated.Id}");
                        }
                        else
                        {
                            Items.Insert(0, fullUpdated);
                        }
                    });
                });

                // ✅ Subscribe after all handlers are attached
                var subscribeResult = await channel.Subscribe();
                AppLogger.Success($"Subscribed to realtime ThemeMotif updates: {subscribeResult}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error subscribing to realtime ThemeMotif updates");
            }
        }



        private async Task RefreshThemeMotifCount()
        {
            try
            {
                int totalCount = await _themeMotifService.GetTotalThemeMotifCountAsync();
                TotalCount = totalCount;
                Debug.WriteLine($"Refreshed ThemeMotif count: {TotalCount}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error refreshing ThemeMotif count");
            }
        }
    }
}
