using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawdeIA.Models
{
    [Table("RAG_Documents")]
    public class RAGDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DocumentID { get; set; }

        public int? UserID { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [StringLength(255)]
        public string? FileName { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(MAX)")]
        public string Content { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? ContentSummary { get; set; }

        public long FileSize { get; set; }

        [StringLength(50)]
        public string? FileType { get; set; }

        [StringLength(200)]
        public string? Source { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime? LastAccessed { get; set; }

        [StringLength(20)]
        public string AccessLevel { get; set; } = "Private";

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(50)]
        public string? ProcessingStatus { get; set; }

        public int ChunkCount { get; set; } = 0;

        [StringLength(50)]
        public string? EmbeddingModel { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string? Metadata { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }

        public virtual ICollection<RAGDocumentChunk> Chunks { get; set; } = new List<RAGDocumentChunk>();
        public virtual ICollection<RAGEmbedding> Embeddings { get; set; } = new List<RAGEmbedding>();
        public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    }
}
