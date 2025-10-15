using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

namespace CATERINGMANAGEMENT.Models
{
    [Table("reservation_menu_orders")]
    public class ReservationMenuOrder : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public long Id { get; set; }

        [Column("reservation_id")]
        public long ReservationId { get; set; }

        [Column("menu_option_id")]
        public long MenuOptionId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("menu_options")]
        public MenuOption? menu_options { get; set; }
    }
}
