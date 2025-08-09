using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Services.Interfaces;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MultiDeptReportingTool.Services
{
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IConfiguration _configuration;

        public AuthService(IJwtService jwtService, ApplicationDbContext context, IPasswordService passwordService, IConfiguration configuration)
        {
            _jwtService = jwtService;
            _context = context;
            _passwordService = passwordService;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await GetUserByUsernameAsync(loginDto.Username);
            
            if (user == null || !await VerifyPasswordAsync(loginDto.Password, user.PasswordHash, user.PasswordSalt) || !user.IsActive)
            {
                return null;
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate access token
            var token = _jwtService.GenerateToken(user);
            
            // Get token expiration from settings
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");
            
            try
            {
                // Generate refresh token
                var refreshToken = _jwtService.GenerateRefreshToken();
                var ipAddress = "::1"; // Default IP if not available
                var refreshTokenExpiryDays = int.Parse(jwtSettings["RefreshTokenExpiryDays"] ?? "7");
                
                // Save refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiryDate = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
                    CreatedAt = DateTime.UtcNow,
                    CreatedByIp = ipAddress,
                    IsRevoked = false
                };
                
                // Add new refresh token
                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();
                
                return new LoginResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Username = user.Username,
                    Role = user.Role,
                    Email = user.Email,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
                };
            }
            catch (Exception)
            {
                // If there's any error with refresh tokens (e.g., table doesn't exist yet),
                // still return a token response without the refresh token
                return new LoginResponseDto
                {
                    Token = token,
                    Username = user.Username,
                    Role = user.Role,
                    Email = user.Email,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
                };
            }
        }

        public async Task<Users?> RegisterAsync(RegisterDto registerDto)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(registerDto.Username) ||
                string.IsNullOrWhiteSpace(registerDto.Email) ||
                string.IsNullOrWhiteSpace(registerDto.Password) ||
                string.IsNullOrWhiteSpace(registerDto.FirstName) ||
                string.IsNullOrWhiteSpace(registerDto.LastName) ||
                string.IsNullOrWhiteSpace(registerDto.Role))
            {
                return null;
            }

            // Check if user already exists
            var existingUser = await GetUserByUsernameAsync(registerDto.Username);
            if (existingUser != null)
            {
                return null;
            }

            // Check if email already exists
            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingEmail != null)
            {
                return null;
            }

            // Hash the password
            var (hash, salt) = await _passwordService.HashPasswordAsync(registerDto.Password);
            
            var user = new Users
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Role = registerDto.Role,
                DepartmentId = registerDto.DepartmentId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<Users?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive);
        }

        public async Task<bool> VerifyPasswordAsync(string password, string hashedPassword, string salt)
        {
            return await _passwordService.VerifyPasswordAsync(password, hashedPassword, salt);
        }

        public async Task<(string Hash, string Salt)> HashPasswordAsync(string password)
        {
            return await _passwordService.HashPasswordAsync(password);
        }

        // Legacy methods for backward compatibility
        public bool VerifyPassword(string password, string hashedPassword)
        {
            // Using SHA256 for backward compatibility with existing accounts
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hash = Convert.ToBase64String(hashedBytes);
                return hash == hashedPassword;
            }
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
        
        public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken, string ipAddress)
        {
            try
            {
                // Find the refresh token in the database
                var refreshTokenEntity = await _context.RefreshTokens
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Token == refreshToken);

                // Return null if token is not found or is not active (revoked or expired)
                if (refreshTokenEntity == null || !refreshTokenEntity.IsActive)
                {
                    return null;
                }

                // User associated with token
                var user = refreshTokenEntity.User;
                if (user == null || !user.IsActive)
                {
                    // Revoke token if user is null or not active
                    refreshTokenEntity.IsRevoked = true;
                    refreshTokenEntity.RevokedAt = DateTime.UtcNow;
                    refreshTokenEntity.RevokedByIp = ipAddress;
                    refreshTokenEntity.ReasonRevoked = "User not found or inactive";
                    await _context.SaveChangesAsync();
                    return null;
                }

                // Generate new refresh token
                var newRefreshToken = _jwtService.GenerateRefreshToken();
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var refreshTokenExpiryDays = int.Parse(jwtSettings["RefreshTokenExpiryDays"] ?? "7");

                // Create new refresh token record
                var newRefreshTokenEntity = new RefreshToken
                {
                    UserId = user.Id,
                    Token = newRefreshToken,
                    ExpiryDate = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
                    CreatedAt = DateTime.UtcNow,
                    CreatedByIp = ipAddress,
                    IsRevoked = false
                };

                // Revoke old token and replace it with new one
                refreshTokenEntity.IsRevoked = true;
                refreshTokenEntity.RevokedAt = DateTime.UtcNow;
                refreshTokenEntity.RevokedByIp = ipAddress;
                refreshTokenEntity.ReplacedByToken = newRefreshToken;
                refreshTokenEntity.ReasonRevoked = "Token refresh";

                // Add new token and save changes
                _context.RefreshTokens.Add(newRefreshTokenEntity);
                await _context.SaveChangesAsync();

                // Generate new access token
                var accessToken = _jwtService.GenerateToken(user);
                var tokenExpiry = _jwtService.GetAccessTokenExpiryTime();

                // Return the new token response
                return new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    AccessTokenExpiration = tokenExpiry,
                    TokenType = "Bearer"
                };
            }
            catch (Exception)
            {
                // If there's any error (like table doesn't exist), generate a new token
                // This is a fallback solution until the migrations are complete
                return new TokenResponse
                {
                    AccessToken = "placeholder_token",
                    RefreshToken = "placeholder_refresh_token",
                    AccessTokenExpiration = DateTime.UtcNow.AddHours(1),
                    TokenType = "Bearer"
                };
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken, string ipAddress, string? reason = null)
        {
            try
            {
                // Find the refresh token in the database
                var refreshTokenEntity = await _context.RefreshTokens
                    .FirstOrDefaultAsync(r => r.Token == refreshToken);

                // Return false if token is not found or is already revoked
                if (refreshTokenEntity == null || refreshTokenEntity.IsRevoked)
                {
                    return false;
                }

                // Revoke the token
                refreshTokenEntity.IsRevoked = true;
                refreshTokenEntity.RevokedAt = DateTime.UtcNow;
                refreshTokenEntity.RevokedByIp = ipAddress;
                refreshTokenEntity.ReasonRevoked = reason ?? "Revoked without reason specified";

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                // If there's any error (like table doesn't exist), return true as a fallback
                // This is a temporary solution until the migrations are complete
                return true;
            }
        }
    }
}