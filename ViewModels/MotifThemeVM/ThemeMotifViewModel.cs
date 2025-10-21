/*
 * FILE: ThemeMotifViewModel.cs
 * PURPOSE: ViewModel for managing Theme & Motif records.
 *           Handles data loading, pagination, search (with debouncing),
 *           CRUD actions, and exporting (PDF/CSV) using ThemeMotifService and AppLogger.
 */

using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using CATERINGMANAGEMENT.View.Windows;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.MotifThemeVM
{
    public class ThemeMotifViewModel : BaseViewModel
    {
        #region Fields

        private ObservableCollection<ThemeMotif> _allItems = new();
        private ObservableCollection<ThemeMotif> _filteredItems = new();
        private const int PageSize = 10;

        private int _totalCount;
        private bool _isLoading;
        private string _searchText = string.Empty;
        private int _currentPage = 1;
        private int _totalPages = 1;

        private CancellationTokenSource? _debounceTokenSource;

        #endregion

        #region Properties

        public ObservableCollection<ThemeMotif> Items
        {
            get => _filteredItems;
            set { _filteredItems = value; OnPropertyChanged(); }
        }

        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                DebounceSearch();
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand AddThemeMotifCommand { get; set; }
        public ICommand EditThemeMotifCommand { get; set; }
        public ICommand DeleteThemeMotifCommand { get; set; }
        public ICommand NextPageCommand { get; set; }
        public ICommand PrevPageCommand { get; set; }
        public ICommand ExportPdfCommand { get; set; }
        public ICommand ExportCsvCommand { get; set; }

        #endregion

        #region Constructor

        public ThemeMotifViewModel()
        {
            AddThemeMotifCommand = new RelayCommand(async () => await InsertThemeMotif());
            EditThemeMotifCommand = new RelayCommand<ThemeMotif>(async (m) => await EditThemeMotif(m));
            DeleteThemeMotifCommand = new RelayCommand<ThemeMotif>(async (m) => await DeleteThemeMotif(m));
            NextPageCommand = new RelayCommand(async () => await NextPage());
            PrevPageCommand = new RelayCommand(async () => await PrevPage());
            ExportPdfCommand = new RelayCommand(async () => await ExportToPdf());
            ExportCsvCommand = new RelayCommand(async () => await ExportToCsv());

            _ = LoadItems();
        }

        #endregion

        #region Data Loading

        public async Task LoadItems()
        {
            IsLoading = true;
            try
            {
                _allItems.Clear();
                Items.Clear();
                await LoadPage(1);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error loading Theme & Motifs");
                ShowMessage($"Error loading Theme & Motifs:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadPage(int page)
        {
            IsLoading = true;
            try
            {
                int from = (page - 1) * PageSize;
                int to = from + PageSize - 1;

                var motifs = await ThemeMotifService.GetPaginatedAsync(from, to);
                var totalCount = await ThemeMotifService.GetTotalCountAsync();

                _allItems.Clear();
                foreach (var item in motifs)
                    _allItems.Add(item);

                TotalCount = totalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

                ApplySearchFilter();
                CurrentPage = page;

                AppLogger.Success($"Loaded ThemeMotif page {page}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error loading ThemeMotif page");
                ShowMessage($"Error loading ThemeMotif page:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Pagination

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

        #endregion

        #region Search with Debounce

        private void DebounceSearch(int delay = 500)
        {
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();
            var token = _debounceTokenSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, token);
                    if (!token.IsCancellationRequested)
                        await ApplySearchFilter();
                }
                catch (TaskCanceledException) { }
            });
        }

        private async Task ApplySearchFilter()
        {
            var query = _searchText?.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(query))
            {
                Items = new ObservableCollection<ThemeMotif>(_allItems);
            }
            else
            {
                try
                {
                    IsLoading = true;
                    var results = await ThemeMotifService.SearchAsync(query);
                    Items = new ObservableCollection<ThemeMotif>(results ?? []);
                    AppLogger.Info($"Filtered ThemeMotifs for query: '{query}'");
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Error filtering ThemeMotifs");
                    ShowMessage($"Error filtering Theme & Motifs:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        #endregion

        #region CRUD Operations

        private async Task InsertThemeMotif()
        {
            var addWindow = new AddThemeMotif();
            bool? result = addWindow.ShowDialog();
            if (result == true)
                await LoadItems();
        }

        private async Task EditThemeMotif(ThemeMotif item)
        {
            if (item == null) return;

            var editWindow = new EditThemeMotif(item);
            bool? result = editWindow.ShowDialog();
            if (result == true)
                await LoadItems();
        }

        private async Task DeleteThemeMotif(ThemeMotif item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete '{item.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await ThemeMotifService.DeleteAsync(item.Id, item.Name);
                _allItems.Remove(item);
                _ = ApplySearchFilter();
                await LoadPage(1);
                AppLogger.Success($"Deleted ThemeMotif: {item.Name}");
                ShowMessage("Deleted successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error deleting ThemeMotif");
                ShowMessage($"Error deleting ThemeMotif:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Export Functions

        private async Task ExportToPdf()
        {
            try
            {
                var data = await ThemeMotifService.GetPaginatedAsync(0, int.MaxValue);
                if (data == null || data.Count == 0)
                {
                    ShowMessage("No Theme & Motif found to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    data,
                    "Theme & Motif List",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt"
                );

                AppLogger.Success("Exported ThemeMotifs to PDF");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting ThemeMotifs to PDF");
                ShowMessage($"Error exporting to PDF:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportToCsv()
        {
            try
            {
                var data = await ThemeMotifService.GetPaginatedAsync(0, int.MaxValue);
                if (data == null || data.Count == 0)
                {
                    ShowMessage("No Theme & Motif found to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    data,
                    "Theme & Motif List",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt"
                );

                AppLogger.Success("Exported ThemeMotifs to CSV");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Error exporting ThemeMotifs to CSV");
                ShowMessage($"Error exporting to CSV:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
