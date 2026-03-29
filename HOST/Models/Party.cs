using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOST.Models
{
    public class Party
    {
        [Key]
        public int PartyId { get; set; }

        [Required]
        [StringLength(80)]
        public string PartyName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public int PartySize { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }

        public string OwnerId { get; set; }

        // Navigation
        public virtual ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
        public virtual ICollection<Seating> Seatings { get; set; } = new List<Seating>();

        // Workflow status
        public string Status { get; set; } = "Waiting";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // NEW: Stored historical values
        public int? ActualWaitMinutes { get; set; }          // How long they actually waited
        public int? EstimatedWaitAtJoin { get; set; }        // Estimate they were given when joining

        // Convenience helpers
        [NotMapped] public bool IsWaiting => Status == "Waiting";
        [NotMapped] public bool IsSeated => Status == "Seated";
        [NotMapped] public bool IsCompleted => Status == "Completed";

        // Current wait time (live)
        [NotMapped]
        public int? CurrentWaitMinutes
        {
            get
            {
                var activeEntry = QueueEntries
                    .Where(q => q.Status == "Waiting")
                    .OrderByDescending(q => q.CreatedAt)
                    .FirstOrDefault();

                if (activeEntry == null)
                    return null;

                return (int)(DateTime.UtcNow - activeEntry.CreatedAt).TotalMinutes;
            }
        }

        // Live estimated wait time (computed in Index.cshtml.cs)
        [NotMapped]
        public int EstimatedWaitMinutes { get; set; }
    }
}
