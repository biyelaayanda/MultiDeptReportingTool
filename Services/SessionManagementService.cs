using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Services.Interfaces;

namespace MultiDeptReportingTool.Services
{
    public class SessionManagementService : ISessionManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<SessionManagementService> _logger;
        private readonly IConfiguration _configuration;

        public SessionManagementService(
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<SessionManagementService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<UserSession> CreateSessionAsync(int userId, string deviceFingerprint, string ipAddress, 
            string userAgent, bool rememberMe = false, string? location = null)
        {
            try
            {
                // Check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new ArgumentException("User not found");

                // Check session limits
                if (await HasExceededSessionLimitAsync(userId))
                {
                    // Terminate oldest session
                    await TerminateOldestSessionAsync(userId);
                }

                // Detect device type from user agent
                var deviceType = DetectDeviceType(userAgent);
                var deviceName = ExtractDeviceName(userAgent);

                // Calculate expiration time
                var sessionTimeout = rememberMe ? 
                    TimeSpan.FromMinutes(1440) : // 24 hours for remember me
                    TimeSpan.FromMinutes(480);    // 8 hours for normal session

                // Generate unique session ID
                var sessionId = GenerateSessionId();

                // Create session
                var session = new UserSession
                {
                    SessionId = sessionId,
                    UserId = userId,
                    DeviceFingerprint = deviceFingerprint,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    DeviceType = deviceType,
                    DeviceName = deviceName,
                    Location = location ?? await GetLocationFromIpAsync(ipAddress),
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(sessionTimeout),
                    IsActive = true,
                    IsRememberMe = rememberMe,
                    RequiresMfaVerification = user.IsMfaEnabled
                };

                _context.UserSessions.Add(session);
                await _context.SaveChangesAsync();

                // Log session creation
                await _auditService.LogSecurityEventAsync(
                    action: "SESSION_CREATED",
                    resource: "UserSession",
                    userId: userId,
                    username: user.Username,
                    details: $"New session created from {deviceType} device",
                    ipAddress: ipAddress
                );

                // Log initial activity
                await LogSessionActivityAsync(sessionId, "SESSION_START", "Authentication", ipAddress, 
                    new Dictionary<string, object>
                    {
                        { "deviceType", deviceType },
                        { "rememberMe", rememberMe },
                        { "location", location ?? "Unknown" }
                    });

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session for user {UserId}", userId);
                throw;
            }
        }

        public async Task<UserSession?> ValidateSessionAsync(string sessionId, string ipAddress, string userAgent)
        {
            try
            {
                var session = await _context.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive && !s.IsRevoked);

                if (session == null || session.ExpiresAt < DateTime.UtcNow)
                {
                    if (session != null)
                    {
                        // Mark as expired
                        session.IsActive = false;
                        session.IsRevoked = true;
                        session.RevocationReason = "Session expired";
                        session.RevokedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    return null;
                }

                // Check for suspicious activity
                var isSuspicious = await DetectSuspiciousActivityAsync(sessionId, ipAddress, userAgent);
                if (isSuspicious)
                {
                    session.IsSuspicious = true;
                    session.SuspiciousReason = "IP or User-Agent mismatch detected";
                    await _context.SaveChangesAsync();

                    await _auditService.LogSecurityEventAsync(
                        action: "SUSPICIOUS_SESSION_DETECTED",
                        resource: "UserSession",
                        userId: session.UserId,
                        username: session.User.Username,
                        isSuccess: false,
                        failureReason: "Suspicious activity detected",
                        details: $"Session {sessionId} marked as suspicious",
                        ipAddress: ipAddress
                    );
                }

                // Update last accessed time
                session.LastAccessedAt = DateTime.UtcNow;

                // Auto-extend session if close to expiration and actively used
                if (session.ExpiresAt.Subtract(DateTime.UtcNow).TotalMinutes < 30)
                {
                    await ExtendSessionAsync(sessionId);
                }

                await _context.SaveChangesAsync();

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<bool> TerminateSessionAsync(string sessionId, string reason = "User logout")
        {
            try
            {
                var session = await _context.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return false;

                session.IsActive = false;
                session.IsRevoked = true;
                session.RevocationReason = reason;
                session.RevokedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log session termination
                await _auditService.LogSecurityEventAsync(
                    action: "SESSION_TERMINATED",
                    resource: "UserSession",
                    userId: session.UserId,
                    username: session.User.Username,
                    details: $"Session terminated: {reason}",
                    ipAddress: session.IpAddress
                );

                // Log final activity
                await LogSessionActivityAsync(sessionId, "SESSION_END", "Authentication", session.IpAddress,
                    new Dictionary<string, object> { { "reason", reason } });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<int> TerminateOtherSessionsAsync(int userId, string currentSessionId, string reason = "User requested")
        {
            try
            {
                var otherSessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && s.SessionId != currentSessionId && s.IsActive)
                    .ToListAsync();

                var terminatedCount = 0;
                foreach (var session in otherSessions)
                {
                    session.IsActive = false;
                    session.IsRevoked = true;
                    session.RevocationReason = reason;
                    session.RevokedAt = DateTime.UtcNow;
                    terminatedCount++;
                }

                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);
                await _auditService.LogSecurityEventAsync(
                    action: "BULK_SESSION_TERMINATION",
                    resource: "UserSession",
                    userId: userId,
                    username: user?.Username,
                    details: $"Terminated {terminatedCount} other sessions: {reason}",
                    ipAddress: "System"
                );

                return terminatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating other sessions for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<int> TerminateAllUserSessionsAsync(int userId, string reason = "Admin action")
        {
            try
            {
                var sessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && s.IsActive)
                    .ToListAsync();

                var terminatedCount = 0;
                foreach (var session in sessions)
                {
                    session.IsActive = false;
                    session.IsRevoked = true;
                    session.RevocationReason = reason;
                    session.RevokedAt = DateTime.UtcNow;
                    terminatedCount++;
                }

                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);
                await _auditService.LogSecurityEventAsync(
                    action: "ALL_SESSIONS_TERMINATED",
                    resource: "UserSession",
                    userId: userId,
                    username: user?.Username,
                    details: $"All {terminatedCount} sessions terminated: {reason}",
                    ipAddress: "System"
                );

                return terminatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating all sessions for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<List<ActiveSessionDto>> GetActiveSessionsAsync(int userId)
        {
            try
            {
                var sessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && s.IsActive && !s.IsRevoked)
                    .OrderByDescending(s => s.LastAccessedAt)
                    .ToListAsync();

                return sessions.Select(s => new ActiveSessionDto
                {
                    SessionId = s.SessionId,
                    DeviceType = s.DeviceType,
                    DeviceName = s.DeviceName,
                    Location = s.Location,
                    IpAddress = s.IpAddress,
                    LastAccessed = s.LastAccessedAt,
                    CreatedAt = s.CreatedAt,
                    IsCurrent = false, // Will be set by the caller
                    DaysActive = (DateTime.UtcNow - s.CreatedAt).Days
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions for user {UserId}", userId);
                return new List<ActiveSessionDto>();
            }
        }

        // Helper methods
        private string GenerateSessionId()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private string DetectDeviceType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            userAgent = userAgent.ToLower();

            if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone"))
                return "Mobile";
            if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
                return "Tablet";
            
            return "Desktop";
        }

        private string ExtractDeviceName(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown Device";

            // Simple device name extraction (can be enhanced)
            if (userAgent.Contains("iPhone"))
                return "iPhone";
            if (userAgent.Contains("iPad"))
                return "iPad";
            if (userAgent.Contains("Android"))
                return "Android Device";
            if (userAgent.Contains("Windows"))
                return "Windows PC";
            if (userAgent.Contains("Macintosh"))
                return "Mac";
            if (userAgent.Contains("Linux"))
                return "Linux PC";

            return "Unknown Device";
        }

        private async Task<string> GetLocationFromIpAsync(string ipAddress)
        {
            // Placeholder for IP geolocation service
            // In production, integrate with a service like MaxMind GeoIP
            await Task.CompletedTask;
            return "Unknown Location";
        }

        // Additional required implementations...
        public async Task<SessionDto?> GetSessionDetailsAsync(string sessionId)
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return null;

            return new SessionDto
            {
                SessionId = session.SessionId,
                UserId = session.UserId,
                Username = session.User.Username,
                DeviceFingerprint = session.DeviceFingerprint,
                IpAddress = session.IpAddress,
                UserAgent = session.UserAgent,
                DeviceType = session.DeviceType,
                Location = session.Location,
                CreatedAt = session.CreatedAt,
                LastAccessedAt = session.LastAccessedAt,
                ExpiresAt = session.ExpiresAt,
                IsActive = session.IsActive
            };
        }

        public async Task<bool> HasExceededSessionLimitAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            var activeSessionCount = await _context.UserSessions
                .CountAsync(s => s.UserId == userId && s.IsActive && !s.IsRevoked);

            return activeSessionCount >= user.MaxConcurrentSessions;
        }

        private async Task TerminateOldestSessionAsync(int userId)
        {
            var oldestSession = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive && !s.IsRevoked)
                .OrderBy(s => s.LastAccessedAt)
                .FirstOrDefaultAsync();

            if (oldestSession != null)
            {
                await TerminateSessionAsync(oldestSession.SessionId, "Session limit exceeded - oldest session terminated");
            }
        }

        // Placeholder implementations for remaining interface methods
        public async Task<SessionStatisticsDto> GetSessionStatisticsAsync()
        {
            var totalActive = await _context.UserSessions.CountAsync(s => s.IsActive);
            var totalUsers = await _context.UserSessions.Where(s => s.IsActive).Select(s => s.UserId).Distinct().CountAsync();
            
            return new SessionStatisticsDto
            {
                TotalActiveSessions = totalActive,
                TotalUsers = totalUsers,
                SessionsCreatedToday = await _context.UserSessions.CountAsync(s => s.CreatedAt.Date == DateTime.UtcNow.Date),
                SessionsTerminatedToday = await _context.UserSessions.CountAsync(s => s.RevokedAt.HasValue && s.RevokedAt.Value.Date == DateTime.UtcNow.Date),
                AverageSessionDurationMinutes = 240, // Placeholder
                SuspiciousSessionsDetected = await _context.UserSessions.CountAsync(s => s.IsSuspicious),
                DeviceTypeBreakdown = new List<DeviceTypeStatDto>(),
                LocationBreakdown = new List<LocationStatDto>()
            };
        }

        public async Task LogSessionActivityAsync(string sessionId, string activity, string resource, 
            string ipAddress, Dictionary<string, object>? metadata = null)
        {
            try
            {
                var sessionActivity = new SessionActivity
                {
                    SessionId = sessionId,
                    Activity = activity,
                    Resource = resource,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                    RiskLevel = "Low"
                };

                _context.SessionActivities.Add(sessionActivity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging session activity for session {SessionId}", sessionId);
            }
        }

        public async Task<bool> DetectSuspiciousActivityAsync(string sessionId, string ipAddress, string userAgent)
        {
            try
            {
                var session = await _context.UserSessions.FindAsync(sessionId);
                if (session == null)
                    return false;

                // Check for IP address changes
                if (session.IpAddress != ipAddress)
                    return true;

                // Check for significant user agent changes (simplified check)
                if (!string.IsNullOrEmpty(session.UserAgent) && !session.UserAgent.Equals(userAgent, StringComparison.OrdinalIgnoreCase))
                {
                    // Allow minor version differences but flag major changes
                    var originalBrowser = ExtractBrowserInfo(session.UserAgent);
                    var currentBrowser = ExtractBrowserInfo(userAgent);
                    
                    if (originalBrowser != currentBrowser)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting suspicious activity for session {SessionId}", sessionId);
                return false;
            }
        }

        private string ExtractBrowserInfo(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            userAgent = userAgent.ToLower();
            
            if (userAgent.Contains("chrome"))
                return "Chrome";
            if (userAgent.Contains("firefox"))
                return "Firefox";
            if (userAgent.Contains("safari") && !userAgent.Contains("chrome"))
                return "Safari";
            if (userAgent.Contains("edge"))
                return "Edge";
            
            return "Other";
        }

        // Placeholder implementations for remaining interface methods
        public async Task<int> CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _context.UserSessions
                .Where(s => s.ExpiresAt < DateTime.UtcNow && s.IsActive)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                session.IsRevoked = true;
                session.RevocationReason = "Session expired";
                session.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return expiredSessions.Count;
        }

        public async Task<string> GenerateDeviceFingerprintAsync(DeviceFingerprintDto fingerprintData, int userId)
        {
            // Simple fingerprint generation (can be enhanced with more sophisticated algorithms)
            var fingerprintString = $"{fingerprintData.UserAgent}|{fingerprintData.ScreenResolution}|{fingerprintData.Timezone}|{fingerprintData.Language}|{fingerprintData.Platform}";
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprintString));
            var fingerprint = Convert.ToHexString(hashBytes).ToLower();

            // Store fingerprint in database
            var existingFingerprint = await _context.DeviceFingerprints
                .FirstOrDefaultAsync(df => df.Fingerprint == fingerprint && df.UserId == userId);

            if (existingFingerprint == null)
            {
                var deviceFingerprint = new DeviceFingerprint
                {
                    Fingerprint = fingerprint,
                    UserId = userId,
                    UserAgent = fingerprintData.UserAgent,
                    ScreenResolution = fingerprintData.ScreenResolution,
                    Timezone = fingerprintData.Timezone,
                    Language = fingerprintData.Language,
                    Platform = fingerprintData.Platform,
                    Plugins = JsonSerializer.Serialize(fingerprintData.Plugins),
                    CookiesEnabled = fingerprintData.CookiesEnabled,
                    JavaEnabled = fingerprintData.JavaEnabled,
                    ColorDepth = fingerprintData.ColorDepth,
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };

                _context.DeviceFingerprints.Add(deviceFingerprint);
                await _context.SaveChangesAsync();
            }
            else
            {
                existingFingerprint.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return fingerprint;
        }

        public async Task<bool> VerifyDeviceFingerprintAsync(string fingerprint, int userId)
        {
            return await _context.DeviceFingerprints
                .AnyAsync(df => df.Fingerprint == fingerprint && df.UserId == userId && !df.IsBlocked);
        }

        public async Task<bool> TrustDeviceAsync(string fingerprint, int userId)
        {
            var device = await _context.DeviceFingerprints
                .FirstOrDefaultAsync(df => df.Fingerprint == fingerprint && df.UserId == userId);

            if (device != null)
            {
                device.IsTrusted = true;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> BlockDeviceAsync(string fingerprint, int userId, string reason)
        {
            var device = await _context.DeviceFingerprints
                .FirstOrDefaultAsync(df => df.Fingerprint == fingerprint && df.UserId == userId);

            if (device != null)
            {
                device.IsBlocked = true;
                await _context.SaveChangesAsync();

                // Terminate all sessions from this device
                var deviceSessions = await _context.UserSessions
                    .Where(s => s.DeviceFingerprint == fingerprint && s.UserId == userId && s.IsActive)
                    .ToListAsync();

                foreach (var session in deviceSessions)
                {
                    await TerminateSessionAsync(session.SessionId, $"Device blocked: {reason}");
                }

                return true;
            }

            return false;
        }

        public async Task<SessionConfigurationDto> GetUserSessionConfigAsync(int userId)
        {
            var config = await _context.SessionConfigurations
                .FirstOrDefaultAsync(sc => sc.UserId == userId);

            if (config == null)
            {
                // Return default configuration
                return new SessionConfigurationDto
                {
                    MaxConcurrentSessions = 3,
                    SessionTimeoutMinutes = 480,
                    ExtendedSessionTimeoutMinutes = 1440,
                    IdleTimeoutMinutes = 30,
                    RequireDeviceVerification = false,
                    EnableConcurrentSessionControl = true,
                    LogAllSessionActivity = true
                };
            }

            return new SessionConfigurationDto
            {
                MaxConcurrentSessions = config.MaxConcurrentSessions,
                SessionTimeoutMinutes = config.SessionTimeoutMinutes,
                ExtendedSessionTimeoutMinutes = config.ExtendedSessionTimeoutMinutes,
                IdleTimeoutMinutes = config.IdleTimeoutMinutes,
                RequireDeviceVerification = config.RequireDeviceVerification,
                EnableConcurrentSessionControl = config.EnableConcurrentSessionControl,
                LogAllSessionActivity = config.LogAllSessionActivity
            };
        }

        public async Task<bool> UpdateUserSessionConfigAsync(int userId, SessionConfigurationDto config)
        {
            try
            {
                var existingConfig = await _context.SessionConfigurations
                    .FirstOrDefaultAsync(sc => sc.UserId == userId);

                if (existingConfig == null)
                {
                    var newConfig = new SessionConfiguration
                    {
                        UserId = userId,
                        MaxConcurrentSessions = config.MaxConcurrentSessions,
                        SessionTimeoutMinutes = config.SessionTimeoutMinutes,
                        ExtendedSessionTimeoutMinutes = config.ExtendedSessionTimeoutMinutes,
                        IdleTimeoutMinutes = config.IdleTimeoutMinutes,
                        RequireDeviceVerification = config.RequireDeviceVerification,
                        EnableConcurrentSessionControl = config.EnableConcurrentSessionControl,
                        LogAllSessionActivity = config.LogAllSessionActivity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.SessionConfigurations.Add(newConfig);
                }
                else
                {
                    existingConfig.MaxConcurrentSessions = config.MaxConcurrentSessions;
                    existingConfig.SessionTimeoutMinutes = config.SessionTimeoutMinutes;
                    existingConfig.ExtendedSessionTimeoutMinutes = config.ExtendedSessionTimeoutMinutes;
                    existingConfig.IdleTimeoutMinutes = config.IdleTimeoutMinutes;
                    existingConfig.RequireDeviceVerification = config.RequireDeviceVerification;
                    existingConfig.EnableConcurrentSessionControl = config.EnableConcurrentSessionControl;
                    existingConfig.LogAllSessionActivity = config.LogAllSessionActivity;
                    existingConfig.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session configuration for user {UserId}", userId);
                return false;
            }
        }

        public async Task<int> ForceLogoutSuspiciousSessionsAsync()
        {
            var suspiciousSessions = await _context.UserSessions
                .Where(s => s.IsSuspicious && s.IsActive)
                .ToListAsync();

            var loggedOutCount = 0;
            foreach (var session in suspiciousSessions)
            {
                await TerminateSessionAsync(session.SessionId, "Suspicious activity detected");
                loggedOutCount++;
            }

            return loggedOutCount;
        }

        public async Task<List<SessionActivityDto>> GetSessionActivityAsync(string sessionId, int pageNumber = 1, int pageSize = 50)
        {
            var activities = await _context.SessionActivities
                .Where(sa => sa.SessionId == sessionId)
                .OrderByDescending(sa => sa.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return activities.Select(a => new SessionActivityDto
            {
                SessionId = a.SessionId,
                Activity = a.Activity,
                Resource = a.Resource,
                IpAddress = a.IpAddress,
                Timestamp = a.Timestamp,
                Metadata = string.IsNullOrEmpty(a.Metadata) ? 
                    new Dictionary<string, object>() : 
                    JsonSerializer.Deserialize<Dictionary<string, object>>(a.Metadata) ?? new Dictionary<string, object>()
            }).ToList();
        }

        public async Task<bool> ExtendSessionAsync(string sessionId)
        {
            try
            {
                var session = await _context.UserSessions.FindAsync(sessionId);
                if (session == null || !session.IsActive)
                    return false;

                var extensionTime = session.IsRememberMe ? 
                    TimeSpan.FromMinutes(1440) : // 24 hours for remember me
                    TimeSpan.FromMinutes(480);    // 8 hours for normal session

                session.ExpiresAt = DateTime.UtcNow.Add(extensionTime);
                await _context.SaveChangesAsync();

                await LogSessionActivityAsync(sessionId, "SESSION_EXTENDED", "Authentication", session.IpAddress);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> RequiresMfaReverificationAsync(string sessionId)
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null || !session.User.IsMfaEnabled)
                return false;

            // Require MFA reverification every 24 hours for sensitive operations
            if (!session.LastMfaVerification.HasValue)
                return true;

            return DateTime.UtcNow.Subtract(session.LastMfaVerification.Value).TotalHours > 24;
        }

        public async Task UpdateMfaVerificationAsync(string sessionId)
        {
            var session = await _context.UserSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.LastMfaVerification = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await LogSessionActivityAsync(sessionId, "MFA_REVERIFIED", "Authentication", session.IpAddress);
            }
        }
    }
}
