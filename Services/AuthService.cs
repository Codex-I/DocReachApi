using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DocReachApi.Data;
using DocReachApi.Models;
using DocReachApi.Models.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace DocReachApi.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
        Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        Task<bool> LogoutAsync(string userId);
        Task<bool> ConfirmEmailAsync(string userId, string token);
        Task<bool> ResendEmailConfirmationAsync(string email);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IJwtService jwtService,
            ApplicationDbContext context,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _context = context;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Validate age (must be at least 18 for healthcare)
                var age = DateTime.UtcNow.Year - request.DateOfBirth.Year;
                if (request.DateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;
                if (age < 18)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "You must be at least 18 years old to register"
                    };
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "User with this email already exists"
                    };
                }

                // Create user
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Address = request.Address,
                    EmailConfirmed = false, // Require email confirmation for security
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = true, // Enable 2FA for healthcare
                    LockoutEnabled = true,
                    AccessFailedCount = 0
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = string.Join(", ", result.Errors.Select(e => e.Description))
                    };
                }

                // Ensure role exists
                if (!await _roleManager.RoleExistsAsync(request.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(request.Role));
                }

                // Assign role
                await _userManager.AddToRoleAsync(user, request.Role);

                // Create role-specific records
                if (request.Role == "Doctor")
                {
                    var doctor = new Doctor
                    {
                        UserId = user.Id,
                        MedicalLicenseNumber = "", // Will be filled during KYC
                        HospitalAffiliation = "",
                        Degree = "",
                        IsApproved = false,
                        IsOnline = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Doctors.Add(doctor);
                }
                else if (request.Role == "Admin")
                {
                    var admin = new Admin
                    {
                        UserId = user.Id,
                        AdminRole = "Admin",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Admins.Add(admin);
                }

                await _context.SaveChangesAsync();

                // Generate email confirmation token
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // TODO: Send confirmation email (implement email service)
                _logger.LogInformation($"Email confirmation token for {user.Email}: {emailToken}");

                return new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful. Please check your email to confirm your account.",
                    UserId = user.Id,
                    UserRole = request.Role,
                    FullName = user.FullName,
                    RequiresKyc = true,
                    IsKycVerified = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during registration"
                };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // Find user
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Check if account is locked
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Account is temporarily locked. Please try again later."
                    };
                }

                // Verify password
                var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!passwordValid)
                {
                    await _userManager.AccessFailedAsync(user);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Check if email is confirmed
                if (!user.EmailConfirmed)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Please confirm your email address before logging in"
                    };
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Account is deactivated. Please contact support."
                    };
                }

                // Reset failed count on successful login
                await _userManager.ResetAccessFailedCountAsync(user);

                // Generate tokens
                var authResponse = await _jwtService.GenerateTokensAsync(user);
                authResponse.Message = "Login successful";

                return authResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            return await _jwtService.RefreshTokenAsync(request.RefreshToken);
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return false;
            }
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null) return true; // Don't reveal if user exists

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // TODO: Send reset email (implement email service)
                _logger.LogInformation($"Password reset token for {user.Email}: {token}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password");
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null) return false;

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return false;
            }
        }

        public async Task<bool> LogoutAsync(string userId)
        {
            try
            {
                return await _jwtService.RevokeAllUserTokensAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return false;
            }
        }

        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var result = await _userManager.ConfirmEmailAsync(user, token);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error confirming email {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ResendEmailConfirmationAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null) return false;

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // TODO: Send confirmation email (implement email service)
                _logger.LogInformation($"Email confirmation token for {user.Email}: {token}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending email confirmation");
                return false;
            }
        }
    }
} 