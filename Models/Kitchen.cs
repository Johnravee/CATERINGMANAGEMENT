using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace CATERINGMANAGEMENT.Models
{
    [Table("kitchen")]
    public class Kitchen : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("item_name")]
        public string? ItemName { get; set; }

        [Column("unit")]
        public string? Unit { get; set; }

        [Column("quantity")]
        public decimal Quantity { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
