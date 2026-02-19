using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace HOST.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(80)]
        public string? DisplayName { get; set; }

        [StringLength(120)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [AllowNull]
        [Column(TypeName = "datetime2")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Seating> SeatedByEntries { get; set; } = new List<Seating>();
        public virtual ICollection<Seating> AssignedServerEntries { get; set; } = new List<Seating>();
    }
}