using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models
{
    [Table("Chats")]
    public class Chat
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public bool IsGroup { get; set; } = false;

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? GroupAvatar { get; set; }

        [MaxLength(500)]
        public string? GroupDescription { get; set; }

        [MaxLength(50)]
        public string? CreatedById { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<CallLog> CallLogs { get; set; } = new List<CallLog>();
    }
}
