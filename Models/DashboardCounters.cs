using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CATERINGMANAGEMENT.Models
{
    [Table("dashboard_counters")]
    public class DashboardCounters : BaseModel
    {
        [Column("damaged_equipment")]
        public int DamagedEquipment { get; set; }

        [Column("kitchen_low_stock")]
        public int KitchenLowStock { get; set; }

        [Column("active_workers")]
        public int ActiveWorkers { get; set; }

        [Column("pending_reservations")]
        public int PendingReservations { get; set; }

        [Column("total_equipments")]
        public int TotalEquipments { get; set; }

        [Column("total_kitchen_items")]
        public int TotalKitchenItems { get; set; }

        [Column("total_reservations")]
        public int TotalReservations { get; set; }

        [Column("total_workers")]
        public int TotalWorkers { get; set; }
    }
}
