using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models
{
    [Table("Statuses")]
    public class Status
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(50)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "TEXT"; // TEXT, IMAGE, VIDEO

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Caption { get; set; }

        [MaxLength(30)]
        public string BackgroundColor { get; set; } = "#128c7e";

        [MaxLength(50)]
        public string Font { get; set; } = "sans-serif";

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
