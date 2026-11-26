using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models
{
    [Table("CallLogs")]
    public class CallLog
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(50)]
        public string CallerId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ReceiverId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ChatId { get; set; }

        [MaxLength(20)]
        public string Type { get; set; } = "AUDIO"; // AUDIO, VIDEO

        [MaxLength(20)]
        public string Status { get; set; } = "MISSED"; // MISSED, REJECTED, COMPLETED

        public int Duration { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CallerId")]
        public User Caller { get; set; } = null!;

        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; } = null!;

        [ForeignKey("ChatId")]
        public Chat? Chat { get; set; }
    }
}
