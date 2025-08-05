using System.ComponentModel.DataAnnotations;

namespace DocReachApi.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string MedicalLicenseNumber { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string HospitalAffiliation { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Degree { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Specialties { get; set; }
        
        [MaxLength(1000)]
        public string? Bio { get; set; }
        
        [MaxLength(500)]
        public string? MedicalLicensePath { get; set; }
        
        [MaxLength(500)]
        public string? DegreeCertificatePath { get; set; }
        
        public bool IsApproved { get; set; } = false;
        
        public bool IsOnline { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ApprovedAt { get; set; }
        
        public string? ApprovedBy { get; set; }
        
        // Location fields
        public double Latitude { get; set; }
        
        public double Longitude { get; set; }
        
        [MaxLength(200)]
        public string? LocationAddress { get; set; }
        
        // Navigation property
        public virtual ApplicationUser User { get; set; } = null!;
    }
} 