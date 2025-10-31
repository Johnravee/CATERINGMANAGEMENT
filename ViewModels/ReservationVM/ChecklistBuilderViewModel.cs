using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.Services.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.ReservationVM
{
    public class ChecklistBuilderViewModel : BaseViewModel
    {
        private readonly ReservationChecklistService _checklistService = new();
        private readonly SchedulingService _scheduling_service = new();

        public ObservableCollection<Reservation> CompletedReservations { get; } = new();
        private Reservation? _selectedReservation;
        public Reservation? SelectedReservation
        {
            get => _selectedReservation;
            set { _selectedReservation = value; OnPropertyChanged(); UpdateDefaultCallTime(); }
        }

        public ObservableCollection<SelectedEquipmentItem> SelectedItems { get; } = new();
        private List<string> _equipmentNames = new();
        public List<string> EquipmentNames { get => _equipmentNames; set { _equipmentNames = value; OnPropertyChanged(); } }

        private string? _designImagePath;
        public string? DesignImagePath { get => _designImagePath; set { _designImagePath = value; OnPropertyChanged(); } }

        // Structured time picker properties
        public List<int> Hours { get; } = Enumerable.Range(1, 12).ToList();
        public List<int> Minutes { get; } = Enumerable.Range(0, 59).ToList();
        public List<string> Periods { get; } = new() { "AM", "PM" };

        private int _callHour = 8;
        public int CallHour { get => _callHour; set { _callHour = value; OnPropertyChanged(); OnPropertyChanged(nameof(CallTimeString)); } }

        private int _callMinute = 0;
        public int CallMinute { get => _callMinute; set { _callMinute = value; OnPropertyChanged(); OnPropertyChanged(nameof(CallTimeString)); } }

        private string _callPeriod = "AM";
        public string CallPeriod { get => _callPeriod; set { _callPeriod = value; OnPropertyChanged(); OnPropertyChanged(nameof(CallTimeString)); } }

        // Convenience formatted time string (not enforced) kept for defaults
        public string CallTimeString => string.Format("{0:D2}:{1:D2} {2}", CallHour, CallMinute, CallPeriod);

        // Free-form call time entered by user (no parsing/validation)
        private string _callTime = string.Empty;
        public string CallTime
        {
            get => _callTime;
            set { _callTime = value ?? string.Empty; OnPropertyChanged(); }
        }

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

            // initialize call time with default formatted suggestion
            CallTime = CallTimeString;

            _ = LoadEquipmentNames();
            _ = LoadCompletedReservations();
        }

        private void UpdateDefaultCallTime()
        {
            if (SelectedReservation != null)
            {
                try
                {
                    if (SelectedReservation.EventTime != default)
                    {
                        var dt = DateTime.Today + SelectedReservation.EventTime;
                        var suggested = dt.AddHours(-1);
                        int hour12 = suggested.Hour % 12;
                        if (hour12 == 0) hour12 = 12;
                        CallHour = hour12;
                        CallMinute = suggested.Minute;
                        CallPeriod = suggested.Hour >= 12 ? "PM" : "AM";

                        // set free-form call time to suggested formatted value
                        CallTime = CallTimeString;
                    }
                }
                catch { }
            }
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
                var list = await _scheduling_service.GetCompletedReservationsAsync();
                CompletedReservations.Clear();
                foreach (var r in list)
                    CompletedReservations.Add(r);

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
                // pass free-form call time to PDF generator
                await _checklistService.GenerateChecklistPdfAsync(SelectedReservation.Id, SelectedItems, DesignImagePath, CallTime);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Checklist generation failed");
            }
        }
    }
}
