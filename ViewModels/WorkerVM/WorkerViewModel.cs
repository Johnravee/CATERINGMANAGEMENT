/*
 * FILE: WorkerViewModel.cs
 * PURPOSE: Handles loading, pagination, search (with debounce), adding, editing, deleting, 
 *           and realtime synchronization for Worker data.
 * 
 * RESPONSIBILITIES:
 *  - Load workers with pagination
 *  - Search workers with debounce
 *  - Add, edit, and delete workers
 *  - Subscribe to Supabase Realtime updates (insert, update, delete)
 *  - Display messages for user interaction
 */

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

namespace CATERINGMANAGEMENT.ViewModels.WorkerVM
{
    public class WorkerViewModel : BaseViewModel
    {
        #region Constants
        private const int PageSize = 20;
        #endregion

        #region Services
        private readonly WorkerService _workerService = new();
        #endregion

        #region Fields
        private CancellationTokenSource? _searchDebounceToken;
        #endregion

        #region Collections
        public ObservableCollection<Worker> Items { get; } = new();
        #endregion

        #region UI State
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
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

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _ = ApplySearchDebouncedAsync();
            }
        }
        #endregion

        #region Commands
        public ICommand DeleteWorkerCommand { get; }
        public ICommand EditWorkerCommand { get; }
        public ICommand AddWorkerCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        #endregion

        #region Constructor
        public WorkerViewModel()
        {
            DeleteWorkerCommand = new RelayCommand<Worker>(async w => await DeleteWorkerAsync(w));
            EditWorkerCommand = new RelayCommand<Worker>(EditWorker);
            AddWorkerCommand = new RelayCommand(AddWorker);
            NextPageCommand = new RelayCommand(async () => await LoadPageAsync(CurrentPage + 1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await LoadPageAsync(CurrentPage - 1), () => CurrentPage > 1);

            _ = LoadPageAsync(1);

            // ✅ Start realtime subscription
            _ = Task.Run(SubscribeToRealtime);
        }
        #endregion

        #region Load Workers
        public async Task LoadPageAsync(int pageNumber)
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var (workers, totalCount) = await _workerService.GetWorkersPageAsync(pageNumber);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    foreach (var w in workers ?? new List<Worker>())
                        Items.Add(w);
                });

                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
                CurrentPage = Math.Max(1, Math.Min(pageNumber, TotalPages == 0 ? 1 : TotalPages));
            }
            catch (Exception ex)
            {
                ShowMessage($"Error loading workers:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex, "Error loading workers page");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Search
        private async Task ApplySearchDebouncedAsync()
        {
            _searchDebounceToken?.Cancel();
            var cts = new CancellationTokenSource();
            _searchDebounceToken = cts;

            try
            {
                await Task.Delay(400, cts.Token);
                await ApplySearchAsync();
            }
            catch (TaskCanceledException) { }
        }

        private async Task ApplySearchAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadPageAsync(1);
                    return;
                }

                IsLoading = true;

                var results = await _workerService.SearchWorkersAsync(SearchText);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    foreach (var w in results ?? new List<Worker>())
                        Items.Add(w);
                });

                TotalPages = 1;
                CurrentPage = 1;
            }
            catch (Exception ex)
            {
                ShowMessage($"Search failed:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex, "Error searching workers");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region CRUD Operations
        private async Task DeleteWorkerAsync(Worker worker)
        {
            if (worker == null) return;

            var confirm = MessageBox.Show($"Delete {worker.Name}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                if (await _workerService.DeleteWorkerAsync(worker.Id))
                {
                    Application.Current.Dispatcher.Invoke(() => Items.Remove(worker));
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Delete failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLogger.Error(ex, "Error deleting worker");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void EditWorker(Worker worker)
        {
            if (worker == null) return;

            try
            {
                var editWindow = new EditWorker(worker);
                editWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error opening EditWorker window");
                ShowMessage($"Failed to open edit window:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddWorker()
        {
            try
            {
                var addWindow = new AddWorker();
                addWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error opening AddWorker window");
                ShowMessage($"Failed to open add worker window:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Realtime Sync
        private async Task SubscribeToRealtime()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                // Subscribe to "workers" table in "public" schema
                var channel = client.Realtime.Channel("realtime", "public", "workers");

                //  Log all events
                channel.AddPostgresChangeHandler(ListenType.All, (sender, change) =>
                {
                    Debug.WriteLine($"Realtime event: {change.Event}");
                    Debug.WriteLine($"Payload: {change.Payload}");
                });

                // INSERT handler
                channel.AddPostgresChangeHandler(ListenType.Inserts, (sender, change) =>
                {
                    var inserted = change.Model<Worker>();
                    if (inserted == null)
                    {
                        Debug.WriteLine("[Realtime Insert] Failed to deserialize worker.");
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(w => w.Id == inserted.Id);
                        if (existing == null)
                        {
                            Items.Insert(0, inserted);
                            Debug.WriteLine($"Realtime Insert: Added worker ID {inserted.Id}");
                        }
                    });
                });

                // UPDATE handler
                channel.AddPostgresChangeHandler(ListenType.Updates, (sender, change) =>
                {
                    var updated = change.Model<Worker>();
                    if (updated == null)
                    {
                        Debug.WriteLine("[Realtime Update] Failed to deserialize worker.");
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(w => w.Id == updated.Id);
                        if (existing != null)
                        {
                            var index = Items.IndexOf(existing);
                            Items[index] = updated;
                            Debug.WriteLine($"Realtime Update: Updated worker ID {updated.Id}");
                        }
                        else
                        {
                            Items.Insert(0, updated);
                            Debug.WriteLine($"Realtime Update: Inserted missing worker ID {updated.Id}");
                        }
                    });
                });

                var result = await channel.Subscribe();
                AppLogger.Success($"✅ Subscribed to realtime worker updates: {result}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error subscribing to realtime worker updates");
            }
        }
        #endregion
    }
}
