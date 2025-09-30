using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace CATERINGMANAGEMENT.Models
{
    [Table("thememotif")]
    public class ThemeMotif : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

      
        [Column("package_id")]
        public long? PackageId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

       
        [JsonProperty("packages")]
        public Package? Package { get; set; }
    }
}

