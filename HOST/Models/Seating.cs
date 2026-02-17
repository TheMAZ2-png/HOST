using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HOST.Models
{
    public class Seating
    {
        [Key]
        public int SeatingId { get; set; }

        [ForeignKey(nameof(AssignedServer))]
        public int AssignedServerId { get; set; }
        public Employee AssignedServer { get; set; }

        [ForeignKey(nameof(SeatedByEmployee))]
        public int SeatedByEmployeeId { get; set; }
        public Employee SeatedByEmployee { get; set; }

        public int RestaurantTableId { get; set; }
        public RestaurantTable RestaurantTable { get; set; }

        public int PartyId { get; set; }
        public Party Party { get; set; }

        public DateTime SeatedAt { get; set; }
        public DateTime? ClearedAt { get; set; }
    }
}