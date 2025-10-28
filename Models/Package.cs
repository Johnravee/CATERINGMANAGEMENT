using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace CATERINGMANAGEMENT.Models
{
    [Table("packages")]
    public class Package : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id", ignoreOnInsert: true)]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

       
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
