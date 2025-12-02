using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawdeIA.Models
{
    [Table("Usage_Metrics")]
    public class UsageMetric
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MetricID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int ModelID { get; set; }

        [Required]
        [StringLength(50)]
        public string OperationType { get; set; } = string.Empty;

        [Required]
        public int TokensUsed { get; set; }

        public decimal? CostIncurred { get; set; }
        public int? DurationMs { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ModelID")]
        public virtual AIModel Model { get; set; } = null!;
    }
}
