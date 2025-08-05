using System.ComponentModel.DataAnnotations;

namespace DocReachApi.Models.DTOs
{
    public class EmergencySearchRequest
    {
        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [MaxLength(500)]
        public string? EmergencyType { get; set; } // "Cardiac", "Trauma", "General", etc.

        [MaxLength(1000)]
        public string? EmergencyDescription { get; set; }

        public bool IsUrgent { get; set; } = true;

        public double MaxDistance { get; set; } = 20.0; // Emergency distance

        public bool RequireSpecialist { get; set; } = false;

        public string? PatientName { get; set; }

        public string? PatientPhone { get; set; }
    }

    public class EmergencyDoctorResponse
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
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public int EstimatedResponseTime { get; set; } // Minutes
        public bool IsEmergencySpecialist { get; set; }
        public List<string> EmergencyServices { get; set; } = new();
    }

    public class EmergencyContactRequest
    {
        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [Required]
        public string ContactMethod { get; set; } = string.Empty; // "Call", "Message", "VideoCall"

        [MaxLength(1000)]
        public string? EmergencyDescription { get; set; }

        [Required]
        public double PatientLatitude { get; set; }

        [Required]
        public double PatientLongitude { get; set; }

        [MaxLength(100)]
        public string? PatientName { get; set; }

        [MaxLength(20)]
        public string? PatientPhone { get; set; }

        public bool IsUrgent { get; set; } = true;

        [MaxLength(500)]
        public string? PatientAddress { get; set; }
    }

    public class EmergencyContactResponse
    {
        public bool Success { get; set; }
        public string? ContactInfo { get; set; } // Phone number, email, etc.
        public string? Message { get; set; }
        public string? Error { get; set; }
        public DateTime ContactTime { get; set; }
        public string ContactMethod { get; set; } = string.Empty;
    }

    public class DoctorAvailabilityRequest
    {
        [Required]
        public string DoctorId { get; set; } = string.Empty;

        public bool IsOnline { get; set; }

        [MaxLength(500)]
        public string? StatusMessage { get; set; } // "Available", "In Surgery", "On Call", etc.

        public DateTime? AvailableUntil { get; set; }
    }

    public class DoctorAvailabilityResponse
    {
        public string DoctorId { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public string? StatusMessage { get; set; }
        public DateTime? AvailableUntil { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class EmergencyType
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] RecommendedSpecialties { get; set; } = new string[0];
        public bool RequiresImmediateAttention { get; set; }
    }
} 