using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace HOST.Models
{
    public class RestaurantTable
    {
        [Key]
        public int TableId { get; set; }

        [Required]
        public int TableNumber { get; set; }

        [Required]
        public int SeatCapacity { get; set; }

        [Required]
        [StringLength(50)]
        public string Section { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Seating> Seatings { get; set; } = new List<Seating>();
    }
}