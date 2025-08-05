using System.ComponentModel.DataAnnotations;

namespace DocReachApi.Models
{
    public class KycSubmission
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty; // NIN, Passport, Driver's License, Medical License, etc.
        
        [Required]
        [MaxLength(500)]
        public string DocumentPath { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? AdditionalDocumentPath { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        
        [MaxLength(500)]
        public string? RejectionReason { get; set; }
        
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReviewedAt { get; set; }
        
        public string? ReviewedBy { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        // Navigation property
        public virtual ApplicationUser User { get; set; } = null!;
    }
} 