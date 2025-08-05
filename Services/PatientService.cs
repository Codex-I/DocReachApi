using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DocReachApi.Data;
using DocReachApi.Models;
using DocReachApi.Models.DTOs;
using System.Security.Claims;

namespace DocReachApi.Services
{
    public interface IPatientService
    {
        Task<bool> SubmitPatientKycAsync(string userId, PatientKycRequest request);
        Task<PatientProfileResponse> GetPatientProfileAsync(string userId);
        Task<bool> UpdatePatientProfileAsync(string userId, UpdatePatientProfileRequest request);
        Task<List<DoctorSearchResponse>> SearchDoctorsAsync(DoctorSearchRequest request);
        Task<DoctorDetailResponse?> GetDoctorDetailsAsync(string doctorId);
        Task<bool> ContactDoctorAsync(string patientId, ContactDoctorRequest request);
        Task<List<DoctorSearchResponse>> GetEmergencyDoctorsAsync(PatientEmergencyRequest request);
        Task<bool> UploadPatientDocumentAsync(string userId, VerificationDocumentRequest request);
        Task<VerificationStatusResponse> GetPatientKycStatusAsync(string userId);
    }

    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IFileUploadService fileUploadService,
            ILogger<PatientService> logger)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        public async Task<bool> SubmitPatientKycAsync(string userId, PatientKycRequest request)
        {
            try
            {
                // Validate user exists and is a patient
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for patient KYC: {userId}");
                    return false;
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Patient"))
                {
                    _logger.LogWarning($"User {userId} is not a patient");
                    return false;
                }

                // Validate age (must be at least 18 for healthcare)
                var age = DateTime.UtcNow.Year - request.DateOfBirth.Year;
                if (request.DateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;
                if (age < 18)
                {
                    _logger.LogWarning($"Patient {userId} is under 18 years old");
                    return false;
                }

                // Update user information
                user.FullName = request.FullName;
                user.DateOfBirth = request.DateOfBirth;
                user.Gender = request.Gender;
                user.Address = request.Address;
                user.PhoneNumber = request.PhoneNumber;
                user.Email = request.Email;
                user.UpdatedAt = DateTime.UtcNow;

                // Mark KYC as verified (basic KYC for patients)
                user.IsKycVerified = true;

                await _userManager.UpdateAsync(user);

                _logger.LogInformation($"Patient KYC submitted successfully for user: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting patient KYC for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<PatientProfileResponse> GetPatientProfileAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new PatientProfileResponse { UserId = userId };
                }

                return new PatientProfileResponse
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender ?? "",
                    Address = user.Address ?? "",
                    IsKycVerified = user.IsKycVerified,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient profile for user: {UserId}", userId);
                return new PatientProfileResponse { UserId = userId };
            }
        }

        public async Task<bool> UpdatePatientProfileAsync(string userId, UpdatePatientProfileRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(request.FullName))
                    user.FullName = request.FullName;

                if (!string.IsNullOrEmpty(request.Address))
                    user.Address = request.Address;

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    user.PhoneNumber = request.PhoneNumber;

                user.UpdatedAt = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);

                _logger.LogInformation($"Patient profile updated for user: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient profile for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<List<DoctorSearchResponse>> SearchDoctorsAsync(DoctorSearchRequest request)
        {
            try
            {
                var query = _context.Doctors
                    .Include(d => d.User)
                    .Where(d => d.IsApproved && d.User.IsActive);

                // Filter by specialty if provided
                if (!string.IsNullOrEmpty(request.Specialty))
                {
                    query = query.Where(d => d.Specialties.Contains(request.Specialty));
                }

                // Filter by online status if provided
                if (request.IsOnline.HasValue)
                {
                    query = query.Where(d => d.IsOnline == request.IsOnline.Value);
                }

                var doctors = await query.ToListAsync();

                // Calculate distances and filter by max distance
                var doctorResponses = new List<DoctorSearchResponse>();
                foreach (var doctor in doctors)
                {
                    var distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        doctor.Latitude, doctor.Longitude);

                    if (distance <= request.MaxDistance)
                    {
                        doctorResponses.Add(new DoctorSearchResponse
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
                            Rating = 0.0, // TODO: Implement rating system
                            ReviewCount = 0 // TODO: Implement review system
                        });
                    }
                }

                // Sort by distance and apply pagination
                var sortedDoctors = doctorResponses
                    .OrderBy(d => d.Distance)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return sortedDoctors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching doctors");
                return new List<DoctorSearchResponse>();
            }
        }

        public async Task<DoctorDetailResponse?> GetDoctorDetailsAsync(string doctorId)
        {
            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.UserId == doctorId && d.IsApproved);

                if (doctor == null)
                {
                    return null;
                }

                return new DoctorDetailResponse
                {
                    DoctorId = doctor.UserId,
                    FullName = doctor.User.FullName,
                    Specialties = doctor.Specialties ?? "",
                    HospitalAffiliation = doctor.HospitalAffiliation,
                    Degree = doctor.Degree,
                    Bio = doctor.Bio,
                    Latitude = doctor.Latitude,
                    Longitude = doctor.Longitude,
                    LocationAddress = doctor.LocationAddress,
                    IsOnline = doctor.IsOnline,
                    IsApproved = doctor.IsApproved,
                    ProfilePhotoPath = doctor.User.ProfilePhotoPath,
                    Rating = 0.0, // TODO: Implement rating system
                    ReviewCount = 0, // TODO: Implement review system
                    CreatedAt = doctor.CreatedAt,
                    AvailableServices = new List<string>(), // TODO: Implement services
                    Languages = new List<string>(), // TODO: Implement languages
                    ContactPhone = doctor.User.PhoneNumber,
                    ContactEmail = doctor.User.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor details for doctor: {DoctorId}", doctorId);
                return null;
            }
        }

        public async Task<bool> ContactDoctorAsync(string patientId, ContactDoctorRequest request)
        {
            try
            {
                // Validate doctor exists and is approved
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.UserId == request.DoctorId && d.IsApproved);

                if (doctor == null)
                {
                    return false;
                }

                // TODO: Implement contact system (email, SMS, in-app messaging)
                // For now, just log the contact request
                _logger.LogInformation($"Patient {patientId} contacted doctor {request.DoctorId}: {request.Message}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error contacting doctor");
                return false;
            }
        }

        public async Task<List<DoctorSearchResponse>> GetEmergencyDoctorsAsync(PatientEmergencyRequest request)
        {
            try
            {
                // Get all online, approved doctors within emergency distance (20km)
                var emergencyDoctors = await _context.Doctors
                    .Include(d => d.User)
                    .Where(d => d.IsApproved && d.IsOnline && d.User.IsActive)
                    .ToListAsync();

                var emergencyResponses = new List<DoctorSearchResponse>();
                foreach (var doctor in emergencyDoctors)
                {
                    var distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        doctor.Latitude, doctor.Longitude);

                    // Emergency distance: 20km
                    if (distance <= 20.0)
                    {
                        emergencyResponses.Add(new DoctorSearchResponse
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
                            Rating = 0.0,
                            ReviewCount = 0
                        });
                    }
                }

                // Sort by distance for emergency response
                return emergencyResponses.OrderBy(d => d.Distance).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emergency doctors");
                return new List<DoctorSearchResponse>();
            }
        }

        public async Task<bool> UploadPatientDocumentAsync(string userId, VerificationDocumentRequest request)
        {
            try
            {
                // Validate user exists
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Validate document type for patients
                var validDocumentTypes = new[] { "IdDocument", "ProofOfAddress", "MedicalRecord" };
                if (!validDocumentTypes.Contains(request.DocumentType))
                {
                    return false;
                }

                // Upload document
                var filePath = await _fileUploadService.UploadDocumentAsync(
                    request.FileContent,
                    request.FileName,
                    request.ContentType,
                    userId,
                    request.DocumentType);

                // Create KYC submission record
                var kycSubmission = new KycSubmission
                {
                    UserId = userId,
                    DocumentType = request.DocumentType,
                    DocumentPath = filePath,
                    Status = "Pending",
                    SubmittedAt = DateTime.UtcNow
                };

                _context.KycSubmissions.Add(kycSubmission);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Patient document uploaded successfully for user {userId}: {request.DocumentType}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading patient document for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<VerificationStatusResponse> GetPatientKycStatusAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new VerificationStatusResponse { UserId = userId };
                }

                var kycSubmissions = await _context.KycSubmissions
                    .Where(k => k.UserId == userId)
                    .ToListAsync();

                var requiredDocuments = new[] { "IdDocument", "ProofOfAddress" };
                var submittedDocuments = kycSubmissions.Select(k => k.DocumentType).ToList();
                var missingDocuments = requiredDocuments.Except(submittedDocuments).ToList();

                return new VerificationStatusResponse
                {
                    UserId = userId,
                    IsSubmitted = user.IsKycVerified,
                    IsUnderReview = !user.IsKycVerified && kycSubmissions.Any(),
                    IsApproved = user.IsKycVerified,
                    IsRejected = false, // Patients have simpler KYC
                    RejectionReason = null,
                    SubmittedAt = user.CreatedAt,
                    ReviewedAt = user.UpdatedAt,
                    ReviewedBy = null,
                    MissingDocuments = missingDocuments,
                    SubmittedDocuments = submittedDocuments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient KYC status for user: {UserId}", userId);
                return new VerificationStatusResponse { UserId = userId };
            }
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