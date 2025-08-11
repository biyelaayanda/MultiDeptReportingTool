using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.Models
{
    public class UserSession
    {
        [Key]
        public string SessionId { get; set; } = string.Empty;
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string DeviceFingerprint { get; set; } = string.Empty;
        
        [Required]
        public string IpAddress { get; set; } = string.Empty;
        
        public string UserAgent { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty; // Mobile, Desktop, Tablet
        public string DeviceName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty; // City, Country
        public string RefreshToken { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsRevoked { get; set; } = false;
        public string? RevocationReason { get; set; }
        public DateTime? RevokedAt { get; set; }
        
        // Security flags
        public bool IsSuspicious { get; set; } = false;
        public string? SuspiciousReason { get; set; }
        public int FailedAccessAttempts { get; set; } = 0;
        
        // Extended session properties
        public bool IsRememberMe { get; set; } = false;
        public bool RequiresMfaVerification { get; set; } = false;
        public DateTime? LastMfaVerification { get; set; }
        
        // Navigation properties
        public virtual Users User { get; set; } = null!;
    }

    public class SessionActivity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string SessionId { get; set; } = string.Empty;
        
        [Required]
        public string Activity { get; set; } = string.Empty;
        
        public string Resource { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Additional context data stored as JSON
        public string? Metadata { get; set; }
        
        // Risk assessment
        public string RiskLevel { get; set; } = "Low"; // Low, Medium, High
        public string? RiskReason { get; set; }
        
        // Navigation properties
        public virtual UserSession Session { get; set; } = null!;
    }

    public class DeviceFingerprint
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Fingerprint { get; set; } = string.Empty;
        
        [Required]
        public int UserId { get; set; }
        
        public string DeviceName { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string ScreenResolution { get; set; } = string.Empty;
        public string Timezone { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string? Plugins { get; set; } // Stored as JSON
        
        public bool CookiesEnabled { get; set; } = true;
        public bool JavaEnabled { get; set; } = false;
        public string ColorDepth { get; set; } = string.Empty;
        
        public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public bool IsTrusted { get; set; } = false;
        public bool IsBlocked { get; set; } = false;
        
        // Navigation properties
        public virtual Users User { get; set; } = null!;
        public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    }

    public class SessionConfiguration
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        public int MaxConcurrentSessions { get; set; } = 3;
        public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours
        public int ExtendedSessionTimeoutMinutes { get; set; } = 1440; // 24 hours
        public int IdleTimeoutMinutes { get; set; } = 30;
        
        public bool RequireDeviceVerification { get; set; } = false;
        public bool EnableConcurrentSessionControl { get; set; } = true;
        public bool LogAllSessionActivity { get; set; } = true;
        public bool AllowRememberMe { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Users User { get; set; } = null!;
    }
}
