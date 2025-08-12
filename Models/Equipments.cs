using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;


namespace CATERINGMANAGEMENT.Models
{
    [Table("equipments")]
    public class Equipments : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("item_name")]
        public string? ItemName { get; set; }

        [Column("quantity")]
        public decimal? Quantity { get; set; }

        [Column("condition")]
        public string? Condition { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
