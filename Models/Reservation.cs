using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CATERINGMANAGEMENT.Models
{
    [Table("reservations")]
    public class Reservation : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public long Id { get; set; }

        [Column("receipt_number")]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Column("celebrant")]
        public string Celebrant { get; set; } = string.Empty;

        [Column("venue")]
        public string Venue { get; set; } = string.Empty;

        [Column("event_date")]
        public DateTime EventDate { get; set; }

        [Column("event_time")]
        public TimeSpan EventTime { get; set; }

        [Column("location")]
        public string Location { get; set; } = string.Empty;

        [Column("adults_qty")]
        public decimal AdultsQty { get; set; }

        [Column("kids_qty")]
        public decimal KidsQty { get; set; }

        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("profile_id")]
        public long ProfileId { get; set; }

        [Column("theme_motif_id")]
        public long ThemeMotifId { get; set; }

        [Column("package_id")]
        public long PackageId { get; set; }

        [Column("grazing_id")]
        public long GrazingId { get; set; }

        // --- Related Tables
        [JsonPropertyName("profile")]
        public Profile? Profile { get; set; }

        [JsonPropertyName("thememotif")]
        public ThemeMotif? ThemeMotif { get; set; }

        [JsonPropertyName("grazing")]
        public GrazingTable? Grazing { get; set; }

        [JsonPropertyName("package")]
        public Package? Package { get; set; }

        [JsonPropertyName("menu_orders")]
        public List<ReservationMenuOrder>? MenuOrders { get; set; }
    }
}
