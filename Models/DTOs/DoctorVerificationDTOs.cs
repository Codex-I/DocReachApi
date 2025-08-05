using System.ComponentModel.DataAnnotations;

namespace DocReachApi.Models.DTOs
{
    public class DoctorVerificationRequest
    {
        [Required]
        [MaxLength(100)]
        public string MedicalLicenseNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string HospitalAffiliation { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Degree { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Specialties { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Bio { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [MaxLength(200)]
        public string? LocationAddress { get; set; }

        // Document upload paths (handled separately)
        public string? MedicalLicensePath { get; set; }
        public string? DegreeCertificatePath { get; set; }
        public string? HospitalAffiliationLetterPath { get; set; }
        public string? ProfessionalPhotoPath { get; set; }
    }

    public class VerificationDocumentRequest
    {
        [Required]
        public string DocumentType { get; set; } = string.Empty; // MedicalLicense, DegreeCertificate, HospitalAffiliation, ProfessionalPhoto

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FileContent { get; set; } = string.Empty; // Base64 encoded

        [Required]
        public string ContentType { get; set; } = string.Empty;
    }

    public class VerificationStatusResponse
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsSubmitted { get; set; }
        public bool IsUnderReview { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }
        public List<string> MissingDocuments { get; set; } = new();
        public List<string> SubmittedDocuments { get; set; } = new();
    }

    public class AdminVerificationRequest
    {
        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [Required]
        public bool IsApproved { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        [MaxLength(1000)]
        public string? AdminNotes { get; set; }
    }

    public class VerificationDocument
    {
        public string DocumentType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public bool IsVerified { get; set; }
        public string? VerificationNotes { get; set; }
    }
} 