using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace HOST.Models
{
    public class QueueEntry
    {
        [Key]
        public int QueueEntryId { get; set; }

        [Required]
        [ForeignKey(nameof(Party))]
        public int PartyId { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? PublicAccessCode { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        [AllowNull]
        public int? EstimatedWaitMinutes { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [AllowNull]
        [Column(TypeName = "datetime2")]
        public DateTime? UpdatedAt { get; set; }

        [AllowNull]
        [Column(TypeName = "datetime2")]
        public DateTime? SeatedAt { get; set; }

        // Navigation properties
        public virtual Party Party { get; set; }
    }
}