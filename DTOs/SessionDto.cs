using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs
{
    public class SessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DeviceFingerprint { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty; // Mobile, Desktop, Tablet
        public string Location { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class ActiveSessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime LastAccessed { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCurrent { get; set; }
        public int DaysActive { get; set; }
    }

    public class SessionTerminationRequestDto
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;
        
        public string Reason { get; set; } = string.Empty;
    }

    public class BulkSessionTerminationRequestDto
    {
        [Required]
        public List<string> SessionIds { get; set; } = new List<string>();
        
        public string Reason { get; set; } = "Bulk termination requested by user";
    }

    public class DeviceFingerprintDto
    {
        public string Fingerprint { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string ScreenResolution { get; set; } = string.Empty;
        public string Timezone { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public List<string> Plugins { get; set; } = new List<string>();
        public bool CookiesEnabled { get; set; }
        public bool JavaEnabled { get; set; }
        public string ColorDepth { get; set; } = string.Empty;
    }

    public class SessionConfigurationDto
    {
        public int MaxConcurrentSessions { get; set; } = 3;
        public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours
        public int ExtendedSessionTimeoutMinutes { get; set; } = 1440; // 24 hours
        public int IdleTimeoutMinutes { get; set; } = 30;
        public bool RequireDeviceVerification { get; set; } = false;
        public bool EnableConcurrentSessionControl { get; set; } = true;
        public bool LogAllSessionActivity { get; set; } = true;
    }

    public class SessionActivityDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class SessionStatisticsDto
    {
        public int TotalActiveSessions { get; set; }
        public int TotalUsers { get; set; }
        public int SessionsCreatedToday { get; set; }
        public int SessionsTerminatedToday { get; set; }
        public int AverageSessionDurationMinutes { get; set; }
        public int SuspiciousSessionsDetected { get; set; }
        public List<DeviceTypeStatDto> DeviceTypeBreakdown { get; set; } = new List<DeviceTypeStatDto>();
        public List<LocationStatDto> LocationBreakdown { get; set; } = new List<LocationStatDto>();
    }

    public class DeviceTypeStatDto
    {
        public string DeviceType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class LocationStatDto
    {
        public string Location { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}
