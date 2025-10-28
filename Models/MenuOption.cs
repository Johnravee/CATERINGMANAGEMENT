using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;


namespace CATERINGMANAGEMENT.Models
{
    [Table("menu_options")]
    public class MenuOption : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id", ignoreOnInsert: true)]
        public long Id { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("category")]
        public string? Category { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

    }
}
