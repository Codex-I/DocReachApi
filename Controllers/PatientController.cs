using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocReachApi.Models.DTOs;
using DocReachApi.Services;
using System.Security.Claims;

namespace DocReachApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientController> _logger;

        public PatientController(IPatientService patientService, ILogger<PatientController> logger)
        {
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// Submit patient KYC (requires Patient role)
        /// </summary>
        [HttpPost("kyc")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SubmitKyc([FromBody] PatientKycRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var result = await _patientService.SubmitPatientKycAsync(userId, request);
                
                if (result)
                {
                    _logger.LogInformation($"Patient KYC submitted for user: {userId}");
                    return Ok(new { Message = "KYC submitted successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to submit KYC" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting patient KYC");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Get patient profile (requires Patient role)
        /// </summary>
        [HttpGet("profile")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(PatientProfileResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var profile = await _patientService.GetPatientProfileAsync(userId);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient profile");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Update patient profile (requires Patient role)
        /// </summary>
        [HttpPut("profile")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdatePatientProfileRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var result = await _patientService.UpdatePatientProfileAsync(userId, request);
                
                if (result)
                {
                    _logger.LogInformation($"Patient profile updated for user: {userId}");
                    return Ok(new { Message = "Profile updated successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to update profile" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient profile");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Search for doctors (requires Patient role)
        /// </summary>
        [HttpPost("search-doctors")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(List<DoctorSearchResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SearchDoctors([FromBody] DoctorSearchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data" });
                }

                var doctors = await _patientService.SearchDoctorsAsync(request);
                return Ok(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching doctors");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Get doctor details (requires Patient role)
        /// </summary>
        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(DoctorDetailResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDoctorDetails(string doctorId)
        {
            try
            {
                var doctor = await _patientService.GetDoctorDetailsAsync(doctorId);
                if (doctor == null)
                {
                    return NotFound(new { Message = "Doctor not found" });
                }

                return Ok(doctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor details");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Contact doctor (requires Patient role)
        /// </summary>
        [HttpPost("contact-doctor")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ContactDoctor([FromBody] ContactDoctorRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data" });
                }

                var patientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(patientId))
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var result = await _patientService.ContactDoctorAsync(patientId, request);
                
                if (result)
                {
                    _logger.LogInformation($"Patient {patientId} contacted doctor {request.DoctorId}");
                    return Ok(new { Message = "Message sent successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to send message" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error contacting doctor");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Get emergency doctors (requires Patient role)
        /// </summary>
        [HttpPost("emergency")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(List<DoctorSearchResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetEmergencyDoctors([FromBody] PatientEmergencyRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data" });
                }

                var emergencyDoctors = await _patientService.GetEmergencyDoctorsAsync(request);
                return Ok(emergencyDoctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emergency doctors");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Upload patient document (requires Patient role)
        /// </summary>
        [HttpPost("upload-document")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UploadDocument([FromBody] VerificationDocumentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var result = await _patientService.UploadPatientDocumentAsync(userId, request);
                
                if (result)
                {
                    _logger.LogInformation($"Document uploaded for patient: {userId}, type: {request.DocumentType}");
                    return Ok(new { Message = "Document uploaded successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to upload document" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading patient document");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Get patient KYC status (requires Patient role)
        /// </summary>
        [HttpGet("kyc-status")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(VerificationStatusResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetKycStatus()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var status = await _patientService.GetPatientKycStatusAsync(userId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient KYC status");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Get nearby doctors (simple search)
        /// </summary>
        [HttpGet("nearby-doctors")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(List<DoctorSearchResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetNearbyDoctors([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double maxDistance = 10.0)
        {
            try
            {
                var request = new DoctorSearchRequest
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    MaxDistance = maxDistance,
                    IsOnline = true, // Only online doctors
                    IsApproved = true, // Only approved doctors
                    Page = 1,
                    PageSize = 50
                };

                var doctors = await _patientService.SearchDoctorsAsync(request);
                return Ok(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby doctors");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Get doctor specialties (for search filters)
        /// </summary>
        [HttpGet("specialties")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public IActionResult GetSpecialties()
        {
            try
            {
                var specialties = new[]
                {
                    "Cardiology",
                    "Dermatology",
                    "Emergency Medicine",
                    "Family Medicine",
                    "General Surgery",
                    "Internal Medicine",
                    "Neurology",
                    "Obstetrics & Gynecology",
                    "Oncology",
                    "Ophthalmology",
                    "Orthopedics",
                    "Pediatrics",
                    "Psychiatry",
                    "Radiology",
                    "Urology"
                };

                return Ok(specialties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting specialties");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }
    }
} 