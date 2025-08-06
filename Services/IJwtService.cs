using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Services
{
    public interface IJwtService
    {
        string GenerateToken(Users user);
        bool ValidateToken(string token);
    }
}
