using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HOST.Models
{
    public class Party
    {
        [Key]
        public int PartyId { get; set; }

        [Required]
        [StringLength(80)]
        public string PartyName { get; set; }

        [AllowNull]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        public int PartySize { get; set; }

        [AllowNull]
        [StringLength(250)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
    }
}