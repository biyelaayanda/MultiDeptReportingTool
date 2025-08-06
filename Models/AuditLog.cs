using System;

namespace MultiDeptReportingTool.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty; // "Create", "Update", "Delete", "Login", etc.
        public string EntityName { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public int UserId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // Navigation property
        public virtual Users User { get; set; } = null!;
    }
}
