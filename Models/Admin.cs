using System.ComponentModel.DataAnnotations;

namespace DocReachApi.Models
{
    public class Admin
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string AdminRole { get; set; } = "Admin"; // Admin, SuperAdmin, etc.
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation property
        public virtual ApplicationUser User { get; set; } = null!;
    }
} 