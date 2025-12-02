using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawdeIA.Models
{
    [Table("RAG_Embeddings")]
    public class RAGEmbedding
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmbeddingID { get; set; }

        [Required]
        public long ChunkID { get; set; }

        [Required]
        public int DocumentID { get; set; }

        [Required]
        public byte[] Vector { get; set; } = null!;

        [Required]
        public short VectorDimensions { get; set; }

        [Required]
        public int ChunkIndex { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? ModelUsed { get; set; }

        [StringLength(20)]
        public string EmbeddingVersion { get; set; } = "v1";

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("ChunkID")]
        public virtual RAGDocumentChunk Chunk { get; set; } = null!;

        [ForeignKey("DocumentID")]
        public virtual RAGDocument Document { get; set; } = null!;
    }
}
