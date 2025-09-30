using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CATERINGMANAGEMENT.Models
{
    [Table("thememotif")]
    public class NewThemeMotif : BaseModel
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
    }


}
