using System.ComponentModel.DataAnnotations;

namespace HOST.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(30)]
        public string RoleName { get; set; }

        // Navigation properties
        public virtual ICollection<EmployeeRole> EmployeeRoles { get; set; } = new List<EmployeeRole>();
    }
}