using System.Security.Cryptography;
using System.Text;

namespace MultiDeptReportingTool.Services.Interfaces
{
    public interface IFileSecurityService
    {
        Task<FileValidationResult> ValidateFileAsync(IFormFile file, FileUploadOptions? options = null);
        Task<FileValidationResult> ValidateFileAsync(Stream fileStream, string fileName, string contentType, FileUploadOptions? options = null);
        Task<bool> ScanForMalwareAsync(Stream fileStream, string fileName);
        Task<bool> ValidateFileSignatureAsync(Stream fileStream, string fileName);
        bool IsAllowedFileType(string fileName, string contentType, string[] allowedExtensions);
        bool IsAllowedMimeType(string contentType, string[] allowedMimeTypes);
        string CalculateFileHash(Stream fileStream);
        Task<bool> CheckVirusTotalAsync(string fileHash);
        Task QuarantineFileAsync(string filePath, string reason);
        Task<FileMetadata> ExtractFileMetadataAsync(Stream fileStream, string fileName);
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsSafe { get; set; }
        public List<string> Violations { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public FileMetadata? Metadata { get; set; }
        public string? FileHash { get; set; }
        public long FileSizeBytes { get; set; }
        public SecurityRisk RiskLevel { get; set; } = SecurityRisk.Low;
    }

    public class FileUploadOptions
    {
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB default
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
        public string[] AllowedMimeTypes { get; set; } = Array.Empty<string>();
        public string[] BlockedExtensions { get; set; } = { ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar", ".ps1" };
        public bool RequireVirusScanning { get; set; } = true;
        public bool ValidateFileSignature { get; set; } = true;
        public bool ExtractMetadata { get; set; } = true;
        public bool QuarantineSuspiciousFiles { get; set; } = true;
        public SecurityRisk MaxAllowedRisk { get; set; } = SecurityRisk.Medium;
    }

    public class FileMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public bool HasMacros { get; set; }
        public bool HasEmbeddedObjects { get; set; }
        public bool IsPasswordProtected { get; set; }
        public string? DocumentAuthor { get; set; }
        public string? DocumentTitle { get; set; }
        public string? ApplicationName { get; set; }
    }

    public enum SecurityRisk
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}
