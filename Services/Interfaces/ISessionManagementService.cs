using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Services.Interfaces
{
    public interface ISessionManagementService
    {
        /// <summary>
        /// Create a new user session
        /// </summary>
        Task<UserSession> CreateSessionAsync(int userId, string deviceFingerprint, string ipAddress, 
            string userAgent, bool rememberMe = false, string? location = null);

        /// <summary>
        /// Validate and refresh an existing session
        /// </summary>
        Task<UserSession?> ValidateSessionAsync(string sessionId, string ipAddress, string userAgent);

        /// <summary>
        /// Terminate a specific session
        /// </summary>
        Task<bool> TerminateSessionAsync(string sessionId, string reason = "User logout");

        /// <summary>
        /// Terminate all sessions for a user except the current one
        /// </summary>
        Task<int> TerminateOtherSessionsAsync(int userId, string currentSessionId, string reason = "User requested");

        /// <summary>
        /// Terminate all sessions for a user
        /// </summary>
        Task<int> TerminateAllUserSessionsAsync(int userId, string reason = "Admin action");

        /// <summary>
        /// Get all active sessions for a user
        /// </summary>
        Task<List<ActiveSessionDto>> GetActiveSessionsAsync(int userId);

        /// <summary>
        /// Get session details by session ID
        /// </summary>
        Task<SessionDto?> GetSessionDetailsAsync(string sessionId);

        /// <summary>
        /// Get session statistics for admin dashboard
        /// </summary>
        Task<SessionStatisticsDto> GetSessionStatisticsAsync();

        /// <summary>
        /// Log session activity
        /// </summary>
        Task LogSessionActivityAsync(string sessionId, string activity, string resource, 
            string ipAddress, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Check if user has exceeded concurrent session limit
        /// </summary>
        Task<bool> HasExceededSessionLimitAsync(int userId);

        /// <summary>
        /// Clean up expired sessions
        /// </summary>
        Task<int> CleanupExpiredSessionsAsync();

        /// <summary>
        /// Generate and store device fingerprint
        /// </summary>
        Task<string> GenerateDeviceFingerprintAsync(DeviceFingerprintDto fingerprintData, int userId);

        /// <summary>
        /// Verify device fingerprint
        /// </summary>
        Task<bool> VerifyDeviceFingerprintAsync(string fingerprint, int userId);

        /// <summary>
        /// Mark device as trusted
        /// </summary>
        Task<bool> TrustDeviceAsync(string fingerprint, int userId);

        /// <summary>
        /// Block a device fingerprint
        /// </summary>
        Task<bool> BlockDeviceAsync(string fingerprint, int userId, string reason);

        /// <summary>
        /// Detect suspicious session activity
        /// </summary>
        Task<bool> DetectSuspiciousActivityAsync(string sessionId, string ipAddress, string userAgent);

        /// <summary>
        /// Get user's session configuration
        /// </summary>
        Task<SessionConfigurationDto> GetUserSessionConfigAsync(int userId);

        /// <summary>
        /// Update user's session configuration
        /// </summary>
        Task<bool> UpdateUserSessionConfigAsync(int userId, SessionConfigurationDto config);

        /// <summary>
        /// Force logout suspicious sessions
        /// </summary>
        Task<int> ForceLogoutSuspiciousSessionsAsync();

        /// <summary>
        /// Get session activity history
        /// </summary>
        Task<List<SessionActivityDto>> GetSessionActivityAsync(string sessionId, int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Extend session expiration (for remember me functionality)
        /// </summary>
        Task<bool> ExtendSessionAsync(string sessionId);

        /// <summary>
        /// Check if session requires MFA reverification
        /// </summary>
        Task<bool> RequiresMfaReverificationAsync(string sessionId);

        /// <summary>
        /// Update MFA verification timestamp for session
        /// </summary>
        Task UpdateMfaVerificationAsync(string sessionId);
    }
}
