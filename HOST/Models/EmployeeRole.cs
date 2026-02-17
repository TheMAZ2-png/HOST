using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOST.Models
{
    public class EmployeeRole
    {
        [Key]
        public int EmployeeRoleId { get; set; }

        [Required]
        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }

        [Required]
        [ForeignKey(nameof(Role))]
        public int RoleId { get; set; }

        // Navigation properties
        public virtual Employee Employee { get; set; }
        public virtual Role Role { get; set; }
    }
}