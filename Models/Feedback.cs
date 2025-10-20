
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CATERINGMANAGEMENT.Models
{
    [Table("feedbacks")]
    public class Feedback : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("profile_id")]
        public long? ProfileId { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("feedback")]
        public string? FeedbackText { get; set; }

        [Column("category")]
        public string? Category { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("profiles")]
        public Profile? Profile { get; set; }
    }
}
