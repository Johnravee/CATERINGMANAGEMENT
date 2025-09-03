using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CATERINGMANAGEMENT.Models
{
    [Table("scheduling")]
    public class NewScheduling : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("reservation_id")]
        public long ReservationId { get; set; }

        [Column("worker_id")]
        public long WorkerId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
