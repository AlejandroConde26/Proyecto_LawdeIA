using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawdeIA.Models
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MessageID { get; set; }

        [Required]
        public int ConversationID { get; set; }

        [Required]
        [StringLength(20)]
        public string SenderType { get; set; } = string.Empty; // "User" or "AI"

        [Required]
        [Column(TypeName = "nvarchar(MAX)")]
        public string Content { get; set; } = string.Empty;

        public byte[]? ContentHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }

        public long? ParentMessageID { get; set; }
        public int? TokensUsed { get; set; }

        [StringLength(50)]
        public string? ModelUsed { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string? Metadata { get; set; }

        // Navigation properties
        [ForeignKey("ConversationID")]
        public virtual Conversation Conversation { get; set; } = null!;

        [ForeignKey("ParentMessageID")]
        public virtual Message? ParentMessage { get; set; }

        public virtual ICollection<Message> ChildMessages { get; set; } = new List<Message>();
    }
}