using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Supabase.Realtime.PostgresChanges.PostgresChangesOptions;

namespace CATERINGMANAGEMENT.ViewModels.PackageVM
{
    public class PackageViewModel : BaseViewModel
    {
        private readonly PackageService _packageService = new();

        private ObservableCollection<Package> _allItems = new();
        private ObservableCollection<Package> _filteredItems = new();

        private const int PageSize = 10;

        public ObservableCollection<Package> Items
        {
            get => _filteredItems;
            set { _filteredItems = value; OnPropertyChanged(); }
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

        // Commands
        public ICommand AddPackageCommand { get; }
        public ICommand EditPackageCommand { get; }
        public ICommand DeletePackageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public PackageViewModel()
        {
            AddPackageCommand = new RelayCommand(() => new AddPackage().ShowDialog());
            EditPackageCommand = new RelayCommand<Package>(p => { if (p != null) new EditPackage(p).ShowDialog(); });
            DeletePackageCommand = new RelayCommand<Package>(async p => await DeletePackageAsync(p));
            NextPageCommand = new RelayCommand(async () => await LoadPageAsync(CurrentPage + 1));
            PrevPageCommand = new RelayCommand(async () => await LoadPageAsync(CurrentPage - 1));
            ExportPdfCommand = new RelayCommand(async () => await _packageService.ExportPackagesToPdfAsync());
            ExportCsvCommand = new RelayCommand(async () => await _packageService.ExportPackagesToCsvAsync());

            _ = LoadPageAsync(1);
            _ = RefreshPackageCount();
            _ = Task.Run(SubscribeToRealtime);
        }

        public async Task LoadPageAsync(int page)
        {
            if (page < 1) page = 1;

            IsLoading = true;
            try
            {
                var (items, totalCount) = await _packageService.GetPackagePageAsync(page);

                _allItems.Clear();
                foreach (var item in items) _allItems.Add(item);

                TotalCount = totalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
                CurrentPage = page;

                await ApplySearchFilterAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading packages:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (string.IsNullOrWhiteSpace(_searchText))
                {
                    Items = new ObservableCollection<Package>(_allItems);
                }
                else
                {
                    var searchResults = await _packageService.SearchPackagesAsync(_searchText.Trim());
                    Items = new ObservableCollection<Package>(searchResults);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeletePackageAsync(Package item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete '{item.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool deleted = await _packageService.DeletePackageAsync(item.Id);
                if (deleted)
                {
                    _allItems.Remove(item);
                    await RefreshPackageCount();
                    await ApplySearchFilterAsync();
                    MessageBox.Show("Deleted successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to delete package.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting package:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task RefreshPackageCount()
        {
            try
            {
                _packageService.InvalidateAllCaches();

                var count = await _packageService.GetPackageCountAsync();
                TotalCount = count;
                Debug.WriteLine($"Package count refreshed: {count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching package count:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Realtime Updates
        private async Task SubscribeToRealtime()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var channel = client.Realtime.Channel("realtime", "public", "packages");

                // Generic handler
                channel.AddPostgresChangeHandler(ListenType.All, (sender, change) =>
                {
                    Debug.WriteLine("Realtime event: " + change.Event);
                    Debug.WriteLine("Payload: " + change.Payload);
                });

                // Insert
                channel.AddPostgresChangeHandler(ListenType.Inserts, (sender, change) =>
                {
                    var inserted = change.Model<Package>();
                    if (inserted == null) return;

                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var existing = _allItems.FirstOrDefault(p => p.Id == inserted.Id);
                        if (existing == null)
                        {
                            _allItems.Insert(0, inserted);
                            await RefreshPackageCount();
                        }
                        else
                        {
                            var index = _allItems.IndexOf(existing);
                            _allItems[index] = inserted;
                            await RefreshPackageCount();
                        }
                        await ApplySearchFilterAsync();
                    });
                });

                // Update
                channel.AddPostgresChangeHandler(ListenType.Updates, (sender, change) =>
                {
                    var updated = change.Model<Package>();
                    if (updated == null) return;

                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var existing = _allItems.FirstOrDefault(p => p.Id == updated.Id);
                        if (existing != null)
                        {
                            var index = _allItems.IndexOf(existing);
                            _allItems[index] = updated;
                        }
                        else
                        {
                            _allItems.Insert(0, updated);
                        }
                        await ApplySearchFilterAsync();
                    });
                });

                var result = await channel.Subscribe();
                Debug.WriteLine($"✅ Subscribed to realtime package updates: {result}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error subscribing to realtime package updates: {ex.Message}");
            }
        }
        #endregion
    }
}
