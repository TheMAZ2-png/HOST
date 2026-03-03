using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOST.Models
{
    public class FailedLogin
    {
        [Key]
        public int Id { get; set; }

        public string? Username { get; set; }

        [Required]
        [Column(TypeName = "datetime2(7)")]
        public DateTime Timestamp { get; set; }

        public string? IpAddress { get; set; }
    }
}
