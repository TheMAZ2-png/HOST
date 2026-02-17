using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace HOST.Models
{
    public class SystemSetting
    {
        [Key]
        [StringLength(50)]
        public string SettingKey { get; set; }

        [AllowNull]
        [StringLength(200)]
        public string SettingValue { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime UpdatedAt { get; set; }

        [Required]
        [ForeignKey(nameof(UpdatedByEmployee))]
        public int UpdatedByEmployeeId { get; set; }

        // Navigation properties
        public virtual Employee UpdatedByEmployee { get; set; }
    }
}