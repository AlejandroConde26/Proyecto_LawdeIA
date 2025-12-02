using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawdeIA.Models
{
    [Table("Conversation_Metadata")]
    public class ConversationMetadata
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MetadataID { get; set; }

        [Required]
        public int ConversationID { get; set; }

        [StringLength(50)]
        public string? ModelUsed { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string? Parameters { get; set; } // JSON

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ConversationID")]
        public virtual Conversation Conversation { get; set; } = null!;
    }
}
