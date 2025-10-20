using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace CATERINGMANAGEMENT.Models
{
    /// <summary>
    /// Represents a denormalized view combining reservation, client, and assigned workers.
    /// </summary>
    [Table("grouped_schedule_view")]
    public class GroupedScheduleView : BaseModel
    {
        [Column("reservation_id")]
        public long ReservationId { get; set; }

        [Column("receipt_number")]
        public string? ReceiptNumber { get; set; }

        [Column("event_date")]
        public DateTime EventDate { get; set; }

        [Column("location")]
        public string? Location { get; set; }

        [Column("venue")]
        public string? Venue { get; set; }

        [Column("package_name")]
        public string? PackageName { get; set; }

        [Column("client_name")]
        public string? ClientName { get; set; }

        [Column("client_email")]
        public string? ClientEmail { get; set; }

        [Column("client_contact")]
        public string? ClientContact { get; set; }

        [Column("assigned_workers")]
        public string? AssignedWorkers { get; set; }

        [Column("assigned_worker_ids")]
        public string? AssignedWorkerIds { get; set; }   

        [Column("assigned_on")]
        public DateTime AssignedOn { get; set; }
    }
}
