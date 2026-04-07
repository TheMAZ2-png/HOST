using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOST.Models
{
    public class ManagerAccount
    {
        [Key]
        public int ManagerId { get; set; }

        [Required]
        [StringLength(120)]
        public string Email { get; set; }

        // No password stored here — Identity handles that

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }
    }
}
