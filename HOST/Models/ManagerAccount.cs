using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOST.Models
{
    public class ManagerAccount
    {
        [Key]
        public int ManagerId { get; set; }

        [StringLength(120)]
        public string? Email { get; set; }

        [Required]
        [StringLength(200)]
        public string PasswordHash { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }
    }
}
