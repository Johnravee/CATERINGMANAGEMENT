using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

namespace CATERINGMANAGEMENT.Models
{
    [Table("scheduling")]
    public class Scheduling : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("reservation_id")]
        public long ReservationId { get; set; }

        [Column("worker_id")]
        public long WorkerId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Relations from Supabase (JOIN)
        [JsonPropertyName("reservations")]
        public Reservation? Reservations { get; set; }

        [JsonPropertyName("workers")]
        public Worker? Workers { get; set; }

        [JsonPropertyName("profiles")]
        [JsonIgnore]
        public Profile? Profile { get; set; }

        [JsonPropertyName("package")]
        [JsonIgnore]
        public Package? Package { get; set; }

        [JsonPropertyName("grazing")]
        [JsonIgnore]
        public GrazingTable? Grazing { get; set; }
    } 
}
