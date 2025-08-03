using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using Supabase;
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

        public ObservableCollection<Reservation> Reservations { get; } = new();

        public async Task LoadReservations()
        {
            var client = await SupabaseService.GetClientAsync();
            var result = await client.From<Reservation>().Get();

            Reservations.Clear();
            foreach (var reservation in result.Models)
            {
                Reservations.Add(reservation);
            }

            OnPropertyChanged(nameof(Reservations));
        }
    }
}
