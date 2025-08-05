using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocReachApi.Models.DTOs;
using DocReachApi.Services;
using System.Security.Claims;

namespace DocReachApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user (Patient, Doctor, or Admin)
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                // Additional validation for healthcare
                if (request.Role == "Doctor" && string.IsNullOrEmpty(request.Address))
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Address is required for doctor registration"
                    });
                }

                var result = await _authService.RegisterAsync(request);
                
                if (result.Success)
                {
                    _logger.LogInformation($"New user registered: {request.Email} with role: {request.Role}");
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "An internal server error occurred"
                });
            }
        }

        /// <summary>
        /// Login user with email and password
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                var result = await _authService.LoginAsync(request);
                
                if (result.Success)
                {
                    _logger.LogInformation($"User logged in: {request.Email}");
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "An internal server error occurred"
                });
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(AuthResponse), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid refresh token"
                    });
                }

                var result = await _authService.RefreshTokenAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "An internal server error occurred"
                });
            }
        }

        /// <summary>
        /// Change user password (requires authentication)
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
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

                var result = await _authService.ChangePasswordAsync(userId, request);
                
                if (result)
                {
                    _logger.LogInformation($"Password changed for user: {userId}");
                    return Ok(new { Message = "Password changed successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to change password" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Request password reset (sends email)
        /// </summary>
        [HttpPost("forgot-password")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid email address" });
                }

                var result = await _authService.ForgotPasswordAsync(request);
                
                // Always return success to prevent email enumeration
                return Ok(new { Message = "If the email exists, a password reset link has been sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password for email: {Email}", request.Email);
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Reset password using token from email
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data" });
                }

                var result = await _authService.ResetPasswordAsync(request);
                
                if (result)
                {
                    return Ok(new { Message = "Password reset successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to reset password. Token may be invalid or expired." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Logout user (revokes all tokens)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var result = await _authService.LogoutAsync(userId);
                
                if (result)
                {
                    _logger.LogInformation($"User logged out: {userId}");
                    return Ok(new { Message = "Logged out successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to logout" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Confirm email address using token
        /// </summary>
        [HttpPost("confirm-email")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ConfirmEmail([FromBody] EmailConfirmationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new { Message = "Invalid confirmation data" });
                }

                var result = await _authService.ConfirmEmailAsync(request.UserId, request.Token);
                
                if (result)
                {
                    return Ok(new { Message = "Email confirmed successfully" });
                }
                else
                {
                    return BadRequest(new { Message = "Failed to confirm email. Token may be invalid or expired." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Resend email confirmation
        /// </summary>
        [HttpPost("resend-email-confirmation")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ResendEmailConfirmation([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid email address" });
                }

                var result = await _authService.ResendEmailConfirmationAsync(request.Email);
                
                // Always return success to prevent email enumeration
                return Ok(new { Message = "If the email exists and is not confirmed, a confirmation email has been sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending email confirmation for email: {Email}", request.Email);
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Get current user profile (requires authentication)
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public IActionResult GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var fullName = User.FindFirst(ClaimTypes.Name)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var isKycVerified = User.FindFirst("IsKycVerified")?.Value;

                return Ok(new
                {
                    UserId = userId,
                    Email = email,
                    FullName = fullName,
                    Role = role,
                    IsKycVerified = bool.Parse(isKycVerified ?? "false")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }

        /// <summary>
        /// Debug endpoint to test token generation (for development only)
        /// </summary>
        [HttpPost("debug-generate-token")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DebugGenerateToken([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                // Use the resend email confirmation service instead
                var result = await _authService.ResendEmailConfirmationAsync(request.Email);
                if (!result)
                {
                    return BadRequest(new { Message = "User not found or error occurred" });
                }

                return Ok(new { Message = "Token generated and logged. Check console for details." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating debug token");
                return StatusCode(500, new { Message = "An internal server error occurred" });
            }
        }
    }
} 