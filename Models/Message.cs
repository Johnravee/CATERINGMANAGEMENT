using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace CATERINGMANAGEMENT.Models
{
    [Table("messages")]
    public class Message : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public long Id { get; set; }

        [Column("sender_id")]
        public long? SenderId { get; set; }

        [Column("receiver_id")]
        public long? ReceiverId { get; set; }

        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public Profile? Sender { get; set; }
        public Profile? Receiver { get; set; }

        // ✅ Add this for sender header control
        public bool ShowSenderHeader { get; set; }
    }
}
