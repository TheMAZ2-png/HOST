using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string Status { get; set; } = "Available";

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Seating> Seatings { get; set; } = new List<Seating>();

        public int? CurrentPartyId { get; set; }
        public Party? CurrentParty { get; set; }
    }
}
