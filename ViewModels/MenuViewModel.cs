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
    public class MenuViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<MenuOption> _allItems = new();
        private ObservableCollection<MenuOption> _filteredItems = new();

        private const int PageSize = 10;

        public ObservableCollection<MenuOption> Items
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

        public ICommand AddMenuCommand { get; set; }
        public ICommand EditMenuCommand { get; set; }
        public ICommand DeleteMenuCommand { get; set; }
        public ICommand NextPageCommand { get; set; }
        public ICommand PrevPageCommand { get; set; }
        public ICommand ExportPdfCommand { get; set; }
        public ICommand ExportCsvCommand { get; set; }

        public MenuViewModel()
        {
            AddMenuCommand = new RelayCommand(async () => await InsertMenuItem());
            EditMenuCommand = new RelayCommand<MenuOption>(async (m) => await EditMenu(m));
            DeleteMenuCommand = new RelayCommand<MenuOption>(async (m) => await DeleteMenu(m));
            NextPageCommand = new RelayCommand(async () => await NextPage());
            PrevPageCommand = new RelayCommand(async () => await PrevPage());

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
                MessageBox.Show($"Error loading menu options:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    .From<MenuOption>()
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
                    .From<MenuOption>()
                    .Count(CountType.Exact);

                TotalCount = countResult;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

                ApplySearchFilter();
                CurrentPage = page;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading menu page:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Items = new ObservableCollection<MenuOption>(_allItems);
            }
            else
            {
                try
                {
                    IsLoading = true;
                    var client = await SupabaseService.GetClientAsync();

                    var response = await client
                        .From<MenuOption>()
                        .Filter(x => x.Name, Operator.ILike, $"%{query}%")
                        .Order(x => x.CreatedAt, Ordering.Descending)
                        .Get();

                    if (response.Models != null)
                        Items = new ObservableCollection<MenuOption>(response.Models);
                    else
                        Items = new ObservableCollection<MenuOption>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error filtering menu options:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task InsertMenuItem()
        {
            var addWindow = new AddMenu(); // kailangan mo gawin yung AddMenu.xaml
            bool? result = addWindow.ShowDialog();

            if (result == true)
                await LoadItems();
        }

        private async Task EditMenu(MenuOption item)
        {
            if (item == null) return;

            var editWindow = new EditMenu(item); 
            bool? result = editWindow.ShowDialog();

            if (result == true)
                await LoadItems();
        }

        private async Task DeleteMenu(MenuOption item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete '{item.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.From<MenuOption>().Where(x => x.Id == item.Id).Delete();

                _allItems.Remove(item);
                ApplySearchFilter();

                MessageBox.Show("Deleted successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting menu item:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportToPdf()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var response = await client
                    .From<MenuOption>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var menus = response.Models;

                if (menus == null || menus.Count == 0)
                {
                    MessageBox.Show("No menus found to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    menus,
                    "Menu Options",
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
                    .From<MenuOption>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                var menus = response.Models;

                if (menus == null || menus.Count == 0)
                {
                    MessageBox.Show("No menus found to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    menus,
                    "Menu Options",
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

