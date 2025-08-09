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
        public string Role { get; set; } = string.Empty; // "Admin", "Executive", "DepartmentLead", "Staff" - Legacy field
        public int? RoleId { get; set; } // New RBAC Role foreign key
        public int? DepartmentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Department? Department { get; set; }
        public virtual Role? RoleEntity { get; set; } // New RBAC Role navigation property
        public virtual ICollection<Report> CreatedReports { get; set; } = new List<Report>();
        public virtual ICollection<Report> ApprovedReports { get; set; } = new List<Report>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        
        // Phase 2: RBAC Navigation Properties
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        public virtual ICollection<SecurityAuditLog> SecurityAuditLogs { get; set; } = new List<SecurityAuditLog>();
        public virtual ICollection<SecurityAlert> SecurityAlerts { get; set; } = new List<SecurityAlert>();
        public virtual ICollection<SecurityAlert> ResolvedSecurityAlerts { get; set; } = new List<SecurityAlert>();
    }
}
