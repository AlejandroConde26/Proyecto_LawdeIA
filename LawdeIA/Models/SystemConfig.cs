using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawdeIA.Models
{
    [Table("System_Config")]
    public class SystemConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ConfigID { get; set; }

        [Required]
        [StringLength(100)]
        public string ConfigKey { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(MAX)")]
        public string ConfigValue { get; set; } = string.Empty;

        [StringLength(20)]
        public string DataType { get; set; } = "String";

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedBy { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User? UpdatedByUser { get; set; }
    }
}