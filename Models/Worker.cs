
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;


namespace CATERINGMANAGEMENT.Models
{
    [Table("workers")] 
    public class Worker : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("role")]
        public string? Role { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("contact")]
        public string? Contact { get; set; }

        [Column("salary")]
        public long? Salary { get; set; }

        [Column("hire_date")]
        public DateTime? HireDate { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

    }
}
