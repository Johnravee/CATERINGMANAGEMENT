using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace CATERINGMANAGEMENT.Models
{
    [Table("monthly_reservation_summary")] 
    public class MonthlyReservationSummary : BaseModel
    {
 

        [Column("reservation_year")]
        public int ReservationYear { get; set; }

        [Column("reservation_month")]
        public int ReservationMonth { get; set; }

        [Column("year_month_label")]
        public string YearMonthLabel { get; set; } = string.Empty;

        [Column("total_reservations")]
        public int TotalReservations { get; set; }
    }
}
