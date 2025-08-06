using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<Users?> RegisterAsync(RegisterDto registerDto);
        Task<Users?> GetUserByUsernameAsync(string username);
        bool VerifyPassword(string password, string hashedPassword);
        string HashPassword(string password);
    }
}
