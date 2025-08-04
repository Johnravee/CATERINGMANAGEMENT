using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using Supabase;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.ViewModel
{
    public class ReservationListViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ✅ Observable list of reservations
        public ObservableCollection<Reservation> Reservations { get; } = new();

        // ✅ Add this property below the Reservations collection
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

        // ✅ Modified async method using loading state
        public async Task LoadReservations()
        {
            IsLoading = true;
            try
            {
                var client = await SupabaseService.GetClientAsync();
                var result = await client.From<Reservation>().Get();

                Reservations.Clear();
                foreach (var reservation in result.Models)
                {
                    Reservations.Add(reservation);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading reservations: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                IsLoading = false;
            }

            OnPropertyChanged(nameof(Reservations));
        }
    }
}
