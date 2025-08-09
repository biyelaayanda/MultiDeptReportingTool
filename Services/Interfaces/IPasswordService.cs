using System;
using System.Threading.Tasks;

namespace MultiDeptReportingTool.Services.Interfaces
{
    public interface IPasswordService
    {
        Task<(string Hash, string Salt)> HashPasswordAsync(string password);
        Task<bool> VerifyPasswordAsync(string password, string hash, string salt);
        string GenerateSalt();
    }
}
