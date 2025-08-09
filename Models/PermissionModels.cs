namespace MultiDeptReportingTool.Models
{
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemLevel { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        public virtual ICollection<DepartmentPermission> DepartmentPermissions { get; set; } = new List<DepartmentPermission>();
    }

    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<Users> Users { get; set; } = new List<Users>();
    }

    public class RolePermission
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public int? GrantedByUserId { get; set; }
        
        // Navigation properties
        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
        public virtual Users? GrantedByUser { get; set; }
    }

    public class UserPermission
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; } = true;
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public int? GrantedByUserId { get; set; }
        public DateTime? ExpiresAt { get; set; }
        
        // Navigation properties
        public virtual Users User { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
        public virtual Users? GrantedByUser { get; set; }
    }

    public class DepartmentPermission
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; } = true;
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public int? GrantedByUserId { get; set; }
        
        // Navigation properties
        public virtual Department Department { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
        public virtual Users? GrantedByUser { get; set; }
    }
}
