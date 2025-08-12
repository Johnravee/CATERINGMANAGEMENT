using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using static Supabase.Postgrest.Constants;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class EquipmentsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Equipments> _equipments = new();
        public ObservableCollection<Equipments> Equipments
        {
            get => _equipments;
            set
            {
                _equipments = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadEquipments()
        {
            try
            {
                IsLoading = true;
                var client = await SupabaseService.GetClientAsync();

                var response = await client
                    .From<Equipments>()
                    .Order(e => e.Id, Ordering.Ascending)
                    .Get();

                Console.WriteLine($"📦 Raw response count: {response.Models?.Count}");

                if (response.Models != null && response.Models.Count > 0)
                {
                    Equipments = new ObservableCollection<Equipments>(response.Models);
                    Console.WriteLine("✅ Equipments loaded:");
                    IsLoading = false;

                }
                else
                {
                    Console.WriteLine("⚠ No equipment found. (Check Supabase table data)");
                    Equipments.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading equipments: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
