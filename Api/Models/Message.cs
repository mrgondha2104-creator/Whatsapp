using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(50)]
        public string ChatId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SenderId { get; set; } = string.Empty;

        public string? Text { get; set; }

        [MaxLength(30)]
        public string Type { get; set; } = "TEXT"; // TEXT, IMAGE, VIDEO, AUDIO, VOICE, DOCUMENT, SYSTEM

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        [MaxLength(255)]
        public string? FileName { get; set; }

        public long? FileSize { get; set; }

        [MaxLength(100)]
        public string? FileMime { get; set; }

        public double? Duration { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "SENT"; // SENT, DELIVERED, READ

        public bool IsDeleted { get; set; } = false;

        [MaxLength(50)]
        public string? ReplyToId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ChatId")]
        public Chat Chat { get; set; } = null!;

        [ForeignKey("SenderId")]
        public User Sender { get; set; } = null!;

        [ForeignKey("ReplyToId")]
        public Message? ReplyTo { get; set; }

        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public ICollection<StarredMessage> StarredBy { get; set; } = new List<StarredMessage>();
    }
}
