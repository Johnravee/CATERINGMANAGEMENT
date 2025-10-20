using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json;


namespace CATERINGMANAGEMENT.Models
{
    [Table("payroll")]
    public class Payroll : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("worker_id")]
        public long? WorkerId { get; set; }

        [JsonProperty("workers")]
        public Worker? Worker { get; set; }

        public long? ReservationId { get; set; }

        [JsonProperty("reservations")]
        public Reservation? Reservation { get; set; }

        [Column("gross_pay")]
        public decimal? GrossPay { get; set; }

        [Column("paid_status")]
        public string? PaidStatus { get; set; }

        [Column("paid_date")]
        public DateTime? PaidDate { get; set; } 

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
