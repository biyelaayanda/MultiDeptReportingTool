using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;
using System.Security.Cryptography;
using System.Text;

namespace MultiDeptReportingTool.Services
{
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly ApplicationDbContext _context;

        public AuthService(IJwtService jwtService, ApplicationDbContext context)
        {
            _jwtService = jwtService;
            _context = context;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await GetUserByUsernameAsync(loginDto.Username);
            
            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash) || !user.IsActive)
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

            var user = new Users
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
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

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
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