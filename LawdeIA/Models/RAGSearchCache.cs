using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawdeIA.Models
{
    [Table("RAG_Search_Cache")]
    public class RAGSearchCache
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CacheID { get; set; }

        [Required]
        public byte[] QueryHash { get; set; } = null!;

        [Required]
        public int DocumentID { get; set; }

        [Required]
        [StringLength(1000)]
        public string QueryText { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(MAX)")]
        public string Results { get; set; } = string.Empty; // JSON

        [Required]
        public int ResultCount { get; set; }

        public decimal? MaxSimilarity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        [ForeignKey("DocumentID")]
        public virtual RAGDocument Document { get; set; } = null!;
    }
}
