using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using LiveChartsCore;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class EquipmentsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Equipments> _equipments = new();
        private ObservableCollection<Equipments> _filteredEquipments = new();

        public ObservableCollection<Equipments> Equipments
        {
            get => _filteredEquipments;
            set { _filteredEquipments = value; OnPropertyChanged(); }
        }

        public int TotalCount { get; set; }
        public int GoodConditionCount { get; set; }
        public int NeedsRepairCount { get; set; }

        public ObservableCollection<ISeries> TotalItemsSeries { get; set; } = new();
        public ObservableCollection<ISeries> GoodConditionSeries { get; set; } = new();
        public ObservableCollection<ISeries> NeedsRepairSeries { get; set; } = new();

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

        // Commands
        public ICommand DeleteEquipmentCommand { get; set; }
        public ICommand EditEquipmentCommand { get; set; }
        public ICommand AddEquipmentCommand { get; set; }

        public EquipmentsViewModel()
        {
            DeleteEquipmentCommand = new RelayCommand<Equipments>(async (e) => await DeleteEquipment(e));
            AddEquipmentCommand = new RelayCommand(() => AddNewEquipment());
            EditEquipmentCommand = new RelayCommand<Equipments>(async (eq) => await EditEquipment(eq));
        }
        

        // Load from Supabase
        public async Task LoadEquipments()
        {
            IsLoading = true;
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client
                    .From<Equipments>()
                    .Order(e => e.Id, Ordering.Ascending)
                    .Get();

                if (response.Models != null && response.Models.Count > 0)
                {
                    _equipments = new ObservableCollection<Equipments>(response.Models);
                    ApplySearchFilter();
                    UpdateCounts();
                }
                else
                {
                    _equipments.Clear();
                    Equipments.Clear();
                    TotalCount = GoodConditionCount = NeedsRepairCount = 0;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading equipments:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Filter logic
        private void ApplySearchFilter()
        {
            var query = _searchText?.Trim().ToLower() ?? "";
            Equipments = string.IsNullOrWhiteSpace(query)
                ? new ObservableCollection<Equipments>(_equipments)
                : new ObservableCollection<Equipments>(_equipments.Where(e =>
                    (!string.IsNullOrEmpty(e.ItemName) && e.ItemName.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(e.Condition) && e.Condition.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(e.Notes) && e.Notes.ToLower().Contains(query))
                ));
        }

        // Update pie chart counts
        private void UpdateCounts()
        {
            TotalCount = _equipments.Count;
            GoodConditionCount = _equipments.Count(e => e.Condition == "Good");
            NeedsRepairCount = _equipments.Count(e => e.Condition == "Needs Repair");

            TotalItemsSeries.Clear();
            TotalItemsSeries.Add(new PieSeries<int> { Values = new int[] { TotalCount }, Fill = new SolidColorPaint(SKColors.MediumPurple), InnerRadius = 15 });

            GoodConditionSeries.Clear();
            GoodConditionSeries.Add(new PieSeries<int> { Values = new int[] { GoodConditionCount }, Fill = new SolidColorPaint(SKColors.Green), InnerRadius = 15 });

            NeedsRepairSeries.Clear();
            NeedsRepairSeries.Add(new PieSeries<int> { Values = new int[] { NeedsRepairCount }, Fill = new SolidColorPaint(SKColors.Red), InnerRadius = 15 });

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(GoodConditionCount));
            OnPropertyChanged(nameof(NeedsRepairCount));
        }

        // Delete equipment with confirmation
        private async Task DeleteEquipment(Equipments equipment)
        {
            if (equipment == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete {equipment.ItemName}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                await client.From<Equipments>().Where(e => e.Id == equipment.Id).Delete();

                _equipments.Remove(equipment);
                ApplySearchFilter();
                UpdateCounts();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error deleting equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Edit/View equipment
        private async Task EditEquipment(Equipments equipment)
        {
            if (equipment == null) return;

            var editWindow = new EditEquipments(equipment);
            bool? result = editWindow.ShowDialog();

            if (result == true && editWindow.Equipments != null)
            {
                try
                {
                    editWindow.Equipments.UpdatedAt = DateTime.UtcNow;

                    var client = await SupabaseService.GetClientAsync();
                    var response = await client.From<Equipments>()
                        .Where(e => e.Id == editWindow.Equipments.Id)
                        .Update(editWindow.Equipments);

                    if (response.Models != null && response.Models.Count > 0)
                    {
                        // Update local collection
                        var index = _equipments.IndexOf(equipment);
                        if (index >= 0)
                            _equipments[index] = response.Models[0];

                        ApplySearchFilter();
                        UpdateCounts(); 
                        MessageBox.Show("Equipment updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error updating equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Insert new equipment
        private void AddNewEquipment()
        {
            // Open the Add Item window
            var addWindow = new EquipmentItemAdd();
         

            // Show as dialog
            bool? result = addWindow.ShowDialog();

            if (result == true && addWindow.NewEquipment != null)
            {
               
                InsertEquipment(addWindow.NewEquipment);
            }
        }

        private async void InsertEquipment(Equipments equipment)
        {
            if (equipment == null) return;

            try
            {
                var client = await SupabaseService.GetClientAsync();
                var response = await client.From<Equipments>().Insert(equipment);

                if (response.Models != null && response.Models.Count > 0)
                {
                    _equipments.Add(response.Models[0]);
                    ApplySearchFilter();
                    UpdateCounts();
                    MessageBox.Show("Equipment added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error adding equipment:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
