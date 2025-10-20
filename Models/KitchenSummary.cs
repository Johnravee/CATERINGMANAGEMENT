using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace CATERINGMANAGEMENT.Models
{
    [Table("kitchen_summary")]
    public class KitchenSummary : BaseModel
    {
        [Column("total_count")]
        public int TotalCount { get; set; }

        [Column("normal_stock_count")]
        public int NormalCount { get; set; }

        [Column("low_stock_count")]
        public int LowCount { get; set; }
    }
}
