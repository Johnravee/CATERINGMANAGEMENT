using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels.PackageVM
{
    public class PackageViewModel : INotifyPropertyChanged
    {
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

        // Commands
        public ICommand AddPackageCommand { get; set; }
        public ICommand EditPackageCommand { get; set; }
        public ICommand DeletePackageCommand { get; set; }
        public ICommand NextPageCommand { get; set; }
        public ICommand PrevPageCommand { get; set; }
        public ICommand ImportCommand { get; set; }
        public ICommand ExportPdfCommand { get; set; }
        public ICommand ExportCsvCommand { get; set; }

        public PackageViewModel()
        {
            AddPackageCommand = new RelayCommand(async () => await InsertPackageItem());
            EditPackageCommand = new RelayCommand<Package>(async (p) => await EditPackage(p));
            DeletePackageCommand = new RelayCommand<Package>(async (p) => await DeletePackage(p));
            NextPageCommand = new RelayCommand(async () => await NextPage());
            PrevPageCommand = new RelayCommand(async () => await PrevPage());
            ImportCommand = new RelayCommand(async () => await Import());

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
                MessageBox.Show($"Error loading packages:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    .From<Package>()
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
                    .From<Package>()
                    .Count(CountType.Exact);

                TotalCount = countResult;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

                ApplySearchFilter();
                CurrentPage = page;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading package page:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Items = new ObservableCollection<Package>(_allItems);
            }
            else
            {
                try
                {
                    IsLoading = true;

                    var client = await SupabaseService.GetClientAsync();
                    var response = await client
                        .From<Package>()
                        .Filter(x => x.Name, Operator.ILike, $"%{query}%")
                        .Order(x => x.CreatedAt, Ordering.Descending)
                        .Get();

                    if (response.Models != null)
                        Items = new ObservableCollection<Package>(response.Models);
                    else
                        Items = new ObservableCollection<Package>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error filtering packages:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task InsertPackageItem()
        {
            var addWindow = new AddPackage();
            addWindow.DataContext = new AddPackageViewModel(); // Ensure a new instance is created
            bool? result = addWindow.ShowDialog();

            if (result == true)
                await LoadItems();
        }

        private async Task EditPackage(Package item)
        {
            if (item == null) return;

            var editWindow = new EditPackage(item);
            bool? result = editWindow.ShowDialog();

            if (result == true)
                await LoadItems();
        }

        private async Task DeletePackage(Package item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete '{item.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.From<Package>().Where(x => x.Id == item.Id).Delete();

                _allItems.Remove(item);
                ApplySearchFilter();

                MessageBox.Show("Deleted successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting package item:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportToPdf()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var response = await client
                    .From<Package>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var package = response.Models;

                if (package == null || package.Count == 0)
                {
                    MessageBox.Show("No package found to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    package,
                    "Packages",
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
                    .From<Package>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var package = response.Models;

                if (package == null || package.Count == 0)
                {
                    MessageBox.Show("No package found to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    package,
                    "Packages",
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

        private async Task Import()
        {
            MessageBox.Show("Import feature not implemented yet.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            await Task.CompletedTask;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
