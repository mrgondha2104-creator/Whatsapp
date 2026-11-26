using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [MaxLength(50)]
        public string? EmpCode { get; set; } // e.g. EMP-001

        [Required]
        [MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Department { get; set; } = "General";

        [MaxLength(100)]
        public string Designation { get; set; } = "Staff";

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "Employee"; // Admin, Manager, Employee

        [MaxLength(255)]
        public string Password { get; set; } = "Password@123";

        // Permissions
        public bool CanSendFiles { get; set; } = true;
        public bool CanMakeCalls { get; set; } = true;
        public bool CanCreateGroups { get; set; } = true;
        public bool CanPostStatus { get; set; } = true;
        public bool CanDeleteMessages { get; set; } = true;
        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Avatar { get; set; }

        [MaxLength(255)]
        public string About { get; set; } = "Hey there! I am using in-house WhatsApp.";

        public bool IsOnline { get; set; } = false;

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ChatMember> ChatMemberships { get; set; } = new List<ChatMember>();
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public ICollection<StarredMessage> StarredMessages { get; set; } = new List<StarredMessage>();
        public ICollection<Status> StatusUpdates { get; set; } = new List<Status>();
    }
}
