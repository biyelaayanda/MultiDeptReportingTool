using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.Attributes;
using MultiDeptReportingTool.Services.Interfaces;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileSecurityService _fileSecurityService;
        private readonly IAuditService _auditService;
        private readonly ILogger<FileUploadController> _logger;
        private readonly IConfiguration _configuration;

        public FileUploadController(
            IFileSecurityService fileSecurityService,
            IAuditService auditService,
            ILogger<FileUploadController> logger,
            IConfiguration configuration)
        {
            _fileSecurityService = fileSecurityService;
            _auditService = auditService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("document")]
        [RequirePermission("UploadDocuments")]
        public async Task<ActionResult> UploadDocument(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file provided");
                }

                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();

                // Configure upload options for documents
                var options = new FileUploadOptions
                {
                    MaxFileSizeBytes = 10 * 1024 * 1024, // 10MB
                    AllowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".pptx", ".txt", ".csv" },
                    AllowedMimeTypes = new[]
                    {
                        "application/pdf",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                        "text/plain",
                        "text/csv"
                    },
                    RequireVirusScanning = true,
                    ValidateFileSignature = true,
                    ExtractMetadata = true,
                    QuarantineSuspiciousFiles = true,
                    MaxAllowedRisk = SecurityRisk.Medium
                };

                // Validate the file
                var validationResult = await _fileSecurityService.ValidateFileAsync(file, options);

                if (!validationResult.IsValid || !validationResult.IsSafe)
                {
                    await _auditService.LogSecurityEventAsync(
                        action: "FILE_UPLOAD_REJECTED",
                        resource: "FileUpload",
                        userId: userId,
                        username: username,
                        isSuccess: false,
                        failureReason: string.Join("; ", validationResult.Violations),
                        details: $"File: {file.FileName}, Size: {file.Length}, Risk: {validationResult.RiskLevel}",
                        ipAddress: GetClientIpAddress()
                    );

                    return BadRequest(new
                    {
                        message = "File upload rejected",
                        violations = validationResult.Violations,
                        warnings = validationResult.Warnings,
                        riskLevel = validationResult.RiskLevel.ToString()
                    });
                }

                // Save the file securely
                var uploadDir = Path.Combine(_configuration["FileUpload:UploadPath"] ?? "uploads", "documents");
                Directory.CreateDirectory(uploadDir);

                var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadDir, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Log successful upload
                await _auditService.LogSecurityEventAsync(
                    action: "FILE_UPLOADED",
                    resource: "FileUpload",
                    userId: userId,
                    username: username,
                    isSuccess: true,
                    details: $"File: {file.FileName} -> {safeFileName}, Size: {file.Length}, Hash: {validationResult.FileHash}",
                    ipAddress: GetClientIpAddress()
                );

                return Ok(new
                {
                    message = "File uploaded successfully",
                    fileName = safeFileName,
                    originalFileName = file.FileName,
                    size = file.Length,
                    hash = validationResult.FileHash,
                    metadata = validationResult.Metadata,
                    warnings = validationResult.Warnings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, "An error occurred while uploading the file");
            }
        }

        [HttpPost("image")]
        [RequirePermission("UploadImages")]
        public async Task<ActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file provided");
                }

                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();

                // Configure upload options for images
                var options = new FileUploadOptions
                {
                    MaxFileSizeBytes = 5 * 1024 * 1024, // 5MB
                    AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" },
                    AllowedMimeTypes = new[]
                    {
                        "image/jpeg",
                        "image/png",
                        "image/gif",
                        "image/bmp",
                        "image/webp"
                    },
                    RequireVirusScanning = true,
                    ValidateFileSignature = true,
                    ExtractMetadata = false, // Images don't need metadata extraction
                    QuarantineSuspiciousFiles = true,
                    MaxAllowedRisk = SecurityRisk.Low // Stricter for images
                };

                var validationResult = await _fileSecurityService.ValidateFileAsync(file, options);

                if (!validationResult.IsValid || !validationResult.IsSafe)
                {
                    await _auditService.LogSecurityEventAsync(
                        action: "IMAGE_UPLOAD_REJECTED",
                        resource: "FileUpload",
                        userId: userId,
                        username: username,
                        isSuccess: false,
                        failureReason: string.Join("; ", validationResult.Violations),
                        details: $"Image: {file.FileName}, Size: {file.Length}",
                        ipAddress: GetClientIpAddress()
                    );

                    return BadRequest(new
                    {
                        message = "Image upload rejected",
                        violations = validationResult.Violations,
                        riskLevel = validationResult.RiskLevel.ToString()
                    });
                }

                // Save the image securely
                var uploadDir = Path.Combine(_configuration["FileUpload:UploadPath"] ?? "uploads", "images");
                Directory.CreateDirectory(uploadDir);

                var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadDir, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                await _auditService.LogSecurityEventAsync(
                    action: "IMAGE_UPLOADED",
                    resource: "FileUpload",
                    userId: userId,
                    username: username,
                    isSuccess: true,
                    details: $"Image: {file.FileName} -> {safeFileName}, Size: {file.Length}",
                    ipAddress: GetClientIpAddress()
                );

                return Ok(new
                {
                    message = "Image uploaded successfully",
                    fileName = safeFileName,
                    originalFileName = file.FileName,
                    size = file.Length,
                    hash = validationResult.FileHash
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, "An error occurred while uploading the image");
            }
        }

        [HttpPost("validate")]
        [RequirePermission("ValidateFiles")]
        public async Task<ActionResult> ValidateFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file provided");
                }

                var validationResult = await _fileSecurityService.ValidateFileAsync(file);

                return Ok(new
                {
                    isValid = validationResult.IsValid,
                    isSafe = validationResult.IsSafe,
                    riskLevel = validationResult.RiskLevel.ToString(),
                    violations = validationResult.Violations,
                    warnings = validationResult.Warnings,
                    metadata = validationResult.Metadata,
                    fileHash = validationResult.FileHash,
                    size = validationResult.FileSizeBytes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file");
                return StatusCode(500, "An error occurred while validating the file");
            }
        }

        [HttpGet("quarantine")]
        [RequirePermission("ViewQuarantinedFiles")]
        public ActionResult GetQuarantinedFiles()
        {
            try
            {
                var quarantineDir = _configuration["FileUpload:QuarantinePath"] ?? "quarantine";
                
                if (!Directory.Exists(quarantineDir))
                {
                    return Ok(new { files = new object[0] });
                }

                var quarantinedFiles = Directory.GetFiles(quarantineDir, "*.metadata", SearchOption.AllDirectories)
                    .Select(metadataFile =>
                    {
                        try
                        {
                            var metadata = System.Text.Json.JsonSerializer.Deserialize<QuarantineMetadata>(
                                System.IO.File.ReadAllText(metadataFile));
                            return new
                            {
                                originalPath = metadata?.OriginalPath,
                                quarantineTime = metadata?.QuarantineTime,
                                reason = metadata?.Reason,
                                fileHash = metadata?.FileHash
                            };
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(f => f != null)
                    .ToList();

                return Ok(new { files = quarantinedFiles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quarantined files");
                return StatusCode(500, "An error occurred while retrieving quarantined files");
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private string GetCurrentUsername()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private class QuarantineMetadata
        {
            public string? OriginalPath { get; set; }
            public DateTime QuarantineTime { get; set; }
            public string? Reason { get; set; }
            public string? FileHash { get; set; }
        }
    }
}
