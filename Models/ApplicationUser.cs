using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DocReachApi.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Address { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        
        [MaxLength(20)]
        public string? Gender { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // KYC related fields
        public bool IsKycVerified { get; set; } = false;
        
        [MaxLength(500)]
        public string? IdDocumentPath { get; set; }
        
        [MaxLength(500)]
        public string? ProfilePhotoPath { get; set; }
        
        // Navigation properties
        public virtual Doctor? Doctor { get; set; }
        public virtual Admin? Admin { get; set; }
    }
} 