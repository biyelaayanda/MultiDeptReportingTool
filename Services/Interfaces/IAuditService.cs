using MultiDeptReportingTool.Models;

namespace MultiDeptReportingTool.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogSecurityEventAsync(string action, string resource, int? userId = null, string? username = null, 
            bool isSuccess = true, string? failureReason = null, string? details = null, 
            string ipAddress = "", string? userAgent = null, int? departmentId = null, 
            string? sessionId = null, string severity = "Info");
            
        Task LogSystemEventAsync(string eventType, string source, string message, string? details = null, 
            string level = "Info", string? correlationId = null, string? userId = null, string? ipAddress = null);
            
        Task CreateSecurityAlertAsync(string alertType, string title, string description, 
            string severity = "Medium", int? userId = null, string? ipAddress = null, string? metadata = null);
            
        Task<List<SecurityAuditLog>> GetSecurityAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, 
            int? userId = null, string? action = null, string? ipAddress = null, int pageNumber = 1, int pageSize = 50);
            
        Task<List<SecurityAlert>> GetSecurityAlertsAsync(bool? isResolved = null, string? severity = null, 
            string? alertType = null, int pageNumber = 1, int pageSize = 50);
            
        Task<List<SystemEvent>> GetSystemEventsAsync(DateTime? startDate = null, DateTime? endDate = null, 
            string? eventType = null, string? level = null, int pageNumber = 1, int pageSize = 50);
            
        Task<bool> ResolveSecurityAlertAsync(int alertId, int resolvedByUserId, string? resolutionNotes = null);
        
        Task<Dictionary<string, object>> GetSecurityDashboardDataAsync();
        
        Task DetectSuspiciousActivityAsync(string ipAddress, int? userId = null);
        
        Task CleanupOldAuditLogsAsync(int retentionDays = 90);
    }
}
