using System.Threading.Tasks;

namespace MultiDeptReportingTool.Services.Interfaces
{
    public interface IEncryptionService
    {
        // Symmetric encryption for data at rest
        Task<string> EncryptAsync(string plaintext, string? keyId = null);
        Task<string> DecryptAsync(string ciphertext, string? keyId = null);
        
        // Field-level encryption for sensitive data
        Task<string> EncryptFieldAsync(string plaintext, string fieldType);
        Task<string> DecryptFieldAsync(string ciphertext, string fieldType);
        
        // File encryption for exports
        Task<byte[]> EncryptFileAsync(byte[] fileData, string fileName);
        Task<byte[]> DecryptFileAsync(byte[] encryptedData, string fileName);
        
        // Key management
        Task<string> GenerateKeyAsync(string keyType = "AES256");
        Task<bool> RotateKeyAsync(string keyId);
        Task<bool> IsKeyValidAsync(string keyId);
        
        // Hash generation for integrity checks
        string GenerateHash(byte[] data);
        bool VerifyHash(byte[] data, string hash);
    }
}
