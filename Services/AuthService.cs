using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MultiDeptReportingTool.Services
{
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;

        public AuthService(IJwtService jwtService, ApplicationDbContext context, IPasswordService passwordService)
        {
            _jwtService = jwtService;
            _context = context;
            _passwordService = passwordService;
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

            var token = _jwtService.GenerateToken(user);
            var jwtSettings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .GetSection("JwtSettings");
            
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

            return new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                Email = user.Email,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };
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
    }
}