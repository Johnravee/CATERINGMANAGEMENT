using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

namespace CATERINGMANAGEMENT.Models
{
    [Table("reservation_status_counts")]
    public class ReservationStatusCount : BaseModel
    {
        [Column("total_reservations")]
        public int TotalReservations { get; set; }

        [Column("pending")]
        [JsonPropertyName("pending")]
        public int Pending { get; set; }

        [Column("confirmed")]
        public int Confirmed { get; set; }

        [Column("canceled")]
        public int Canceled { get; set; }
    }
}
