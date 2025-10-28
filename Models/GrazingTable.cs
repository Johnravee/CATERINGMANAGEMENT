using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace CATERINGMANAGEMENT.Models
{
    [Table("grazing")]
    public class GrazingTable : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id", ignoreOnInsert: true)]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
