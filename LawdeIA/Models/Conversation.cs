using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawdeIA.Models
{
    [Table("Conversations")]
    public class Conversation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ConversationID { get; set; }

        [Required]
        public int UserID { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public int? SelectedDocumentID { get; set; }
        public int MessageCount { get; set; } = 0;

        [StringLength(500)]
        public string? LastMessagePreview { get; set; }

        public bool IsPinned { get; set; } = false;

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("SelectedDocumentID")]
        public virtual RAGDocument? SelectedDocument { get; set; }

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ConversationMetadata? Metadata { get; set; }
    }
}