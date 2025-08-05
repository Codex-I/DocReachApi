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
    public class EmergencyController : ControllerBase
    {
        private readonly IEmergencyService _emergencyService;
        private readonly ILogger<EmergencyController> _logger;

        public EmergencyController(IEmergencyService emergencyService, ILogger<EmergencyController> logger)
        {
            _emergencyService = emergencyService;
            _logger = logger;
        }

        /// <summary>
        /// SOS - Get emergency doctors (requires Patient role)
        /// </summary>
        [HttpPost("sos")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(List<EmergencyDoctorResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Sos([FromBody] EmergencySearchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data" });
                }

                var emergencyDoctors = await _emergencyService.GetEmergencyDoctorsAsync(request);
                
                _logger.LogInformation($"SOS activated at {request.Latitude}, {request.Longitude}. Found {emergencyDoctors.Count} doctors");
                
                return Ok(new
                {
                    Doctors = emergencyDoctors,
                    TotalCount = emergencyDoctors.Count,
                    SearchLocation = new { Latitude = request.Latitude, Longitude = request.Longitude },
                    EmergencyType = request.EmergencyType,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SOS emergency search");
                return StatusCode(500, new { Message = "Emergency service temporarily unavailable" });
            }
        }

        /// <summary>
        /// Quick SOS - Get nearest emergency doctors (requires Patient role)
        /// </summary>
        [HttpGet("quick-sos")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(List<EmergencyDoctorResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> QuickSos([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] int count = 5)
        {
            try
            {
                var emergencyDoctors = await _emergencyService.GetNearestEmergencyDoctorsAsync(latitude, longitude, count);
                
                _logger.LogInformation($"Quick SOS activated at {latitude}, {longitude}. Found {emergencyDoctors.Count} doctors");
                
                return Ok(new
                {
                    Doctors = emergencyDoctors,
                    TotalCount = emergencyDoctors.Count,
                    SearchLocation = new { Latitude = latitude, Longitude = longitude },
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick SOS");
                return StatusCode(500, new { Message = "Emergency service temporarily unavailable" });
            }
        }

        /// <summary>
        /// Contact emergency doctor (requires Patient role)
        /// </summary>
        [HttpPost("contact")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(EmergencyContactResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ContactEmergencyDoctor([FromBody] EmergencyContactRequest request)
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

                var response = await _emergencyService.ContactEmergencyDoctorAsync(patientId, request);
                
                if (response.Success)
                {
                    _logger.LogInformation($"Emergency contact initiated: Patient {patientId} contacted doctor {request.DoctorId} via {request.ContactMethod}");
                }
                else
                {
                    _logger.LogWarning($"Emergency contact failed: Patient {patientId} tried to contact doctor {request.DoctorId}");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error contacting emergency doctor");
                return StatusCode(500, new { Message = "Failed to contact doctor" });
            }
        }

        /// <summary>
        /// Get emergency types (for SOS button)
        /// </summary>
        [HttpGet("types")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(List<EmergencyType>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetEmergencyTypes()
        {
            try
            {
                var emergencyTypes = await _emergencyService.GetEmergencyTypesAsync();
                return Ok(emergencyTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emergency types");
                return StatusCode(500, new { Message = "Failed to get emergency types" });
            }
        }

        /// <summary>
        /// Update doctor availability (requires Doctor role)
        /// </summary>
        [HttpPost("availability")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateAvailability([FromBody] DoctorAvailabilityRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data" });
                }

                var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(doctorId))
                {
                    return Unauthorized(new { Message = "Doctor not authenticated" });
                }

                // Ensure doctor can only update their own availability
                request.DoctorId = doctorId;

                var result = await _emergencyService.UpdateDoctorAvailabilityAsync(doctorId, request);
                
                if (result)
                {
                    _logger.LogInformation($"Doctor {doctorId} availability updated: {(request.IsOnline ? "Online" : "Offline")}");
                    return Ok(new { Message = "Availability updated successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to update availability" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor availability");
                return StatusCode(500, new { Message = "Failed to update availability" });
            }
        }

        /// <summary>
        /// Get doctor availability (for patients)
        /// </summary>
        [HttpGet("availability/{doctorId}")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(DoctorAvailabilityResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDoctorAvailability(string doctorId)
        {
            try
            {
                var availability = await _emergencyService.GetDoctorAvailabilityAsync(doctorId);
                return Ok(availability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor availability");
                return StatusCode(500, new { Message = "Failed to get doctor availability" });
            }
        }

        /// <summary>
        /// Emergency call doctor (direct phone call)
        /// </summary>
        [HttpPost("call")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(EmergencyContactResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> EmergencyCall([FromBody] EmergencyContactRequest request)
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

                // Force contact method to call
                request.ContactMethod = "Call";

                var response = await _emergencyService.ContactEmergencyDoctorAsync(patientId, request);
                
                if (response.Success)
                {
                    _logger.LogInformation($"Emergency call initiated: Patient {patientId} calling doctor {request.DoctorId}");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making emergency call");
                return StatusCode(500, new { Message = "Failed to initiate call" });
            }
        }

        /// <summary>
        /// Emergency message doctor (urgent messaging)
        /// </summary>
        [HttpPost("message")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(EmergencyContactResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> EmergencyMessage([FromBody] EmergencyContactRequest request)
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

                // Force contact method to message
                request.ContactMethod = "Message";

                var response = await _emergencyService.ContactEmergencyDoctorAsync(patientId, request);
                
                if (response.Success)
                {
                    _logger.LogInformation($"Emergency message sent: Patient {patientId} messaging doctor {request.DoctorId}");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending emergency message");
                return StatusCode(500, new { Message = "Failed to send message" });
            }
        }

        /// <summary>
        /// Emergency video call (telemedicine)
        /// </summary>
        [HttpPost("video-call")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(EmergencyContactResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> EmergencyVideoCall([FromBody] EmergencyContactRequest request)
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

                // Force contact method to video call
                request.ContactMethod = "VideoCall";

                var response = await _emergencyService.ContactEmergencyDoctorAsync(patientId, request);
                
                if (response.Success)
                {
                    _logger.LogInformation($"Emergency video call initiated: Patient {patientId} calling doctor {request.DoctorId}");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating emergency video call");
                return StatusCode(500, new { Message = "Failed to initiate video call" });
            }
        }
    }
} 