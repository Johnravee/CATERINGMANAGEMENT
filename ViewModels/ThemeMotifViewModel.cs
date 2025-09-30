using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class ThemeMotifViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ThemeMotif> _allItems = new();
        private ObservableCollection<ThemeMotif> _filteredItems = new();

        private const int PageSize = 10;

        public ObservableCollection<ThemeMotif> Items
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
                ApplySearchFilter();
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

        public ICommand AddThemeMotifCommand { get; set; }
        public ICommand EditThemeMotifCommand { get; set; }
        public ICommand DeleteThemeMotifCommand { get; set; }
        public ICommand NextPageCommand { get; set; }
        public ICommand PrevPageCommand { get; set; }
        public ICommand ExportPdfCommand { get; set; }
        public ICommand ExportCsvCommand { get; set; }

        public ThemeMotifViewModel()
        {
            EditThemeMotifCommand = new RelayCommand<ThemeMotif>(async (m) => await EditThemeMotif(m));
            DeleteThemeMotifCommand = new RelayCommand<ThemeMotif>(async (m) => await DeleteThemeMotif(m));
            NextPageCommand = new RelayCommand(async () => await NextPage());
            PrevPageCommand = new RelayCommand(async () => await PrevPage());
            AddThemeMotifCommand = new RelayCommand(async () => await InsertThemeMotif());

            ExportPdfCommand = new RelayCommand(ExportToPdf);
            ExportCsvCommand = new RelayCommand(ExportToCsv);

            _ = LoadItems();
        }

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
                MessageBox.Show($"Error loading Theme & Motifs:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var client = await SupabaseService.GetClientAsync();

                int from = (page - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client
                    .From<ThemeMotif>()
                    .Select("*, packages(*)")
                    .Range(from, to)
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                _allItems.Clear();
                if (response.Models != null)
                {
                    foreach (var item in response.Models)
                        _allItems.Add(item);
                }

                var countResult = await client
                    .From<ThemeMotif>()
                    .Count(CountType.Exact);

                TotalCount = countResult;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

                ApplySearchFilter();
                CurrentPage = page;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading ThemeMotif page:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void ApplySearchFilter()
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

                    var client = await SupabaseService.GetClientAsync();
                    var response = await client
                        .From<ThemeMotif>()
                        .Select("*")
                        .Filter(x => x.Name, Operator.ILike, $"%{query}%")
                        .Order(x => x.CreatedAt, Ordering.Descending)
                        .Get();

                    if (response.Models != null)
                        Items = new ObservableCollection<ThemeMotif>(response.Models);
                    else
                        Items = new ObservableCollection<ThemeMotif>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error filtering Theme & Motifs:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

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

            var confirm = MessageBox.Show($"Are you sure you want to delete '{item.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.From<ThemeMotif>().Where(x => x.Id == item.Id).Delete();

                _allItems.Remove(item);
                ApplySearchFilter();

                MessageBox.Show("Deleted successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting ThemeMotif:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportToPdf()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var response = await client
                    .From<ThemeMotif>()
                    .Select("*, package_id(*)")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var motifs = response.Models;

                if (motifs == null || motifs.Count == 0)
                {
                    MessageBox.Show("No Theme & Motif found to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    motifs,
                    "Theme & Motif List",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to PDF:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportToCsv()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var response = await client
                    .From<ThemeMotif>()
                    .Select("*, package_id(*)")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var motifs = response.Models;

                if (motifs == null || motifs.Count == 0)
                {
                    MessageBox.Show("No Theme & Motif found to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    motifs,
                    "Theme & Motif List",
                    "Id",
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to CSV:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
