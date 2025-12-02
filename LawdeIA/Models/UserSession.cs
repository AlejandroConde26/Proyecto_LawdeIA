// Models/UserSession.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawdeIA.Models
{
    [Table("User_Sessions")]
    public class UserSession
    {
        [Key]
        public Guid SessionID { get; set; } = Guid.NewGuid();

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(512)]
        public string Token { get; set; } = string.Empty;

        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        [StringLength(45)]
        public string? IPAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public bool IsValid { get; set; } = true;

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;
    }
}
