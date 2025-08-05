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
    public class DoctorVerificationController : ControllerBase
    {
        private readonly IDoctorVerificationService _verificationService;
        private readonly ILogger<DoctorVerificationController> _logger;

        public DoctorVerificationController(IDoctorVerificationService verificationService, ILogger<DoctorVerificationController> logger)
        {
            _verificationService = verificationService;
            _logger = logger;
        }

        /// <summary>
        /// Submit doctor verification (requires Doctor role)
        /// </summary>
        [HttpPost("submit")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SubmitVerification([FromBody] DoctorVerificationRequest request)
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

                var result = await _verificationService.SubmitVerificationAsync(userId, request);
                
                if (result)
                {
                    _logger.LogInformation($"Doctor verification submitted for user: {userId}");
                    return Ok(new { Message = "Verification submitted successfully. It will be reviewed by our team." });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to submit verification. Please ensure all required documents are uploaded." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting doctor verification");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Upload verification document (requires Doctor role)
        /// </summary>
        [HttpPost("upload-document")]
        [Authorize(Roles = "Doctor")]
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

                var result = await _verificationService.UploadVerificationDocumentAsync(userId, request);
                
                if (result)
                {
                    _logger.LogInformation($"Document uploaded for user: {userId}, type: {request.DocumentType}");
                    return Ok(new { Message = "Document uploaded successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to upload document" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading verification document");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Get verification status (requires Doctor role)
        /// </summary>
        [HttpGet("status")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(VerificationStatusResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetVerificationStatus()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var status = await _verificationService.GetVerificationStatusAsync(userId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification status");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Get pending verifications (requires Admin role)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPendingVerifications()
        {
            try
            {
                var pendingVerifications = await _verificationService.GetPendingVerificationsAsync();
                return Ok(pendingVerifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending verifications");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Review doctor verification (requires Admin role)
        /// </summary>
        [HttpPost("review")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ReviewVerification([FromBody] AdminVerificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data" });
                }

                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminId))
                {
                    return Unauthorized(new { Message = "Admin not authenticated" });
                }

                var result = await _verificationService.AdminReviewVerificationAsync(adminId, request);
                
                if (result)
                {
                    var action = request.IsApproved ? "approved" : "rejected";
                    _logger.LogInformation($"Admin {adminId} {action} doctor {request.DoctorId}");
                    return Ok(new { Message = $"Doctor verification {(request.IsApproved ? "approved" : "rejected")} successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to review verification" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing doctor verification");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Get verification requirements (for Doctor role)
        /// </summary>
        [HttpGet("requirements")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public IActionResult GetVerificationRequirements()
        {
            try
            {
                var requirements = new
                {
                    RequiredDocuments = new[]
                    {
                        new { Type = "MedicalLicense", Name = "Medical License", Description = "Valid medical license from recognized medical board" },
                        new { Type = "DegreeCertificate", Name = "Medical Degree Certificate", Description = "Medical degree certificate from accredited institution" },
                        new { Type = "HospitalAffiliation", Name = "Hospital Affiliation Letter", Description = "Letter confirming current hospital affiliation" },
                        new { Type = "ProfessionalPhoto", Name = "Professional Photo", Description = "Recent professional headshot (optional)" }
                    },
                    RequiredInformation = new[]
                    {
                        "Medical License Number",
                        "Hospital Affiliation",
                        "Medical Degree",
                        "Specialties",
                        "Practice Location (GPS coordinates)",
                        "Professional Bio"
                    },
                    ValidationProcess = new[]
                    {
                        "License verification with medical board",
                        "Hospital affiliation verification",
                        "Background check",
                        "Document authenticity verification",
                        "Admin review and approval"
                    },
                    EstimatedTime = "3-5 business days"
                };

                return Ok(requirements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification requirements");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }
    }
} 