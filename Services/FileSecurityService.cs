using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MultiDeptReportingTool.Services.Interfaces;

namespace MultiDeptReportingTool.Services
{
    public class FileSecurityService : IFileSecurityService
    {
        private readonly ILogger<FileSecurityService> _logger;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        // Known file signatures for validation
        private static readonly Dictionary<string, byte[][]> FileSignatures = new()
        {
            { ".pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }, // %PDF
            { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".zip", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new byte[] { 0x50, 0x4B, 0x05, 0x06 } } },
            { ".docx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".xlsx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".pptx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".txt", new[] { new byte[] { 0xEF, 0xBB, 0xBF }, new byte[] { 0xFF, 0xFE }, new byte[] { 0xFE, 0xFF } } },
            { ".csv", new[] { new byte[] { 0xEF, 0xBB, 0xBF } } }
        };

        // Dangerous file extensions that should always be blocked
        private static readonly string[] HighRiskExtensions = 
        {
            ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".vbe", ".js", ".jse",
            ".ws", ".wsf", ".wsc", ".wsh", ".ps1", ".ps1xml", ".ps2", ".ps2xml", ".psc1", 
            ".psc2", ".msh", ".msh1", ".msh2", ".mshxml", ".msh1xml", ".msh2xml", ".scf",
            ".lnk", ".inf", ".reg", ".app", ".application", ".gadget", ".msi", ".msp",
            ".mst", ".jar", ".hta", ".cpl", ".msc", ".ocx", ".dll", ".sys"
        };

        public FileSecurityService(
            ILogger<FileSecurityService> logger,
            IAuditService auditService,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _logger = logger;
            _auditService = auditService;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<FileValidationResult> ValidateFileAsync(IFormFile file, FileUploadOptions? options = null)
        {
            options ??= GetDefaultOptions();
            
            using var stream = file.OpenReadStream();
            return await ValidateFileAsync(stream, file.FileName, file.ContentType, options);
        }

        public async Task<FileValidationResult> ValidateFileAsync(Stream fileStream, string fileName, string contentType, FileUploadOptions? options = null)
        {
            options ??= GetDefaultOptions();
            var result = new FileValidationResult
            {
                FileSizeBytes = fileStream.Length
            };

            try
            {
                // 1. File size validation
                if (fileStream.Length > options.MaxFileSizeBytes)
                {
                    result.Violations.Add($"File size ({fileStream.Length} bytes) exceeds maximum allowed size ({options.MaxFileSizeBytes} bytes)");
                    result.IsValid = false;
                }

                // 2. File extension validation
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                
                // Check blocked extensions
                if (options.BlockedExtensions.Contains(extension) || HighRiskExtensions.Contains(extension))
                {
                    result.Violations.Add($"File extension '{extension}' is not allowed for security reasons");
                    result.IsValid = false;
                    result.RiskLevel = SecurityRisk.Critical;
                }

                // Check allowed extensions if specified
                if (options.AllowedExtensions.Length > 0 && !IsAllowedFileType(fileName, contentType, options.AllowedExtensions))
                {
                    result.Violations.Add($"File extension '{extension}' is not in the allowed list");
                    result.IsValid = false;
                }

                // 3. MIME type validation
                if (options.AllowedMimeTypes.Length > 0 && !IsAllowedMimeType(contentType, options.AllowedMimeTypes))
                {
                    result.Violations.Add($"MIME type '{contentType}' is not allowed");
                    result.IsValid = false;
                }

                // 4. File signature validation
                if (options.ValidateFileSignature)
                {
                    var isValidSignature = await ValidateFileSignatureAsync(fileStream, fileName);
                    if (!isValidSignature)
                    {
                        result.Violations.Add("File signature does not match the file extension");
                        result.IsValid = false;
                        result.RiskLevel = SecurityRisk.High;
                    }
                }

                // 5. Calculate file hash
                fileStream.Position = 0;
                result.FileHash = CalculateFileHash(fileStream);

                // 6. Extract metadata
                if (options.ExtractMetadata)
                {
                    fileStream.Position = 0;
                    result.Metadata = await ExtractFileMetadataAsync(fileStream, fileName);
                    
                    // Check for potentially dangerous metadata
                    if (result.Metadata.HasMacros)
                    {
                        result.Warnings.Add("File contains macros which may pose security risks");
                        result.RiskLevel = (SecurityRisk)Math.Max((int)result.RiskLevel, (int)SecurityRisk.Medium);
                    }

                    if (result.Metadata.HasEmbeddedObjects)
                    {
                        result.Warnings.Add("File contains embedded objects");
                        result.RiskLevel = (SecurityRisk)Math.Max((int)result.RiskLevel, (int)SecurityRisk.Medium);
                    }
                }

                // 7. Malware scanning
                if (options.RequireVirusScanning && result.IsValid)
                {
                    fileStream.Position = 0;
                    var isSafe = await ScanForMalwareAsync(fileStream, fileName);
                    result.IsSafe = isSafe;
                    
                    if (!isSafe)
                    {
                        result.Violations.Add("File failed malware scan");
                        result.IsValid = false;
                        result.RiskLevel = SecurityRisk.Critical;
                    }
                }

                // 8. Overall risk assessment
                if (result.RiskLevel > options.MaxAllowedRisk)
                {
                    result.Violations.Add($"File risk level ({result.RiskLevel}) exceeds maximum allowed ({options.MaxAllowedRisk})");
                    result.IsValid = false;
                }

                // Final validation
                result.IsValid = result.IsValid && result.Violations.Count == 0;
                result.IsSafe = result.IsSafe && result.IsValid;

                // Log the validation result
                await _auditService.LogSecurityEventAsync(
                    action: "FILE_VALIDATION",
                    resource: "FileUpload",
                    isSuccess: result.IsValid,
                    details: $"File: {fileName}, Valid: {result.IsValid}, Safe: {result.IsSafe}, Risk: {result.RiskLevel}",
                    failureReason: result.Violations.Count > 0 ? string.Join("; ", result.Violations) : null
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file {FileName}", fileName);
                result.Violations.Add("File validation failed due to internal error");
                result.IsValid = false;
                result.IsSafe = false;
                return result;
            }
        }

        public async Task<bool> ScanForMalwareAsync(Stream fileStream, string fileName)
        {
            try
            {
                // 1. Simple heuristic scanning
                var suspiciousPatterns = new[]
                {
                    Encoding.ASCII.GetBytes("cmd.exe"),
                    Encoding.ASCII.GetBytes("powershell"),
                    Encoding.ASCII.GetBytes("exec("),
                    Encoding.ASCII.GetBytes("eval("),
                    Encoding.ASCII.GetBytes("<script"),
                    Encoding.ASCII.GetBytes("javascript:"),
                    Encoding.ASCII.GetBytes("vbscript:"),
                    Encoding.ASCII.GetBytes("data:text/html"),
                    Encoding.UTF8.GetBytes("﻿<?xml"),
                    Encoding.ASCII.GetBytes("CreateObject")
                };

                fileStream.Position = 0;
                var buffer = new byte[Math.Min(1024 * 1024, fileStream.Length)]; // Read first 1MB
                var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

                foreach (var pattern in suspiciousPatterns)
                {
                    if (ContainsPattern(buffer, pattern, bytesRead))
                    {
                        _logger.LogWarning("Suspicious pattern detected in file {FileName}", fileName);
                        return false;
                    }
                }

                // 2. Check for double extensions (e.g., file.txt.exe)
                var parts = fileName.Split('.');
                if (parts.Length > 2)
                {
                    for (int i = 1; i < parts.Length - 1; i++)
                    {
                        if (HighRiskExtensions.Contains($".{parts[i].ToLowerInvariant()}"))
                        {
                            _logger.LogWarning("Double extension detected in file {FileName}", fileName);
                            return false;
                        }
                    }
                }

                // 3. VirusTotal integration (if API key is configured)
                var fileHash = CalculateFileHash(fileStream);
                if (!string.IsNullOrEmpty(_configuration["VirusTotal:ApiKey"]))
                {
                    var virusTotalResult = await CheckVirusTotalAsync(fileHash);
                    if (!virusTotalResult)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file {FileName} for malware", fileName);
                return false; // Fail secure
            }
        }

        public async Task<bool> ValidateFileSignatureAsync(Stream fileStream, string fileName)
        {
            try
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                
                if (!FileSignatures.ContainsKey(extension))
                {
                    // If we don't have a signature for this extension, allow it
                    return true;
                }

                fileStream.Position = 0;
                var buffer = new byte[16]; // Read first 16 bytes for signature check
                var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

                var signatures = FileSignatures[extension];
                foreach (var signature in signatures)
                {
                    if (bytesRead >= signature.Length)
                    {
                        var matches = true;
                        for (int i = 0; i < signature.Length; i++)
                        {
                            if (buffer[i] != signature[i])
                            {
                                matches = false;
                                break;
                            }
                        }
                        if (matches)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file signature for {FileName}", fileName);
                return false;
            }
        }

        public bool IsAllowedFileType(string fileName, string contentType, string[] allowedExtensions)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        public bool IsAllowedMimeType(string contentType, string[] allowedMimeTypes)
        {
            return allowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
        }

        public string CalculateFileHash(Stream fileStream)
        {
            fileStream.Position = 0;
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(fileStream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public async Task<bool> CheckVirusTotalAsync(string fileHash)
        {
            try
            {
                var apiKey = _configuration["VirusTotal:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return true; // If no API key, assume safe
                }

                var url = $"https://www.virustotal.com/vtapi/v2/file/report?apikey={apiKey}&resource={fileHash}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<VirusTotalResponse>(content);
                    
                    if (result?.ResponseCode == 1 && result.Positives > 0)
                    {
                        _logger.LogWarning("File hash {FileHash} flagged by {Positives}/{Total} VirusTotal engines", 
                            fileHash, result.Positives, result.Total);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking VirusTotal for hash {FileHash}", fileHash);
                return true; // If check fails, assume safe to avoid blocking legitimate files
            }
        }

        public async Task QuarantineFileAsync(string filePath, string reason)
        {
            try
            {
                var quarantineDir = Path.Combine(_configuration["FileUpload:QuarantinePath"] ?? "quarantine", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                Directory.CreateDirectory(quarantineDir);

                var quarantinePath = Path.Combine(quarantineDir, $"{Guid.NewGuid()}{Path.GetExtension(filePath)}");
                File.Move(filePath, quarantinePath);

                var metadataPath = quarantinePath + ".metadata";
                var metadata = new
                {
                    OriginalPath = filePath,
                    QuarantineTime = DateTime.UtcNow,
                    Reason = reason,
                    FileHash = CalculateFileHash(File.OpenRead(quarantinePath))
                };

                await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

                await _auditService.LogSecurityEventAsync(
                    action: "FILE_QUARANTINED",
                    resource: "FileUpload",
                    details: $"File quarantined: {filePath} -> {quarantinePath}, Reason: {reason}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error quarantining file {FilePath}", filePath);
            }
        }

        public async Task<FileMetadata> ExtractFileMetadataAsync(Stream fileStream, string fileName)
        {
            var metadata = new FileMetadata
            {
                FileName = fileName,
                Size = fileStream.Length,
                FileExtension = Path.GetExtension(fileName).ToLowerInvariant()
            };

            try
            {
                // Basic metadata extraction
                // For production, consider using libraries like DocumentFormat.OpenXml for Office files
                // or other specialized libraries for different file types

                if (metadata.FileExtension == ".pdf")
                {
                    await ExtractPdfMetadataAsync(fileStream, metadata);
                }
                else if (metadata.FileExtension is ".docx" or ".xlsx" or ".pptx")
                {
                    await ExtractOfficeMetadataAsync(fileStream, metadata);
                }

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting metadata from {FileName}", fileName);
                return metadata;
            }
        }

        private async Task ExtractPdfMetadataAsync(Stream fileStream, FileMetadata metadata)
        {
            // Simple PDF metadata extraction
            // In production, use a proper PDF library like iTextSharp or PdfSharp
            fileStream.Position = 0;
            var buffer = new byte[1024];
            var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
            var content = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            if (content.Contains("/JavaScript") || content.Contains("/JS"))
            {
                metadata.HasEmbeddedObjects = true;
            }
        }

        private async Task ExtractOfficeMetadataAsync(Stream fileStream, FileMetadata metadata)
        {
            // Simple Office document metadata extraction
            // In production, use DocumentFormat.OpenXml library
            fileStream.Position = 0;
            var buffer = new byte[1024];
            var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
            var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (content.Contains("vbaProject") || content.Contains("macros"))
            {
                metadata.HasMacros = true;
            }

            if (content.Contains("oleObject") || content.Contains("embedded"))
            {
                metadata.HasEmbeddedObjects = true;
            }
        }

        private bool ContainsPattern(byte[] buffer, byte[] pattern, int bufferLength)
        {
            for (int i = 0; i <= bufferLength - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return true;
                }
            }
            return false;
        }

        private FileUploadOptions GetDefaultOptions()
        {
            return new FileUploadOptions
            {
                MaxFileSizeBytes = _configuration.GetValue<long>("FileUpload:MaxSizeBytes", 10 * 1024 * 1024),
                AllowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>(),
                AllowedMimeTypes = _configuration.GetSection("FileUpload:AllowedMimeTypes").Get<string[]>() ?? Array.Empty<string>(),
                RequireVirusScanning = _configuration.GetValue<bool>("FileUpload:RequireVirusScanning", true),
                ValidateFileSignature = _configuration.GetValue<bool>("FileUpload:ValidateFileSignature", true),
                ExtractMetadata = _configuration.GetValue<bool>("FileUpload:ExtractMetadata", true),
                QuarantineSuspiciousFiles = _configuration.GetValue<bool>("FileUpload:QuarantineSuspiciousFiles", true)
            };
        }

        private class VirusTotalResponse
        {
            public int ResponseCode { get; set; }
            public int Positives { get; set; }
            public int Total { get; set; }
        }
    }
}
