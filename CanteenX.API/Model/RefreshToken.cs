using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CanteenX.API.Model
{
    public class RefreshToken
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public string UserId { get; set; } = null!;
        [Required]
        public string TokenHash { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public string? CreatedByIp { get; set; }

        public string? RevokedByIp { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [NotMapped]
        public bool IsActive => RevokedAt == null && !IsExpired;
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
