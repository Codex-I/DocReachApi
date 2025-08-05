using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DocReachApi.Data;
using DocReachApi.Models;
using DocReachApi.Models.DTOs;

namespace DocReachApi.Services
{
    public interface IEmergencyService
    {
        Task<List<EmergencyDoctorResponse>> GetEmergencyDoctorsAsync(EmergencySearchRequest request);
        Task<EmergencyContactResponse> ContactEmergencyDoctorAsync(string patientId, EmergencyContactRequest request);
        Task<bool> UpdateDoctorAvailabilityAsync(string doctorId, DoctorAvailabilityRequest request);
        Task<DoctorAvailabilityResponse> GetDoctorAvailabilityAsync(string doctorId);
        Task<List<EmergencyType>> GetEmergencyTypesAsync();
        Task<List<EmergencyDoctorResponse>> GetNearestEmergencyDoctorsAsync(double latitude, double longitude, int count = 5);
    }

    public class EmergencyService : IEmergencyService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EmergencyService> _logger;

        // Emergency specialties mapping
        private readonly Dictionary<string, string[]> _emergencySpecialties = new()
        {
            ["Cardiac"] = new[] { "Cardiology", "Emergency Medicine", "Internal Medicine" },
            ["Trauma"] = new[] { "Emergency Medicine", "General Surgery", "Orthopedics" },
            ["Respiratory"] = new[] { "Pulmonology", "Emergency Medicine", "Internal Medicine" },
            ["Neurological"] = new[] { "Neurology", "Emergency Medicine", "Neurosurgery" },
            ["Pediatric"] = new[] { "Pediatrics", "Emergency Medicine" },
            ["General"] = new[] { "Emergency Medicine", "Family Medicine", "Internal Medicine" }
        };

        public EmergencyService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<EmergencyService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<List<EmergencyDoctorResponse>> GetEmergencyDoctorsAsync(EmergencySearchRequest request)
        {
            try
            {
                // Get all approved, online doctors
                var query = _context.Doctors
                    .Include(d => d.User)
                    .Where(d => d.IsApproved && d.IsOnline && d.User.IsActive);

                // Filter by emergency specialty if specified
                if (!string.IsNullOrEmpty(request.EmergencyType) && _emergencySpecialties.ContainsKey(request.EmergencyType))
                {
                    var specialties = _emergencySpecialties[request.EmergencyType];
                    query = query.Where(d => specialties.Any(s => d.Specialties.Contains(s)));
                }

                var doctors = await query.ToListAsync();

                // Calculate distances and create emergency responses
                var emergencyDoctors = new List<EmergencyDoctorResponse>();
                foreach (var doctor in doctors)
                {
                    var distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        doctor.Latitude, doctor.Longitude);

                    if (distance <= request.MaxDistance)
                    {
                        var isEmergencySpecialist = IsEmergencySpecialist(doctor.Specialties);
                        var estimatedResponseTime = CalculateEstimatedResponseTime(distance, isEmergencySpecialist);

                        emergencyDoctors.Add(new EmergencyDoctorResponse
                        {
                            DoctorId = doctor.UserId,
                            FullName = doctor.User.FullName,
                            Specialties = doctor.Specialties ?? "",
                            HospitalAffiliation = doctor.HospitalAffiliation,
                            Bio = doctor.Bio,
                            Latitude = doctor.Latitude,
                            Longitude = doctor.Longitude,
                            LocationAddress = doctor.LocationAddress,
                            Distance = distance,
                            IsOnline = doctor.IsOnline,
                            IsApproved = doctor.IsApproved,
                            ProfilePhotoPath = doctor.User.ProfilePhotoPath,
                            Rating = 0.0, // TODO: Implement rating
                            ReviewCount = 0, // TODO: Implement reviews
                            ContactPhone = doctor.User.PhoneNumber,
                            ContactEmail = doctor.User.Email,
                            EstimatedResponseTime = estimatedResponseTime,
                            IsEmergencySpecialist = isEmergencySpecialist,
                            EmergencyServices = GetEmergencyServices(doctor.Specialties)
                        });
                    }
                }

                // Sort by priority: emergency specialists first, then by distance
                return emergencyDoctors
                    .OrderBy(d => !d.IsEmergencySpecialist) // Emergency specialists first
                    .ThenBy(d => d.Distance)
                    .ThenBy(d => d.EstimatedResponseTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emergency doctors");
                return new List<EmergencyDoctorResponse>();
            }
        }

        public async Task<EmergencyContactResponse> ContactEmergencyDoctorAsync(string patientId, EmergencyContactRequest request)
        {
            try
            {
                // Validate doctor exists and is available
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.UserId == request.DoctorId && d.IsApproved && d.IsOnline);

                if (doctor == null)
                {
                    return new EmergencyContactResponse
                    {
                        Success = false,
                        Error = "Doctor not available"
                    };
                }

                // Handle different contact methods
                switch (request.ContactMethod.ToLower())
                {
                    case "call":
                        return await HandleEmergencyCall(doctor, request);
                    case "message":
                        return await HandleEmergencyMessage(doctor, request);
                    case "videocall":
                        return await HandleEmergencyVideoCall(doctor, request);
                    default:
                        return new EmergencyContactResponse
                        {
                            Success = false,
                            Error = "Invalid contact method"
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error contacting emergency doctor");
                return new EmergencyContactResponse
                {
                    Success = false,
                    Error = "Failed to contact doctor"
                };
            }
        }

        public async Task<bool> UpdateDoctorAvailabilityAsync(string doctorId, DoctorAvailabilityRequest request)
        {
            try
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorId);
                if (doctor == null)
                {
                    return false;
                }

                doctor.IsOnline = request.IsOnline;
                doctor.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Doctor {doctorId} availability updated: {(request.IsOnline ? "Online" : "Offline")}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor availability");
                return false;
            }
        }

        public async Task<DoctorAvailabilityResponse> GetDoctorAvailabilityAsync(string doctorId)
        {
            try
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorId);
                if (doctor == null)
                {
                    return new DoctorAvailabilityResponse { DoctorId = doctorId };
                }

                return new DoctorAvailabilityResponse
                {
                    DoctorId = doctor.UserId,
                    IsOnline = doctor.IsOnline,
                    StatusMessage = doctor.IsOnline ? "Available" : "Offline",
                    LastUpdated = doctor.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor availability");
                return new DoctorAvailabilityResponse { DoctorId = doctorId };
            }
        }

        public async Task<List<EmergencyType>> GetEmergencyTypesAsync()
        {
            try
            {
                return new List<EmergencyType>
                {
                    new EmergencyType
                    {
                        Type = "Cardiac",
                        Description = "Heart attack, chest pain, cardiac arrest",
                        RecommendedSpecialties = new[] { "Cardiology", "Emergency Medicine" },
                        RequiresImmediateAttention = true
                    },
                    new EmergencyType
                    {
                        Type = "Trauma",
                        Description = "Accidents, injuries, fractures",
                        RecommendedSpecialties = new[] { "Emergency Medicine", "General Surgery", "Orthopedics" },
                        RequiresImmediateAttention = true
                    },
                    new EmergencyType
                    {
                        Type = "Respiratory",
                        Description = "Breathing difficulties, asthma attack",
                        RecommendedSpecialties = new[] { "Pulmonology", "Emergency Medicine" },
                        RequiresImmediateAttention = true
                    },
                    new EmergencyType
                    {
                        Type = "Neurological",
                        Description = "Stroke, severe headache, seizures",
                        RecommendedSpecialties = new[] { "Neurology", "Emergency Medicine" },
                        RequiresImmediateAttention = true
                    },
                    new EmergencyType
                    {
                        Type = "Pediatric",
                        Description = "Child emergency, fever, injury",
                        RecommendedSpecialties = new[] { "Pediatrics", "Emergency Medicine" },
                        RequiresImmediateAttention = true
                    },
                    new EmergencyType
                    {
                        Type = "General",
                        Description = "General emergency, unknown condition",
                        RecommendedSpecialties = new[] { "Emergency Medicine", "Family Medicine" },
                        RequiresImmediateAttention = false
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emergency types");
                return new List<EmergencyType>();
            }
        }

        public async Task<List<EmergencyDoctorResponse>> GetNearestEmergencyDoctorsAsync(double latitude, double longitude, int count = 5)
        {
            try
            {
                var request = new EmergencySearchRequest
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    MaxDistance = 20.0,
                    IsUrgent = true
                };

                var emergencyDoctors = await GetEmergencyDoctorsAsync(request);
                return emergencyDoctors.Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearest emergency doctors");
                return new List<EmergencyDoctorResponse>();
            }
        }

        private async Task<EmergencyContactResponse> HandleEmergencyCall(Doctor doctor, EmergencyContactRequest request)
        {
            // TODO: Integrate with phone system
            _logger.LogInformation($"Emergency call initiated: Patient {request.PatientName} calling doctor {doctor.User.FullName}");
            
            return new EmergencyContactResponse
            {
                Success = true,
                ContactInfo = doctor.User.PhoneNumber,
                Message = "Calling doctor now...",
                ContactTime = DateTime.UtcNow,
                ContactMethod = "Call"
            };
        }

        private async Task<EmergencyContactResponse> HandleEmergencyMessage(Doctor doctor, EmergencyContactRequest request)
        {
            // TODO: Integrate with messaging system
            _logger.LogInformation($"Emergency message sent: Patient {request.PatientName} messaging doctor {doctor.User.FullName}");
            
            return new EmergencyContactResponse
            {
                Success = true,
                ContactInfo = doctor.User.Email,
                Message = "Emergency message sent to doctor",
                ContactTime = DateTime.UtcNow,
                ContactMethod = "Message"
            };
        }

        private async Task<EmergencyContactResponse> HandleEmergencyVideoCall(Doctor doctor, EmergencyContactRequest request)
        {
            // TODO: Integrate with video calling system
            _logger.LogInformation($"Emergency video call initiated: Patient {request.PatientName} calling doctor {doctor.User.FullName}");
            
            return new EmergencyContactResponse
            {
                Success = true,
                ContactInfo = "Video call link generated",
                Message = "Video call initiated",
                ContactTime = DateTime.UtcNow,
                ContactMethod = "VideoCall"
            };
        }

        private bool IsEmergencySpecialist(string? specialties)
        {
            if (string.IsNullOrEmpty(specialties))
                return false;

            var emergencySpecialties = new[] { "Emergency Medicine", "Cardiology", "Trauma Surgery", "Critical Care" };
            return emergencySpecialties.Any(s => specialties.Contains(s));
        }

        private int CalculateEstimatedResponseTime(double distance, bool isEmergencySpecialist)
        {
            // Base response time: 5-15 minutes for emergency specialists, 10-30 minutes for others
            var baseTime = isEmergencySpecialist ? 10 : 20;
            var distanceFactor = distance * 2; // 2 minutes per km
            return (int)Math.Min(baseTime + distanceFactor, 60); // Max 60 minutes
        }

        private List<string> GetEmergencyServices(string? specialties)
        {
            if (string.IsNullOrEmpty(specialties))
                return new List<string>();

            var services = new List<string>();
            if (specialties.Contains("Emergency Medicine"))
                services.Add("Emergency Care");
            if (specialties.Contains("Cardiology"))
                services.Add("Cardiac Care");
            if (specialties.Contains("Trauma"))
                services.Add("Trauma Care");
            if (specialties.Contains("Pediatrics"))
                services.Add("Pediatric Care");

            return services;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
} 