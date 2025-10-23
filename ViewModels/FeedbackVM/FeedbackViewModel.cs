using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels.FeedbackVM
{
    public class FeedbackViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Feedback> _allItems = new();
        private ObservableCollection<Feedback> _filteredItems = new();

        private const int PageSize = 10;

        public ObservableCollection<Feedback> Items
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

        public ICommand DeleteFeedbackCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }


        public FeedbackViewModel()
        {
            DeleteFeedbackCommand = new RelayCommand<Feedback>(async (item) => await DeleteFeedback(item));
            NextPageCommand = new RelayCommand(async () => await NextPage());
            PrevPageCommand = new RelayCommand(async () => await PrevPage());


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
                MessageBox.Show($"Error loading feedbacks:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    .From<Feedback>()
                    .Select("*, profiles(*)")
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
                    .From<Feedback>()
                    .Count(CountType.Exact);

                TotalCount = countResult;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

                ApplySearchFilter();
                CurrentPage = page;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading feedback page:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Items = new ObservableCollection<Feedback>(_allItems);
            }
            else
            {
                try
                {
                    IsLoading = true;

                    var client = await SupabaseService.GetClientAsync();
                    var response = await client
                        .From<Feedback>()
                        .Select("*")
                        .Filter(x => x.Name, Operator.ILike, $"%{query}%")
                        .Order(x => x.CreatedAt, Ordering.Descending)
                        .Get();

                    if (response.Models != null)
                        Items = new ObservableCollection<Feedback>(response.Models);
                    else
                        Items = new ObservableCollection<Feedback>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error filtering feedbacks:\n{ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task DeleteFeedback(Feedback item)
        {
            if (item == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete feedback from '{item.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                var client = await SupabaseService.GetClientAsync();
                await client.From<Feedback>().Where(x => x.Id == item.Id).Delete();

                _allItems.Remove(item);
                await LoadPage(CurrentPage);
                ApplySearchFilter();

                MessageBox.Show("Deleted successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting feedback:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
