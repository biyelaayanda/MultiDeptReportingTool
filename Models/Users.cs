using System;
using System.Collections.Generic;

namespace MultiDeptReportingTool.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Admin", "Executive", "DepartmentLead", "Staff"
        public int? DepartmentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Department? Department { get; set; }
        public virtual ICollection<Report> CreatedReports { get; set; } = new List<Report>();
        public virtual ICollection<Report> ApprovedReports { get; set; } = new List<Report>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
