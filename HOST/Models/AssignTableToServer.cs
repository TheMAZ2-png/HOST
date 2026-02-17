using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace HOST.Models
{
    public class AssignTableToServer
    {
        [Key]
        public int EmployeeID { get; set; }

        [StringLength(50)]  
        public required string Name { get; set; }

        [AllowNull]
        [StringLength(80)]
        public string DisplayName { get; set; }

        [AllowNull]
        [StringLength(120)]
        public string Email { get; set; }

        [AllowNull]
        [StringLength(30)]
        public string Phone { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [AllowNull]
        public DateTime UpdatedAt { get; set; }


    }
}
