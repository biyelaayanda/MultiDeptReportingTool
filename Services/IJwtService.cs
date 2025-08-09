using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;
using System.Security.Claims;

namespace MultiDeptReportingTool.Services
{
    public interface IJwtService
    {
        string GenerateToken(Users user);
        bool ValidateToken(string token);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        DateTime GetAccessTokenExpiryTime();
    }
}
