using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CATERINGMANAGEMENT.Models
{
    [Table("profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public long Id { get; set; }

        [Column("name")]
        public string FullName { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("contact_number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("auth_id")]
        public Guid AuthId { get; set; }

        [Column("fcm_token")]
        public string? FcmToken { get; set; }

        [Column("is_admin")]
        public bool? IsAdmin { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
