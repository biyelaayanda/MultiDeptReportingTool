using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MultiDeptReportingTool.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MultiDeptReportingTool.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EncryptionService> _logger;
        private readonly string _defaultKey;
        private readonly Dictionary<string, byte[]> _encryptionKeys;
        private readonly Dictionary<string, byte[]> _fieldKeys;

        public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _encryptionKeys = new Dictionary<string, byte[]>();
            _fieldKeys = new Dictionary<string, byte[]>();
            
            // Load or generate default encryption key
            _defaultKey = _configuration["Security:Encryption:DefaultKey"] ?? GenerateDefaultKey();
            
            // Initialize field-specific keys
            InitializeFieldKeys();
        }

        public async Task<string> EncryptAsync(string plaintext, string? keyId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(plaintext))
                    return plaintext;

                var key = GetKey(keyId ?? "default");
                var iv = GenerateIV();

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var encryptor = aes.CreateEncryptor();
                var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

                // Combine IV + Ciphertext + Key ID for storage
                var result = new EncryptedData
                {
                    IV = Convert.ToBase64String(iv),
                    Data = Convert.ToBase64String(ciphertextBytes),
                    KeyId = keyId ?? "default",
                    Timestamp = DateTime.UtcNow
                };

                return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt data");
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        public async Task<string> DecryptAsync(string ciphertext, string? keyId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(ciphertext))
                    return ciphertext;

                var encryptedJson = Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
                var encryptedData = JsonSerializer.Deserialize<EncryptedData>(encryptedJson);

                if (encryptedData == null)
                    throw new InvalidOperationException("Invalid encrypted data format");

                var key = GetKey(encryptedData.KeyId);
                var iv = Convert.FromBase64String(encryptedData.IV);
                var data = Convert.FromBase64String(encryptedData.Data);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(data, 0, data.Length);
                
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt data");
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }

        public async Task<string> EncryptFieldAsync(string plaintext, string fieldType)
        {
            try
            {
                if (string.IsNullOrEmpty(plaintext))
                    return plaintext;

                var key = GetFieldKey(fieldType);
                return await EncryptWithKeyAsync(plaintext, key, $"field_{fieldType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt field of type {FieldType}", fieldType);
                throw new InvalidOperationException($"Field encryption failed for {fieldType}", ex);
            }
        }

        public async Task<string> DecryptFieldAsync(string ciphertext, string fieldType)
        {
            try
            {
                if (string.IsNullOrEmpty(ciphertext))
                    return ciphertext;

                var key = GetFieldKey(fieldType);
                return await DecryptWithKeyAsync(ciphertext, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt field of type {FieldType}", fieldType);
                throw new InvalidOperationException($"Field decryption failed for {fieldType}", ex);
            }
        }

        public async Task<byte[]> EncryptFileAsync(byte[] fileData, string fileName)
        {
            try
            {
                var key = GetKey("file_encryption");
                var iv = GenerateIV();

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var encryptor = aes.CreateEncryptor();
                var encryptedData = encryptor.TransformFinalBlock(fileData, 0, fileData.Length);

                // Create file encryption header
                var header = new FileEncryptionHeader
                {
                    IV = iv,
                    FileName = fileName,
                    OriginalSize = fileData.Length,
                    EncryptedAt = DateTime.UtcNow,
                    Hash = GenerateHash(fileData)
                };

                var headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header));
                var headerSize = BitConverter.GetBytes(headerBytes.Length);

                // Combine: HeaderSize(4) + Header + EncryptedData
                var result = new byte[4 + headerBytes.Length + encryptedData.Length];
                Buffer.BlockCopy(headerSize, 0, result, 0, 4);
                Buffer.BlockCopy(headerBytes, 0, result, 4, headerBytes.Length);
                Buffer.BlockCopy(encryptedData, 0, result, 4 + headerBytes.Length, encryptedData.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt file {FileName}", fileName);
                throw new InvalidOperationException("File encryption failed", ex);
            }
        }

        public async Task<byte[]> DecryptFileAsync(byte[] encryptedData, string fileName)
        {
            try
            {
                // Extract header size
                var headerSize = BitConverter.ToInt32(encryptedData, 0);
                
                // Extract header
                var headerBytes = new byte[headerSize];
                Buffer.BlockCopy(encryptedData, 4, headerBytes, 0, headerSize);
                var header = JsonSerializer.Deserialize<FileEncryptionHeader>(Encoding.UTF8.GetString(headerBytes));

                if (header == null)
                    throw new InvalidOperationException("Invalid file encryption header");

                // Extract encrypted file data
                var encryptedFileData = new byte[encryptedData.Length - 4 - headerSize];
                Buffer.BlockCopy(encryptedData, 4 + headerSize, encryptedFileData, 0, encryptedFileData.Length);

                var key = GetKey("file_encryption");

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = header.IV;

                using var decryptor = aes.CreateDecryptor();
                var decryptedData = decryptor.TransformFinalBlock(encryptedFileData, 0, encryptedFileData.Length);

                // Verify integrity
                var computedHash = GenerateHash(decryptedData);
                if (computedHash != header.Hash)
                {
                    throw new InvalidOperationException("File integrity check failed");
                }

                return decryptedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt file {FileName}", fileName);
                throw new InvalidOperationException("File decryption failed", ex);
            }
        }

        public async Task<string> GenerateKeyAsync(string keyType = "AES256")
        {
            try
            {
                var keyId = Guid.NewGuid().ToString();
                byte[] key;

                switch (keyType.ToUpper())
                {
                    case "AES256":
                        key = new byte[32]; // 256 bits
                        break;
                    case "AES128":
                        key = new byte[16]; // 128 bits
                        break;
                    default:
                        key = new byte[32]; // Default to AES256
                        break;
                }

                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(key);

                _encryptionKeys[keyId] = key;
                
                _logger.LogInformation("Generated new encryption key {KeyId} of type {KeyType}", keyId, keyType);
                return keyId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate encryption key");
                throw new InvalidOperationException("Key generation failed", ex);
            }
        }

        public async Task<bool> RotateKeyAsync(string keyId)
        {
            try
            {
                if (!_encryptionKeys.ContainsKey(keyId))
                    return false;

                var oldKey = _encryptionKeys[keyId];
                var newKey = new byte[oldKey.Length];
                
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(newKey);

                _encryptionKeys[keyId] = newKey;
                
                _logger.LogInformation("Rotated encryption key {KeyId}", keyId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rotate key {KeyId}", keyId);
                return false;
            }
        }

        public async Task<bool> IsKeyValidAsync(string keyId)
        {
            return _encryptionKeys.ContainsKey(keyId) || keyId == "default";
        }

        public string GenerateHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        public bool VerifyHash(byte[] data, string hash)
        {
            var computedHash = GenerateHash(data);
            return computedHash == hash;
        }

        #region Private Methods

        private string GenerateDefaultKey()
        {
            var key = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
            return Convert.ToBase64String(key);
        }

        private void InitializeFieldKeys()
        {
            var fieldTypes = new[] { "email", "phone", "ssn", "credit_card", "bank_account", "personal_info" };
            
            foreach (var fieldType in fieldTypes)
            {
                var configKey = $"Security:Encryption:FieldKeys:{fieldType}";
                var keyString = _configuration[configKey];
                
                if (string.IsNullOrEmpty(keyString))
                {
                    // Generate a new key for this field type
                    var key = new byte[32];
                    using var rng = RandomNumberGenerator.Create();
                    rng.GetBytes(key);
                    _fieldKeys[fieldType] = key;
                    
                    _logger.LogWarning("Generated new field key for {FieldType}. Consider storing in configuration.", fieldType);
                }
                else
                {
                    _fieldKeys[fieldType] = Convert.FromBase64String(keyString);
                }
            }
        }

        private byte[] GetKey(string keyId)
        {
            if (keyId == "default")
            {
                return Convert.FromBase64String(_defaultKey);
            }

            if (_encryptionKeys.TryGetValue(keyId, out var key))
            {
                return key;
            }

            // For file encryption, use a derived key
            if (keyId == "file_encryption")
            {
                var derivedKey = new byte[32];
                var sourceKey = Convert.FromBase64String(_defaultKey);
                var info = Encoding.UTF8.GetBytes("file_encryption");
                
                using var hmac = new HMACSHA256(sourceKey);
                var derived = hmac.ComputeHash(info);
                Buffer.BlockCopy(derived, 0, derivedKey, 0, Math.Min(32, derived.Length));
                
                return derivedKey;
            }

            throw new InvalidOperationException($"Encryption key not found: {keyId}");
        }

        private byte[] GetFieldKey(string fieldType)
        {
            if (_fieldKeys.TryGetValue(fieldType, out var key))
            {
                return key;
            }

            throw new InvalidOperationException($"Field encryption key not found: {fieldType}");
        }

        private byte[] GenerateIV()
        {
            var iv = new byte[16]; // AES block size
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(iv);
            return iv;
        }

        private async Task<string> EncryptWithKeyAsync(string plaintext, byte[] key, string keyId)
        {
            var iv = GenerateIV();

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

            var result = new EncryptedData
            {
                IV = Convert.ToBase64String(iv),
                Data = Convert.ToBase64String(ciphertextBytes),
                KeyId = keyId,
                Timestamp = DateTime.UtcNow
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result)));
        }

        private async Task<string> DecryptWithKeyAsync(string ciphertext, byte[] key)
        {
            var encryptedJson = Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
            var encryptedData = JsonSerializer.Deserialize<EncryptedData>(encryptedJson);

            if (encryptedData == null)
                throw new InvalidOperationException("Invalid encrypted data format");

            var iv = Convert.FromBase64String(encryptedData.IV);
            var data = Convert.FromBase64String(encryptedData.Data);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(data, 0, data.Length);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        #endregion

        #region Data Models

        private class EncryptedData
        {
            public string IV { get; set; } = string.Empty;
            public string Data { get; set; } = string.Empty;
            public string KeyId { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }

        private class FileEncryptionHeader
        {
            public byte[] IV { get; set; } = Array.Empty<byte>();
            public string FileName { get; set; } = string.Empty;
            public int OriginalSize { get; set; }
            public DateTime EncryptedAt { get; set; }
            public string Hash { get; set; } = string.Empty;
        }

        #endregion
    }
}
