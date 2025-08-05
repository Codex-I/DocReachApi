using System.ComponentModel.DataAnnotations;

namespace DocReachApi.Models.DTOs
{
    public class PatientKycRequest
    {
        [Required]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Full name can only contain letters and spaces")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [MaxLength(20)]
        [RegularExpression(@"^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(500)]
        public string? MedicalHistory { get; set; }

        [MaxLength(500)]
        public string? Allergies { get; set; }

        [MaxLength(500)]
        public string? CurrentMedications { get; set; }
    }

    public class DoctorSearchRequest
    {
        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [MaxLength(100)]
        public string? Specialty { get; set; }

        public double? MaxDistance { get; set; } = 10.0; // Default 10km

        public bool? IsOnline { get; set; }

        public bool? IsApproved { get; set; } = true; // Only approved doctors by default

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }

    public class DoctorSearchResponse
    {
        public string DoctorId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Specialties { get; set; } = string.Empty;
        public string HospitalAffiliation { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? LocationAddress { get; set; }
        public double Distance { get; set; } // Distance in km
        public bool IsOnline { get; set; }
        public bool IsApproved { get; set; }
        public string? ProfilePhotoPath { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class DoctorDetailResponse
    {
        public string DoctorId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Specialties { get; set; } = string.Empty;
        public string HospitalAffiliation { get; set; } = string.Empty;
        public string Degree { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? LocationAddress { get; set; }
        public bool IsOnline { get; set; }
        public bool IsApproved { get; set; }
        public string? ProfilePhotoPath { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> AvailableServices { get; set; } = new();
        public List<string> Languages { get; set; } = new();
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
    }

    public class PatientProfileResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsKycVerified { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? MedicalHistory { get; set; }
        public string? Allergies { get; set; }
        public string? CurrentMedications { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdatePatientProfileRequest
    {
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Full name can only contain letters and spaces")]
        public string? FullName { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(500)]
        public string? MedicalHistory { get; set; }

        [MaxLength(500)]
        public string? Allergies { get; set; }

        [MaxLength(500)]
        public string? CurrentMedications { get; set; }
    }

    public class ContactDoctorRequest
    {
        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Subject { get; set; }

        public bool IsUrgent { get; set; } = false;
    }

    public class PatientEmergencyRequest
    {
        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        [MaxLength(500)]
        public string EmergencyDescription { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? PatientName { get; set; }

        [MaxLength(20)]
        public string? PatientPhone { get; set; }

        public bool IsUrgent { get; set; } = true;
    }
} 