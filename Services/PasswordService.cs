using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using MultiDeptReportingTool.Services.Interfaces;

namespace MultiDeptReportingTool.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly IConfiguration _configuration;
        private readonly byte[] _pepper;
        private readonly int _saltSize;
        private readonly int _hashSize;
        private readonly int _degreeOfParallelism;
        private readonly int _iterations;
        private readonly int _memorySize;

        public PasswordService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Load from configuration or use defaults
            _saltSize = _configuration.GetValue<int>("Security:Argon2:SaltSize", 16);
            _hashSize = _configuration.GetValue<int>("Security:Argon2:HashSize", 32);
            _degreeOfParallelism = _configuration.GetValue<int>("Security:Argon2:DegreeOfParallelism", 8);
            _iterations = _configuration.GetValue<int>("Security:Argon2:Iterations", 4);
            _memorySize = _configuration.GetValue<int>("Security:Argon2:MemorySize", 1024 * 1024); // 1 GB default
            
            // Load pepper from configuration
            string pepperString = _configuration["Security:Pepper"] ?? "DefaultSecurePepperForMultiDeptReporting!@#$";
            _pepper = Encoding.UTF8.GetBytes(pepperString);
        }

        public string GenerateSalt()
        {
            var salt = new byte[_saltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        public async Task<(string Hash, string Salt)> HashPasswordAsync(string password)
        {
            var salt = GenerateSalt();
            var hash = await HashPasswordWithSaltAsync(password, salt);
            return (hash, salt);
        }

        public async Task<bool> VerifyPasswordAsync(string password, string hash, string salt)
        {
            var newHash = await HashPasswordWithSaltAsync(password, salt);
            return hash == newHash;
        }

        private async Task<string> HashPasswordWithSaltAsync(string password, string salt)
        {
            // Convert password to bytes
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            
            // Add pepper to password
            var passwordWithPepper = new byte[passwordBytes.Length + _pepper.Length];
            Buffer.BlockCopy(passwordBytes, 0, passwordWithPepper, 0, passwordBytes.Length);
            Buffer.BlockCopy(_pepper, 0, passwordWithPepper, passwordBytes.Length, _pepper.Length);
            
            // Convert salt to bytes
            var saltBytes = Convert.FromBase64String(salt);

            using (var argon2 = new Argon2id(passwordWithPepper))
            {
                argon2.Salt = saltBytes;
                argon2.DegreeOfParallelism = _degreeOfParallelism;
                argon2.Iterations = _iterations;
                argon2.MemorySize = _memorySize;

                var hashBytes = await argon2.GetBytesAsync(_hashSize);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
