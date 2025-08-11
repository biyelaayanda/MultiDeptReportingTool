using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.Services.Interfaces;
using MultiDeptReportingTool.DTOs.Export;

namespace MultiDeptReportingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DataProtectionController : ControllerBase
    {
        private readonly IEncryptionService _encryptionService;
        private readonly IApiSecurityService _apiSecurityService;
        private readonly ILogger<DataProtectionController> _logger;

        public DataProtectionController(
            IEncryptionService encryptionService,
            IApiSecurityService apiSecurityService,
            ILogger<DataProtectionController> logger)
        {
            _encryptionService = encryptionService;
            _apiSecurityService = apiSecurityService;
            _logger = logger;
        }

        /// <summary>
        /// Test endpoint for field-level encryption
        /// </summary>
        [HttpPost("encrypt-field")]
        public async Task<IActionResult> EncryptField([FromBody] EncryptFieldRequest request)
        {
            try
            {
                var encrypted = await _encryptionService.EncryptFieldAsync(request.Value, request.FieldType);
                return Ok(new { encrypted = encrypted, original = request.Value });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt field");
                return BadRequest(new { error = "Encryption failed" });
            }
        }

        /// <summary>
        /// Test endpoint for field-level decryption
        /// </summary>
        [HttpPost("decrypt-field")]
        public async Task<IActionResult> DecryptField([FromBody] DecryptFieldRequest request)
        {
            try
            {
                var decrypted = await _encryptionService.DecryptFieldAsync(request.EncryptedValue, request.FieldType);
                return Ok(new { decrypted = decrypted });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt field");
                return BadRequest(new { error = "Decryption failed" });
            }
        }

        /// <summary>
        /// Test endpoint for input sanitization
        /// </summary>
        [HttpPost("sanitize-input")]
        public async Task<IActionResult> SanitizeInput([FromBody] SanitizeInputRequest request)
        {
            try
            {
                var sanitized = await _apiSecurityService.SanitizeInputAsync(request.Input, request.InputType);
                var isSafe = await _apiSecurityService.IsInputSafeAsync(request.Input, request.InputType);
                
                return Ok(new 
                { 
                    original = request.Input,
                    sanitized = sanitized,
                    isSafe = isSafe,
                    wasSanitized = request.Input != sanitized
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sanitize input");
                return BadRequest(new { error = "Sanitization failed" });
            }
        }

        /// <summary>
        /// Generate a sample encrypted export
        /// </summary>
        [HttpPost("test-encrypted-export")]
        public async Task<IActionResult> TestEncryptedExport()
        {
            try
            {
                var sampleData = System.Text.Encoding.UTF8.GetBytes("This is a test export file with sensitive data.");
                var encrypted = await _encryptionService.EncryptFileAsync(sampleData, "test_export.txt");
                
                return File(encrypted, "application/octet-stream", "test_export.enc");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create encrypted export");
                return BadRequest(new { error = "Export encryption failed" });
            }
        }

        /// <summary>
        /// Test request signature validation
        /// </summary>
        [HttpPost("test-signature")]
        public async Task<IActionResult> TestSignature([FromBody] object requestData)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                var requestBody = System.Text.Json.JsonSerializer.Serialize(requestData);
                var method = HttpContext.Request.Method;
                var path = HttpContext.Request.Path.Value ?? "";

                var signature = await _apiSecurityService.SignRequestAsync(requestBody, timestamp, method, path);
                
                return Ok(new 
                { 
                    signature = signature,
                    timestamp = timestamp,
                    method = method,
                    path = path,
                    body = requestBody
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate signature");
                return BadRequest(new { error = "Signature generation failed" });
            }
        }

        /// <summary>
        /// Get current API security status
        /// </summary>
        [HttpGet("security-status")]
        public async Task<IActionResult> GetSecurityStatus()
        {
            try
            {
                var clientId = HttpContext.Request.Headers["X-Client-ID"].FirstOrDefault() ?? "test-client";
                var version = _apiSecurityService.GetRequestVersion(HttpContext.Request);
                var isVersionSupported = _apiSecurityService.IsVersionSupported(version);
                
                return Ok(new 
                { 
                    apiVersion = version,
                    isVersionSupported = isVersionSupported,
                    clientId = clientId,
                    timestamp = DateTime.UtcNow,
                    requestPath = HttpContext.Request.Path.Value,
                    securityHeaders = HttpContext.Response.Headers
                        .Where(h => h.Key.StartsWith("X-") || h.Key.Contains("Security") || h.Key.Contains("Content-Security"))
                        .ToDictionary(h => h.Key, h => h.Value.ToString())
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get security status");
                return BadRequest(new { error = "Security status check failed" });
            }
        }
    }

    // DTOs for the test endpoints
    public class EncryptFieldRequest
    {
        public string Value { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
    }

    public class DecryptFieldRequest
    {
        public string EncryptedValue { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
    }

    public class SanitizeInputRequest
    {
        public string Input { get; set; } = string.Empty;
        public string InputType { get; set; } = "general";
    }
}
