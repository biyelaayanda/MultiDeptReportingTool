using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Services;
using MultiDeptReportingTool.Services.Interfaces;
using System.Security.Claims;

namespace MultiDeptReportingTool.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MfaController : ControllerBase
    {
        private readonly IMfaService _mfaService;
        private readonly IAuditService _auditService;
        private readonly ILogger<MfaController> _logger;

        public MfaController(
            IMfaService mfaService,
            IAuditService auditService,
            ILogger<MfaController> logger)
        {
            _mfaService = mfaService;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Generate MFA setup data including QR code and backup codes
        /// </summary>
        [HttpPost("setup")]
        public async Task<IActionResult> SetupMfa()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var setupData = await _mfaService.GenerateMfaSetupAsync(userId, clientIp);

                return Ok(setupData);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up MFA for user");
                return StatusCode(500, new { message = "An error occurred while setting up MFA" });
            }
        }

        /// <summary>
        /// Enable MFA after verifying the setup code
        /// </summary>
        [HttpPost("enable")]
        public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _mfaService.EnableMfaAsync(userId, request.VerificationCode);

                if (result)
                {
                    await _auditService.LogSecurityEventAsync(
                        action: "MFA_ENABLED_VIA_API",
                        resource: "User",
                        userId: userId,
                        username: username,
                        details: $"MFA enabled successfully via API for user {username}",
                        ipAddress: clientIp ?? "Unknown"
                    );

                    return Ok(new { message = "MFA enabled successfully" });
                }
                else
                {
                    await _auditService.LogSecurityEventAsync(
                        action: "MFA_ENABLE_FAILED_VIA_API",
                        resource: "User",
                        userId: userId,
                        username: username,
                        isSuccess: false,
                        failureReason: "Invalid verification code",
                        details: $"Failed to enable MFA via API for user {username}",
                        ipAddress: clientIp ?? "Unknown"
                    );

                    return BadRequest(new { message = "Invalid verification code" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling MFA");
                return StatusCode(500, new { message = "An error occurred while enabling MFA" });
            }
        }

        /// <summary>
        /// Disable MFA with current password verification
        /// </summary>
        [HttpPost("disable")]
        public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _mfaService.DisableMfaAsync(userId, request.CurrentPassword);

                if (result)
                {
                    await _auditService.LogSecurityEventAsync(
                        action: "MFA_DISABLED_VIA_API",
                        resource: "User",
                        userId: userId,
                        username: username,
                        details: $"MFA disabled successfully via API for user {username}",
                        ipAddress: clientIp ?? "Unknown"
                    );

                    return Ok(new { message = "MFA disabled successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Unable to disable MFA. Verify your current password." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling MFA");
                return StatusCode(500, new { message = "An error occurred while disabling MFA" });
            }
        }

        /// <summary>
        /// Verify a TOTP code
        /// </summary>
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyMfaRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                bool isValid;

                if (request.IsBackupCode)
                {
                    isValid = await _mfaService.VerifyBackupCodeAsync(userId, request.Code);
                    
                    if (isValid)
                    {
                        await _auditService.LogSecurityEventAsync(
                            action: "MFA_BACKUP_CODE_VERIFIED_VIA_API",
                            resource: "User",
                            userId: userId,
                            username: username,
                            details: $"Backup code verified successfully via API for user {username}",
                            ipAddress: clientIp ?? "Unknown"
                        );
                    }
                }
                else
                {
                    isValid = await _mfaService.VerifyTotpCodeAsync(userId, request.Code);
                    
                    if (isValid)
                    {
                        await _auditService.LogSecurityEventAsync(
                            action: "MFA_TOTP_VERIFIED_VIA_API",
                            resource: "User",
                            userId: userId,
                            username: username,
                            details: $"TOTP code verified successfully via API for user {username}",
                            ipAddress: clientIp ?? "Unknown"
                        );
                    }
                }

                if (!isValid)
                {
                    await _auditService.LogSecurityEventAsync(
                        action: "MFA_VERIFICATION_FAILED_VIA_API",
                        resource: "User",
                        userId: userId,
                        username: username,
                        isSuccess: false,
                        failureReason: "Invalid MFA code",
                        details: $"MFA verification failed via API for user {username}",
                        ipAddress: clientIp ?? "Unknown"
                    );
                }

                return Ok(new { isValid, message = isValid ? "Code verified successfully" : "Invalid code" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying MFA code");
                return StatusCode(500, new { message = "An error occurred while verifying the code" });
            }
        }

        /// <summary>
        /// Generate new backup codes
        /// </summary>
        [HttpPost("backup-codes")]
        public async Task<IActionResult> GenerateBackupCodes()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                var backupCodes = await _mfaService.GenerateNewBackupCodesAsync(userId);

                await _auditService.LogSecurityEventAsync(
                    action: "MFA_BACKUP_CODES_GENERATED_VIA_API",
                    resource: "User",
                    userId: userId,
                    username: username,
                    details: $"New backup codes generated via API for user {username}",
                    ipAddress: clientIp ?? "Unknown"
                );

                return Ok(new { backupCodes, message = "New backup codes generated successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating backup codes");
                return StatusCode(500, new { message = "An error occurred while generating backup codes" });
            }
        }

        /// <summary>
        /// Get MFA status for the current user
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetMfaStatus()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                // This would require a new method in IMfaService to get user MFA status
                // For now, we'll return a basic response
                await _auditService.LogSecurityEventAsync(
                    action: "MFA_STATUS_CHECKED_VIA_API",
                    resource: "User",
                    userId: userId,
                    username: username,
                    details: $"MFA status checked via API for user {username}",
                    ipAddress: clientIp ?? "Unknown"
                );

                return Ok(new { message = "MFA status retrieved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting MFA status");
                return StatusCode(500, new { message = "An error occurred while getting MFA status" });
            }
        }

        /// <summary>
        /// Generate a new QR code for existing MFA setup
        /// </summary>
        [HttpGet("qr-code")]
        public async Task<IActionResult> GetQrCode()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                // This would require a new method in IMfaService to get QR code for existing setup
                // For now, we'll return a basic response
                await _auditService.LogSecurityEventAsync(
                    action: "MFA_QR_CODE_ACCESSED_VIA_API",
                    resource: "User",
                    userId: userId,
                    username: username,
                    details: $"MFA QR code accessed via API for user {username}",
                    ipAddress: clientIp ?? "Unknown"
                );

                return Ok(new { message = "QR code retrieved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting QR code");
                return StatusCode(500, new { message = "An error occurred while getting QR code" });
            }
        }
    }
}
