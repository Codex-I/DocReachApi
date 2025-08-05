using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DocReachApi.Models;
using DocReachApi.Models.DTOs;
using DocReachApi.Data;

namespace DocReachApi.Services
{
    public interface IJwtService
    {
        Task<AuthResponse> GenerateTokensAsync(ApplicationUser user);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        ClaimsPrincipal? ValidateToken(string token);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        Task<bool> RevokeAllUserTokensAsync(string userId);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public JwtService(IConfiguration configuration, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _configuration = configuration;
            _userManager = userManager;
            _context = context;
        }

        public async Task<AuthResponse> GenerateTokensAsync(ApplicationUser user)
        {
            try
            {
                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault() ?? "Patient";

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, userRole),
                    new Claim("UserId", user.Id),
                    new Claim("UserRole", userRole),
                    new Claim("IsKycVerified", user.IsKycVerified.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                // Add additional claims based on role
                if (userRole == "Doctor")
                {
                    var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == user.Id);
                    if (doctor != null)
                    {
                        claims.Add(new Claim("IsApproved", doctor.IsApproved.ToString()));
                        claims.Add(new Claim("IsOnline", doctor.IsOnline.ToString()));
                    }
                }

                // Generate access token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]!);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"])),
                    Issuer = _configuration["JwtSettings:Issuer"],
                    Audience = _configuration["JwtSettings:Audience"],
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var accessToken = tokenHandler.WriteToken(token);

                // Generate refresh token
                var refreshToken = GenerateRefreshToken();

                // Store refresh token in database (you might want to create a separate table for this)
                // For now, we'll store it in the user's SecurityStamp (not ideal for production)
                user.SecurityStamp = refreshToken;
                await _userManager.UpdateAsync(user);

                return new AuthResponse
                {
                    Success = true,
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = tokenDescriptor.Expires,
                    UserId = user.Id,
                    UserRole = userRole,
                    FullName = user.FullName,
                    RequiresKyc = !user.IsKycVerified,
                    IsKycVerified = user.IsKycVerified
                };
            }
            catch (Exception ex)
            {
                // Log the exception (implement proper logging)
                return new AuthResponse
                {
                    Success = false,
                    Message = "Failed to generate tokens"
                };
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // Find user by refresh token
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.SecurityStamp == refreshToken);
                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid refresh token"
                    };
                }

                // Validate refresh token (you might want to add expiration check)
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid refresh token"
                    };
                }

                // Generate new tokens
                return await GenerateTokensAsync(user);
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Failed to refresh token"
                };
            }
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]!);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.SecurityStamp == refreshToken);
                if (user != null)
                {
                    user.SecurityStamp = Guid.NewGuid().ToString();
                    await _userManager.UpdateAsync(user);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RevokeAllUserTokensAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.SecurityStamp = Guid.NewGuid().ToString();
                    await _userManager.UpdateAsync(user);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
} 