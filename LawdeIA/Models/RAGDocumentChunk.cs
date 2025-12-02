using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawdeIA.Models
{
    [Table("RAG_Document_Chunks")]
    public class RAGDocumentChunk
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ChunkID { get; set; }

        [Required]
        public int DocumentID { get; set; }

        [Required]
        public int ChunkIndex { get; set; }

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;

        public byte[]? ContentHash { get; set; }
        public int? TokenCount { get; set; }
        public int? PageNumber { get; set; }

        [StringLength(255)]
        public string? SectionTitle { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("DocumentID")]
        public virtual RAGDocument Document { get; set; } = null!;

        public virtual ICollection<RAGEmbedding> Embeddings { get; set; } = new List<RAGEmbedding>();
    }
}