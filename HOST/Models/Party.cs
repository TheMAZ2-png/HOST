using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public int PartySize { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }

        public string OwnerId { get; set; }

        public virtual ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
        public virtual ICollection<Seating> Seatings { get; set; } = new List<Seating>();

        public string Status { get; set; } = "Waiting";


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


    }

}