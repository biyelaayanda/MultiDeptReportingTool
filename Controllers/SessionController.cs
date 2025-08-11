using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Services.Interfaces;
using MultiDeptReportingTool.Attributes;

namespace MultiDeptReportingTool.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly ISessionManagementService _sessionService;
        private readonly ILogger<SessionController> _logger;

        public SessionController(
            ISessionManagementService sessionService,
            ILogger<SessionController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        [HttpGet("active")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult<List<ActiveSessionDto>>> GetActiveSessions()
        {
            try
            {
                var userId = GetCurrentUserId();
                var sessions = await _sessionService.GetActiveSessionsAsync(userId);
                
                // Mark current session
                var currentSessionId = GetCurrentSessionId();
                foreach (var session in sessions.Where(s => s.SessionId == currentSessionId))
                {
                    session.IsCurrent = true;
                }

                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active sessions for user");
                return StatusCode(500, "An error occurred while retrieving sessions");
            }
        }

        [HttpPost("terminate/{sessionId}")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult> TerminateSession(string sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var currentSessionId = GetCurrentSessionId();

                // Verify session belongs to current user
                var sessionDetails = await _sessionService.GetSessionDetailsAsync(sessionId);
                if (sessionDetails == null || sessionDetails.UserId != userId)
                {
                    return NotFound("Session not found");
                }

                // Prevent terminating current session
                if (sessionId == currentSessionId)
                {
                    return BadRequest("Cannot terminate current session. Use logout instead.");
                }

                var result = await _sessionService.TerminateSessionAsync(sessionId, "User terminated session");
                
                if (result)
                {
                    return Ok(new { message = "Session terminated successfully" });
                }

                return BadRequest("Failed to terminate session");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
                return StatusCode(500, "An error occurred while terminating the session");
            }
        }

        [HttpPost("terminate-others")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult> TerminateOtherSessions()
        {
            try
            {
                var userId = GetCurrentUserId();
                var currentSessionId = GetCurrentSessionId();

                var terminatedCount = await _sessionService.TerminateOtherSessionsAsync(
                    userId, currentSessionId, "User terminated other sessions");

                return Ok(new { 
                    message = $"{terminatedCount} other sessions terminated successfully",
                    terminatedCount 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating other sessions for user");
                return StatusCode(500, "An error occurred while terminating sessions");
            }
        }

        [HttpGet("current")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult<SessionDto>> GetCurrentSession()
        {
            try
            {
                var sessionId = GetCurrentSessionId();
                var session = await _sessionService.GetSessionDetailsAsync(sessionId);

                if (session == null)
                {
                    return NotFound("Current session not found");
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current session");
                return StatusCode(500, "An error occurred while retrieving session details");
            }
        }

        [HttpPost("extend")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult> ExtendCurrentSession()
        {
            try
            {
                var sessionId = GetCurrentSessionId();
                var result = await _sessionService.ExtendSessionAsync(sessionId);

                if (result)
                {
                    return Ok(new { message = "Session extended successfully" });
                }

                return BadRequest("Failed to extend session");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending current session");
                return StatusCode(500, "An error occurred while extending the session");
            }
        }

        [HttpGet("activity/{sessionId}")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult<List<SessionActivityDto>>> GetSessionActivity(
            string sessionId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Verify session belongs to current user
                var sessionDetails = await _sessionService.GetSessionDetailsAsync(sessionId);
                if (sessionDetails == null || sessionDetails.UserId != userId)
                {
                    return NotFound("Session not found");
                }

                var activities = await _sessionService.GetSessionActivityAsync(sessionId, pageNumber, pageSize);
                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session activity for {SessionId}", sessionId);
                return StatusCode(500, "An error occurred while retrieving session activity");
            }
        }

        [HttpPost("device-fingerprint")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult<string>> GenerateDeviceFingerprint([FromBody] DeviceFingerprintDto fingerprintData)
        {
            try
            {
                var userId = GetCurrentUserId();
                var fingerprint = await _sessionService.GenerateDeviceFingerprintAsync(fingerprintData, userId);
                
                return Ok(new { fingerprint });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating device fingerprint for user");
                return StatusCode(500, "An error occurred while generating device fingerprint");
            }
        }

        [HttpPost("trust-device")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult> TrustCurrentDevice()
        {
            try
            {
                var userId = GetCurrentUserId();
                var sessionId = GetCurrentSessionId();
                var sessionDetails = await _sessionService.GetSessionDetailsAsync(sessionId);

                if (sessionDetails?.DeviceFingerprint == null)
                {
                    return BadRequest("Device fingerprint not available");
                }

                var result = await _sessionService.TrustDeviceAsync(sessionDetails.DeviceFingerprint, userId);

                if (result)
                {
                    return Ok(new { message = "Device trusted successfully" });
                }

                return BadRequest("Failed to trust device");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error trusting device for user");
                return StatusCode(500, "An error occurred while trusting the device");
            }
        }

        [HttpGet("configuration")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult<SessionConfigurationDto>> GetSessionConfiguration()
        {
            try
            {
                var userId = GetCurrentUserId();
                var config = await _sessionService.GetUserSessionConfigAsync(userId);
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session configuration for user");
                return StatusCode(500, "An error occurred while retrieving session configuration");
            }
        }

        [HttpPut("configuration")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult> UpdateSessionConfiguration([FromBody] SessionConfigurationDto config)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _sessionService.UpdateUserSessionConfigAsync(userId, config);

                if (result)
                {
                    return Ok(new { message = "Session configuration updated successfully" });
                }

                return BadRequest("Failed to update session configuration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session configuration for user");
                return StatusCode(500, "An error occurred while updating session configuration");
            }
        }

        [HttpGet("requires-mfa-verification")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult<bool>> RequiresMfaVerification()
        {
            try
            {
                var sessionId = GetCurrentSessionId();
                var requiresMfa = await _sessionService.RequiresMfaReverificationAsync(sessionId);
                
                return Ok(new { requiresMfaVerification = requiresMfa });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking MFA verification requirement");
                return StatusCode(500, "An error occurred while checking MFA requirements");
            }
        }

        [HttpPost("update-mfa-verification")]
        [RequirePermission("ManageOwnSessions")]
        public async Task<ActionResult> UpdateMfaVerification()
        {
            try
            {
                var sessionId = GetCurrentSessionId();
                await _sessionService.UpdateMfaVerificationAsync(sessionId);
                
                return Ok(new { message = "MFA verification timestamp updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating MFA verification timestamp");
                return StatusCode(500, "An error occurred while updating MFA verification");
            }
        }

        // Admin endpoints
        [HttpGet("statistics")]
        [RequirePermission("ViewSystemStatistics")]
        public async Task<ActionResult<SessionStatisticsDto>> GetSessionStatistics()
        {
            try
            {
                var statistics = await _sessionService.GetSessionStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session statistics");
                return StatusCode(500, "An error occurred while retrieving session statistics");
            }
        }

        [HttpPost("cleanup-expired")]
        [RequirePermission("ManageSystemSessions")]
        public async Task<ActionResult> CleanupExpiredSessions()
        {
            try
            {
                var cleanedCount = await _sessionService.CleanupExpiredSessionsAsync();
                return Ok(new { 
                    message = $"{cleanedCount} expired sessions cleaned up",
                    cleanedCount 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired sessions");
                return StatusCode(500, "An error occurred while cleaning up expired sessions");
            }
        }

        [HttpPost("force-logout-suspicious")]
        [RequirePermission("ManageSystemSessions")]
        public async Task<ActionResult> ForceLogoutSuspiciousSessions()
        {
            try
            {
                var loggedOutCount = await _sessionService.ForceLogoutSuspiciousSessionsAsync();
                return Ok(new { 
                    message = $"{loggedOutCount} suspicious sessions logged out",
                    loggedOutCount 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forcing logout of suspicious sessions");
                return StatusCode(500, "An error occurred while logging out suspicious sessions");
            }
        }

        [HttpPost("admin/terminate-user-sessions/{userId}")]
        [RequirePermission("ManageSystemSessions")]
        public async Task<ActionResult> TerminateAllUserSessions(int userId, [FromBody] string reason = "Admin action")
        {
            try
            {
                var terminatedCount = await _sessionService.TerminateAllUserSessionsAsync(userId, reason);
                return Ok(new { 
                    message = $"{terminatedCount} sessions terminated for user {userId}",
                    terminatedCount 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating all sessions for user {UserId}", userId);
                return StatusCode(500, "An error occurred while terminating user sessions");
            }
        }

        [HttpPost("admin/block-device")]
        [RequirePermission("ManageSystemSessions")]
        public async Task<ActionResult> BlockDevice([FromBody] BlockDeviceRequest request)
        {
            try
            {
                var result = await _sessionService.BlockDeviceAsync(request.Fingerprint, request.UserId, request.Reason);
                
                if (result)
                {
                    return Ok(new { message = "Device blocked successfully" });
                }

                return BadRequest("Failed to block device");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking device");
                return StatusCode(500, "An error occurred while blocking the device");
            }
        }

        // Helper methods
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }

        private string GetCurrentSessionId()
        {
            // Session ID should be stored in a custom claim or extracted from the JWT token
            var sessionIdClaim = User.FindFirst("SessionId")?.Value;
            if (string.IsNullOrEmpty(sessionIdClaim))
            {
                throw new UnauthorizedAccessException("Session ID not found in token");
            }
            return sessionIdClaim;
        }
    }

    // DTOs for admin operations
    public class BlockDeviceRequest
    {
        public string Fingerprint { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
