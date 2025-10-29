using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.ReservationVM
{
    public class ChecklistBuilderViewModel : BaseViewModel
    {
        private readonly ReservationChecklistService _checklistService = new();
        private readonly SchedulingService _schedulingService = new();

        public ObservableCollection<Reservation> CompletedReservations { get; } = new();
        private Reservation? _selectedReservation;
        public Reservation? SelectedReservation
        {
            get => _selectedReservation;
            set { _selectedReservation = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SelectedEquipmentItem> SelectedItems { get; } = new();
        private List<string> _equipmentNames = new();
        public List<string> EquipmentNames { get => _equipmentNames; set { _equipmentNames = value; OnPropertyChanged(); } }

        private string? _designImagePath;
        public string? DesignImagePath { get => _designImagePath; set { _designImagePath = value; OnPropertyChanged(); } }

        public ICommand AddRowCommand { get; }
        public ICommand RemoveRowCommand { get; }
        public ICommand BrowseImageCommand { get; }
        public ICommand GenerateCommand { get; }

        public ChecklistBuilderViewModel()
        {
            AddRowCommand = new RelayCommand(() => SelectedItems.Add(new SelectedEquipmentItem { Quantity = 1 }));
            RemoveRowCommand = new RelayCommand<SelectedEquipmentItem>(item => { if (item != null) SelectedItems.Remove(item); });
            BrowseImageCommand = new RelayCommand(BrowseDesignImage);
            GenerateCommand = new RelayCommand(async () => await GenerateAsync(), () => SelectedReservation != null && SelectedItems.Count > 0);

            _ = LoadEquipmentNames();
            _ = LoadCompletedReservations();
        }

        private async Task LoadEquipmentNames()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var list = (await client.From<Equipment>()
                                .Select("item_name")
                                .Order(e => e.ItemName!, Supabase.Postgrest.Constants.Ordering.Ascending)
                                .Get()).Models ?? new List<Equipment>();
                EquipmentNames = list.Where(e => !string.IsNullOrWhiteSpace(e.ItemName))
                                     .Select(e => e.ItemName!)
                                     .Distinct()
                                     .OrderBy(x => x)
                                     .ToList();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed loading equipment names");
            }
        }

        private async Task LoadCompletedReservations()
        {
            try
            {
                var list = await _schedulingService.GetCompletedReservationsAsync();
                CompletedReservations.Clear();
                foreach (var r in list)
                    CompletedReservations.Add(r);

                // pre-select first if available
                if (CompletedReservations.Count > 0)
                    SelectedReservation = CompletedReservations[0];
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed loading completed reservations");
            }
        }

        private void BrowseDesignImage()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" };
            if (dlg.ShowDialog() == true)
            {
                DesignImagePath = dlg.FileName;
            }
        }

        private async Task GenerateAsync()
        {
            if (SelectedReservation == null) return;
            try
            {
                await _checklistService.GenerateChecklistPdfAsync(SelectedReservation.Id, SelectedItems, DesignImagePath);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Checklist generation failed");
            }
        }
    }
}
