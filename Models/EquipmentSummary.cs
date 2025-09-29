using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CATERINGMANAGEMENT.Models
{
    [Table("equipment_summary")]
    public class EquipmentSummary : BaseModel
    {
        [Column("total_count")]
        public int TotalCount { get; set; }

        [Column("good_count")]
        public int GoodCount { get; set; }

        [Column("damaged_count")]
        public int DamagedCount { get; set; }
    }
}
