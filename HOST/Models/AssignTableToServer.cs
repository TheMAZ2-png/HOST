using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOST.Models
{
    public class AssignTableToServer
    {
        [Key]
        [ForeignKey(nameof(Employee))]
        public int EmployeeID { get; set; }

        [StringLength(50)]
        public required string Name { get; set; }

        [StringLength(80)]
        public string? DisplayName { get; set; }

        [StringLength(120)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
