using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DocReachApi.Data;
using DocReachApi.Models;
using DocReachApi.Models.DTOs;

namespace DocReachApi.Services
{
    public interface IDoctorVerificationService
    {
        Task<bool> SubmitVerificationAsync(string userId, DoctorVerificationRequest request);
        Task<bool> UploadVerificationDocumentAsync(string userId, VerificationDocumentRequest request);
        Task<VerificationStatusResponse> GetVerificationStatusAsync(string userId);
        Task<bool> AdminReviewVerificationAsync(string adminId, AdminVerificationRequest request);
        Task<List<Doctor>> GetPendingVerificationsAsync();
        Task<bool> ValidateMedicalLicenseAsync(string licenseNumber, string hospitalAffiliation);
        Task<bool> ValidateHospitalAffiliationAsync(string hospitalName, string doctorName);
        Task<bool> PerformBackgroundCheckAsync(string userId, string fullName, string licenseNumber);
    }

    public class DoctorVerificationService : IDoctorVerificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<DoctorVerificationService> _logger;

        public DoctorVerificationService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IFileUploadService fileUploadService,
            ILogger<DoctorVerificationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        public async Task<bool> SubmitVerificationAsync(string userId, DoctorVerificationRequest request)
        {
            try
            {
                // Validate user exists and is a doctor
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for verification: {userId}");
                    return false;
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Doctor"))
                {
                    _logger.LogWarning($"User {userId} is not a doctor");
                    return false;
                }

                // Get or create doctor record
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null)
                {
                    _logger.LogWarning($"Doctor record not found for user: {userId}");
                    return false;
                }

                // Validate required documents are uploaded
                var requiredDocuments = new[] { "MedicalLicense", "DegreeCertificate", "HospitalAffiliation" };
                var uploadedDocuments = await _context.KycSubmissions
                    .Where(k => k.UserId == userId && requiredDocuments.Contains(k.DocumentType))
                    .Select(k => k.DocumentType)
                    .ToListAsync();

                var missingDocuments = requiredDocuments.Except(uploadedDocuments).ToList();
                if (missingDocuments.Any())
                {
                    _logger.LogWarning($"Missing required documents for user {userId}: {string.Join(", ", missingDocuments)}");
                    return false;
                }

                // Update doctor information
                doctor.MedicalLicenseNumber = request.MedicalLicenseNumber;
                doctor.HospitalAffiliation = request.HospitalAffiliation;
                doctor.Degree = request.Degree;
                doctor.Specialties = request.Specialties;
                doctor.Bio = request.Bio;
                doctor.Latitude = request.Latitude;
                doctor.Longitude = request.Longitude;
                doctor.LocationAddress = request.LocationAddress;
                doctor.UpdatedAt = DateTime.UtcNow;

                // Perform validation checks
                var licenseValid = await ValidateMedicalLicenseAsync(request.MedicalLicenseNumber, request.HospitalAffiliation);
                var hospitalValid = await ValidateHospitalAffiliationAsync(request.HospitalAffiliation, user.FullName);
                var backgroundCheck = await PerformBackgroundCheckAsync(userId, user.FullName, request.MedicalLicenseNumber);

                if (!licenseValid || !hospitalValid || !backgroundCheck)
                {
                    _logger.LogWarning($"Verification validation failed for user {userId}");
                    return false;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Doctor verification submitted successfully for user: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting doctor verification for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UploadVerificationDocumentAsync(string userId, VerificationDocumentRequest request)
        {
            try
            {
                // Validate user exists
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Validate document type
                var validDocumentTypes = new[] { "MedicalLicense", "DegreeCertificate", "HospitalAffiliation", "ProfessionalPhoto" };
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

                _logger.LogInformation($"Document uploaded successfully for user {userId}: {request.DocumentType}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading verification document for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<VerificationStatusResponse> GetVerificationStatusAsync(string userId)
        {
            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.UserId == userId);

                if (doctor == null)
                {
                    return new VerificationStatusResponse { UserId = userId };
                }

                var kycSubmissions = await _context.KycSubmissions
                    .Where(k => k.UserId == userId)
                    .ToListAsync();

                var requiredDocuments = new[] { "MedicalLicense", "DegreeCertificate", "HospitalAffiliation", "ProfessionalPhoto" };
                var submittedDocuments = kycSubmissions.Select(k => k.DocumentType).ToList();
                var missingDocuments = requiredDocuments.Except(submittedDocuments).ToList();

                return new VerificationStatusResponse
                {
                    UserId = userId,
                    IsSubmitted = !string.IsNullOrEmpty(doctor.MedicalLicenseNumber),
                    IsUnderReview = !string.IsNullOrEmpty(doctor.MedicalLicenseNumber) && !doctor.IsApproved,
                    IsApproved = doctor.IsApproved,
                    IsRejected = !doctor.IsApproved && !string.IsNullOrEmpty(doctor.MedicalLicenseNumber),
                    RejectionReason = null, // Would be stored in a separate table
                    SubmittedAt = doctor.CreatedAt,
                    ReviewedAt = doctor.ApprovedAt,
                    ReviewedBy = doctor.ApprovedBy,
                    MissingDocuments = missingDocuments,
                    SubmittedDocuments = submittedDocuments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification status for user: {UserId}", userId);
                return new VerificationStatusResponse { UserId = userId };
            }
        }

        public async Task<bool> AdminReviewVerificationAsync(string adminId, AdminVerificationRequest request)
        {
            try
            {
                // Validate admin
                var admin = await _userManager.FindByIdAsync(adminId);
                if (admin == null)
                {
                    return false;
                }

                var adminRoles = await _userManager.GetRolesAsync(admin);
                if (!adminRoles.Contains("Admin"))
                {
                    return false;
                }

                // Get doctor record
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == request.DoctorId);
                if (doctor == null)
                {
                    return false;
                }

                // Update verification status
                doctor.IsApproved = request.IsApproved;
                doctor.UpdatedAt = DateTime.UtcNow;

                if (request.IsApproved)
                {
                    doctor.ApprovedAt = DateTime.UtcNow;
                    doctor.ApprovedBy = adminId;
                }

                // Update KYC submissions status
                var kycSubmissions = await _context.KycSubmissions
                    .Where(k => k.UserId == request.DoctorId)
                    .ToListAsync();

                foreach (var submission in kycSubmissions)
                {
                    submission.Status = request.IsApproved ? "Approved" : "Rejected";
                    submission.ReviewedAt = DateTime.UtcNow;
                    submission.ReviewedBy = adminId;
                    submission.RejectionReason = request.IsApproved ? null : request.RejectionReason;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin {adminId} reviewed doctor {request.DoctorId}: {(request.IsApproved ? "Approved" : "Rejected")}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in admin review for doctor: {DoctorId}", request.DoctorId);
                return false;
            }
        }

        public async Task<List<Doctor>> GetPendingVerificationsAsync()
        {
            try
            {
                return await _context.Doctors
                    .Include(d => d.User)
                    .Where(d => !string.IsNullOrEmpty(d.MedicalLicenseNumber) && !d.IsApproved)
                    .OrderBy(d => d.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending verifications");
                return new List<Doctor>();
            }
        }

        public async Task<bool> ValidateMedicalLicenseAsync(string licenseNumber, string hospitalAffiliation)
        {
            try
            {
                // TODO: Integrate with medical board API for real-time validation
                // For now, perform basic validation
                
                if (string.IsNullOrEmpty(licenseNumber) || string.IsNullOrEmpty(hospitalAffiliation))
                {
                    return false;
                }

                // Check license number format (basic validation)
                if (licenseNumber.Length < 6 || licenseNumber.Length > 20)
                {
                    return false;
                }

                // Check for suspicious patterns
                if (licenseNumber.Contains("TEST") || licenseNumber.Contains("DEMO"))
                {
                    return false;
                }

                // TODO: Call external medical board API
                // var isValid = await MedicalBoardApi.ValidateLicenseAsync(licenseNumber, hospitalAffiliation);
                
                // For now, return true if basic validation passes
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating medical license: {LicenseNumber}", licenseNumber);
                return false;
            }
        }

        public async Task<bool> ValidateHospitalAffiliationAsync(string hospitalName, string doctorName)
        {
            try
            {
                // TODO: Integrate with hospital directory API
                // For now, perform basic validation
                
                if (string.IsNullOrEmpty(hospitalName) || string.IsNullOrEmpty(doctorName))
                {
                    return false;
                }

                // Check for suspicious patterns
                if (hospitalName.Contains("TEST") || hospitalName.Contains("DEMO"))
                {
                    return false;
                }

                // TODO: Call external hospital directory API
                // var isValid = await HospitalDirectoryApi.ValidateAffiliationAsync(hospitalName, doctorName);
                
                // For now, return true if basic validation passes
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating hospital affiliation: {HospitalName}", hospitalName);
                return false;
            }
        }

        public async Task<bool> PerformBackgroundCheckAsync(string userId, string fullName, string licenseNumber)
        {
            try
            {
                // TODO: Integrate with background check service
                // For now, perform basic checks
                
                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(licenseNumber))
                {
                    return false;
                }

                // Check for suspicious patterns in name
                if (fullName.Contains("TEST") || fullName.Contains("DEMO") || fullName.Length < 3)
                {
                    return false;
                }

                // TODO: Call external background check API
                // var backgroundCheck = await BackgroundCheckApi.PerformCheckAsync(fullName, licenseNumber);
                
                // For now, return true if basic validation passes
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing background check for user: {UserId}", userId);
                return false;
            }
        }
    }
} 