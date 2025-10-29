using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CATERINGMANAGEMENT.Models
{
    /// <summary>
    /// Simple DTO for checklist generation only (not stored in DB).
    /// </summary>
    public class SelectedEquipmentItem
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Notes { get; set; }
    }
}
