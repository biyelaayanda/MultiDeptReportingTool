namespace MultiDeptReportingTool.Models
{
    public class SecurityAuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string? Details { get; set; }
        public bool IsSuccess { get; set; }
        public string? FailureReason { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public int? DepartmentId { get; set; }
        public string? SessionId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical
        
        // Navigation properties
        public virtual Users? User { get; set; }
        public virtual Department? Department { get; set; }
    }

    public class SecurityAlert
    {
        public int Id { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
        public int? UserId { get; set; }
        public string? IpAddress { get; set; }
        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }
        public string? ResolutionNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Metadata { get; set; } // JSON for additional data
        
        // Navigation properties
        public virtual Users? User { get; set; }
        public virtual Users? ResolvedByUser { get; set; }
    }

    public class SystemEvent
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string Level { get; set; } = "Info"; // Debug, Info, Warning, Error, Fatal
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? CorrelationId { get; set; }
        public string? UserId { get; set; }
        public string? IpAddress { get; set; }
    }
}
