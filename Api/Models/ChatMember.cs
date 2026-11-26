using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models
{
    [Table("ChatMembers")]
    public class ChatMember
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(50)]
        public string ChatId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Role { get; set; } = "MEMBER"; // ADMIN, MEMBER

        public bool IsPinned { get; set; } = false;

        public bool IsMuted { get; set; } = false;

        [MaxLength(50)]
        public string? LastReadMessageId { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ChatId")]
        public Chat Chat { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
