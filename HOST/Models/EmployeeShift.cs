using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace HOST.Models
{
    public class EmployeeShift
    {
        [Key]
        public int ShiftId { get; set; }

        [Required]
        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime ClockInAt { get; set; }

        [AllowNull]
        [Column(TypeName = "datetime2")]
        public DateTime? ClockOutAt { get; set; }

        [AllowNull]
        [ForeignKey(nameof(ClockOutByEmployee))]
        public int? ClockOutByEmployeeId { get; set; }

        [AllowNull]
        [ForeignKey(nameof(ClockInByEmployee))]
        public int? ClockInByEmployeeId { get; set; }

        // Navigation properties
        public virtual Employee Employee { get; set; }
        public virtual Employee ClockOutByEmployee { get; set; }
        public virtual Employee ClockInByEmployee { get; set; }
    }
}