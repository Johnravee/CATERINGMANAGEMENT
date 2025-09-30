using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CATERINGMANAGEMENT.DocumentsGenerator;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class WorkerViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Worker> _workerItems = new(); // master list
        private ObservableCollection<Worker> _filteredWorkerItems = new(); // filtered list

        private const int PageSize = 20;

        public ObservableCollection<Worker> Items
        {
            get => _filteredWorkerItems;
            set { _filteredWorkerItems = value; OnPropertyChanged(); }
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

        public int TotalCount { get; set; }

        // Commands
        public ICommand DeleteWorkerCommand { get; set; }
        public ICommand EditWorkerCommand { get; set; }
        public ICommand AddWorkerCommand { get; set; }
        public ICommand NextPageCommand { get; set; }
        public ICommand PrevPageCommand { get; set; }
        public ICommand ExportPdfCommand { get; set; }
        public ICommand ExportCsvCommand { get; set; }


        public WorkerViewModel()
        {
            DeleteWorkerCommand = new RelayCommand<Worker>(async (w) => await DeleteWorker(w));
            EditWorkerCommand = new RelayCommand<Worker>(async (w) => await EditWorker(w));
            AddWorkerCommand = new RelayCommand(() => AddNewWorker());

            NextPageCommand = new RelayCommand(async () => await NextPage(), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await PrevPage(), () => CurrentPage > 1);

            ExportPdfCommand = new RelayCommand(ExportToPdf);
            ExportCsvCommand = new RelayCommand(ExportToCsv);

        }

        // Load first page
        public async Task LoadItems()
        {
            IsLoading = true;
            try
            {
                _workerItems.Clear();
                Items.Clear();

                await LoadPage(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading workers:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Load specific page
        public async Task LoadPage(int pageNumber)
        {
            IsLoading = true;
            try
            {
                var client = await SupabaseService.GetClientAsync();

                int from = (pageNumber - 1) * PageSize;
                int to = from + PageSize - 1;

                var response = await client
                    .From<Worker>()
                    .Range(from, to)
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                _workerItems.Clear();
                if (response.Models != null)
                {
                    foreach (var item in response.Models)
                        _workerItems.Add(item);
                }

                TotalCount = await client
                    .From<Worker>()
                    .Select("id")
                    .Count(CountType.Exact);

                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

                ApplySearchFilter();
                CurrentPage = pageNumber;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading workers:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        //Search query
        private async void ApplySearchFilter()
        {
            var query = _searchText?.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(query))
            {
                Items = new ObservableCollection<Worker>(_workerItems);
            }
            else
            {
                try
                {
                    IsLoading = true;
                    var client = await SupabaseService.GetClientAsync();

                    var response = await client
                        .From<Worker>()
                        .Filter(x => x.Name, Operator.ILike, $"%{query}%")
                        .Get();

                    if (response.Models != null)
                        Items = new ObservableCollection<Worker>(response.Models);
                    else
                        Items = new ObservableCollection<Worker>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error searching equipment:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        // Delete Worker
        private async Task DeleteWorker(Worker worker)
        {
            if (worker == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete {worker.Name}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.From<Worker>().Where(w => w.Id == worker.Id).Delete();

                _workerItems.Remove(worker);
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting worker:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Edit Worker
        private async Task EditWorker(Worker worker)
        {
            if (worker == null) return;

            var editWindow = new EditWorker(worker);
            bool? result = editWindow.ShowDialog();

            if (result == true && editWindow.Worker != null)
            {
                try
                {
                    var client = await SupabaseService.GetClientAsync();
                    var response = await client.From<Worker>()
                        .Where(w => w.Id == editWindow.Worker.Id)
                        .Update(editWindow.Worker);

                    if (response.Models != null && response.Models.Count > 0)
                    {
                        var index = _workerItems.IndexOf(worker);
                        if (index >= 0)
                            _workerItems[index] = response.Models[0];

                        ApplySearchFilter();
                        MessageBox.Show("Worker updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating worker:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Add Worker
        private void AddNewWorker()
        {
            var addWindow = new AddWorker();
            bool? result = addWindow.ShowDialog();

            if (result == true && addWindow.NewWorker != null)
            {
                InsertWorker(addWindow.NewWorker);
            }
        }

        private async void InsertWorker(Worker worker)
        {
            if (worker == null) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<Worker>().Insert(worker);

                if (response.Models != null && response.Models.Count > 0)
                {
                    _workerItems.Add(response.Models[0]);
                    ApplySearchFilter();
                    MessageBox.Show("Worker added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding worker:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task ExportToPdf()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();


                var response = await client
                    .From<Worker>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();
                var workers = response.Models;

                if (workers == null || workers.Count == 0)
                {
                    MessageBox.Show("No workers found to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DataGridToPdf.DataGridToPDF(
                    workers,
                    "Workers",
                    "Id",               
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt",
                    "HireDate"
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
                    .From<Worker>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();
                var workers = response.Models;

                if (workers == null || workers.Count == 0)
                {
                    MessageBox.Show("No workers found to export.", "Export Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DatagridToCsv.ExportToCsv(
                    workers,
                    "Workers",
                    "Id",           
                    "BaseUrl",
                    "RequestClientOptions",
                    "TableName",
                    "PrimaryKey",
                    "CreatedAt",
                    "HireDate"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to PDF:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
