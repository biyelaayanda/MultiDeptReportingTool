using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Constants;
using MultiDeptReportingTool.Services.Interfaces;

namespace MultiDeptReportingTool.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogSecurityEventAsync(string action, string resource, int? userId = null, string? username = null, 
            bool isSuccess = true, string? failureReason = null, string? details = null, 
            string ipAddress = "", string? userAgent = null, int? departmentId = null, 
            string? sessionId = null, string severity = "Info")
        {
            try
            {
                var auditLog = new SecurityAuditLog
                {
                    UserId = userId,
                    Username = username,
                    Action = action,
                    Resource = resource,
                    Details = details,
                    IsSuccess = isSuccess,
                    FailureReason = failureReason,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    DepartmentId = departmentId,
                    SessionId = sessionId,
                    Severity = severity,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Check for suspicious activity if this was a failed action
                if (!isSuccess && !string.IsNullOrEmpty(ipAddress))
                {
                    await DetectSuspiciousActivityAsync(ipAddress, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {Action} for user {UserId}", action, userId);
            }
        }

        public async Task LogSystemEventAsync(string eventType, string source, string message, string? details = null, 
            string level = "Info", string? correlationId = null, string? userId = null, string? ipAddress = null)
        {
            try
            {
                var systemEvent = new SystemEvent
                {
                    EventType = eventType,
                    Source = source,
                    Message = message,
                    Details = details,
                    Level = level,
                    CorrelationId = correlationId,
                    UserId = userId,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                _context.SystemEvents.Add(systemEvent);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log system event: {EventType} from {Source}", eventType, source);
            }
        }

        public async Task CreateSecurityAlertAsync(string alertType, string title, string description, 
            string severity = "Medium", int? userId = null, string? ipAddress = null, string? metadata = null)
        {
            try
            {
                var alert = new SecurityAlert
                {
                    AlertType = alertType,
                    Title = title,
                    Description = description,
                    Severity = severity,
                    UserId = userId,
                    IpAddress = ipAddress,
                    Metadata = metadata,
                    IsResolved = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SecurityAlerts.Add(alert);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Security alert created: {AlertType} - {Title} (Severity: {Severity})", 
                    alertType, title, severity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create security alert: {AlertType}", alertType);
            }
        }

        public async Task<List<SecurityAuditLog>> GetSecurityAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, 
            int? userId = null, string? action = null, string? ipAddress = null, int pageNumber = 1, int pageSize = 50)
        {
            var query = _context.SecurityAuditLogs.Include(s => s.User).Include(s => s.Department).AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.Timestamp <= endDate.Value);

            if (userId.HasValue)
                query = query.Where(s => s.UserId == userId.Value);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(s => s.Action.Contains(action));

            if (!string.IsNullOrEmpty(ipAddress))
                query = query.Where(s => s.IpAddress == ipAddress);

            return await query
                .OrderByDescending(s => s.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<SecurityAlert>> GetSecurityAlertsAsync(bool? isResolved = null, string? severity = null, 
            string? alertType = null, int pageNumber = 1, int pageSize = 50)
        {
            var query = _context.SecurityAlerts.Include(a => a.User).Include(a => a.ResolvedByUser).AsQueryable();

            if (isResolved.HasValue)
                query = query.Where(a => a.IsResolved == isResolved.Value);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(a => a.Severity == severity);

            if (!string.IsNullOrEmpty(alertType))
                query = query.Where(a => a.AlertType == alertType);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<SystemEvent>> GetSystemEventsAsync(DateTime? startDate = null, DateTime? endDate = null, 
            string? eventType = null, string? level = null, int pageNumber = 1, int pageSize = 50)
        {
            var query = _context.SystemEvents.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(eventType))
                query = query.Where(e => e.EventType == eventType);

            if (!string.IsNullOrEmpty(level))
                query = query.Where(e => e.Level == level);

            return await query
                .OrderByDescending(e => e.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> ResolveSecurityAlertAsync(int alertId, int resolvedByUserId, string? resolutionNotes = null)
        {
            try
            {
                var alert = await _context.SecurityAlerts.FindAsync(alertId);
                if (alert == null || alert.IsResolved)
                    return false;

                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
                alert.ResolvedByUserId = resolvedByUserId;
                alert.ResolutionNotes = resolutionNotes;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve security alert {AlertId}", alertId);
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetSecurityDashboardDataAsync()
        {
            var now = DateTime.UtcNow;
            var last24Hours = now.AddDays(-1);
            var last7Days = now.AddDays(-7);
            var last30Days = now.AddDays(-30);

            var dashboard = new Dictionary<string, object>();

            // Security alerts summary
            var totalAlerts = await _context.SecurityAlerts.CountAsync();
            var unresolvedAlerts = await _context.SecurityAlerts.CountAsync(a => !a.IsResolved);
            var criticalAlerts = await _context.SecurityAlerts.CountAsync(a => a.Severity == Severities.CRITICAL && !a.IsResolved);

            dashboard["TotalAlerts"] = totalAlerts;
            dashboard["UnresolvedAlerts"] = unresolvedAlerts;
            dashboard["CriticalAlerts"] = criticalAlerts;

            // Recent activity
            var failedLogins24h = await _context.SecurityAuditLogs
                .CountAsync(s => s.Action == AuditActions.LOGIN && !s.IsSuccess && s.Timestamp >= last24Hours);
            
            var successfulLogins24h = await _context.SecurityAuditLogs
                .CountAsync(s => s.Action == AuditActions.LOGIN && s.IsSuccess && s.Timestamp >= last24Hours);

            dashboard["FailedLogins24h"] = failedLogins24h;
            dashboard["SuccessfulLogins24h"] = successfulLogins24h;

            // Top suspicious IPs
            var suspiciousIps = await _context.SecurityAuditLogs
                .Where(s => !s.IsSuccess && s.Timestamp >= last7Days)
                .GroupBy(s => s.IpAddress)
                .Select(g => new { IpAddress = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            dashboard["SuspiciousIPs"] = suspiciousIps;

            // Recent alerts
            var recentAlerts = await _context.SecurityAlerts
                .Where(a => a.CreatedAt >= last7Days)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new
                {
                    a.Id,
                    a.AlertType,
                    a.Title,
                    a.Severity,
                    a.CreatedAt,
                    a.IsResolved
                })
                .ToListAsync();

            dashboard["RecentAlerts"] = recentAlerts;

            // Activity trends
            var dailyActivityTrend = await _context.SecurityAuditLogs
                .Where(s => s.Timestamp >= last30Days)
                .GroupBy(s => s.Timestamp.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalEvents = g.Count(),
                    FailedEvents = g.Count(x => !x.IsSuccess)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            dashboard["DailyActivityTrend"] = dailyActivityTrend;

            return dashboard;
        }

        public async Task DetectSuspiciousActivityAsync(string ipAddress, int? userId = null)
        {
            try
            {
                var now = DateTime.UtcNow;
                var last1Hour = now.AddHours(-1);
                var last24Hours = now.AddDays(-1);

                // Check for multiple failed logins from same IP in last hour
                var failedLoginsCount = await _context.SecurityAuditLogs
                    .CountAsync(s => s.IpAddress == ipAddress && 
                                    s.Action == AuditActions.LOGIN && 
                                    !s.IsSuccess && 
                                    s.Timestamp >= last1Hour);

                if (failedLoginsCount >= 5)
                {
                    await CreateSecurityAlertAsync(
                        AlertTypes.MULTIPLE_FAILED_LOGINS,
                        $"Multiple failed login attempts from IP {ipAddress}",
                        $"IP address {ipAddress} has attempted {failedLoginsCount} failed logins in the last hour.",
                        Severities.HIGH,
                        userId,
                        ipAddress,
                        $"{{\"failed_attempts\": {failedLoginsCount}, \"time_window\": \"1_hour\"}}"
                    );
                }

                // Check for suspicious activity patterns
                var totalFailedAttempts24h = await _context.SecurityAuditLogs
                    .CountAsync(s => s.IpAddress == ipAddress && 
                                    !s.IsSuccess && 
                                    s.Timestamp >= last24Hours);

                if (totalFailedAttempts24h >= 20)
                {
                    await CreateSecurityAlertAsync(
                        AlertTypes.SUSPICIOUS_ACTIVITY,
                        $"Suspicious activity detected from IP {ipAddress}",
                        $"IP address {ipAddress} has generated {totalFailedAttempts24h} failed attempts in the last 24 hours.",
                        Severities.MEDIUM,
                        userId,
                        ipAddress,
                        $"{{\"failed_attempts_24h\": {totalFailedAttempts24h}}}"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect suspicious activity for IP {IpAddress}", ipAddress);
            }
        }

        public async Task CleanupOldAuditLogsAsync(int retentionDays = 90)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                // Clean up old security audit logs
                var oldSecurityLogs = _context.SecurityAuditLogs.Where(s => s.Timestamp < cutoffDate);
                _context.SecurityAuditLogs.RemoveRange(oldSecurityLogs);

                // Clean up old system events
                var oldSystemEvents = _context.SystemEvents.Where(e => e.Timestamp < cutoffDate);
                _context.SystemEvents.RemoveRange(oldSystemEvents);

                // Clean up resolved security alerts older than retention period
                var oldResolvedAlerts = _context.SecurityAlerts
                    .Where(a => a.IsResolved && a.ResolvedAt < cutoffDate);
                _context.SecurityAlerts.RemoveRange(oldResolvedAlerts);

                var deletedRecords = await _context.SaveChangesAsync();
                
                await LogSystemEventAsync(
                    "AuditCleanup",
                    "AuditService",
                    $"Cleaned up {deletedRecords} old audit records",
                    $"Retention period: {retentionDays} days, Cutoff date: {cutoffDate:yyyy-MM-dd}",
                    "Info"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old audit logs");
            }
        }
    }
}
