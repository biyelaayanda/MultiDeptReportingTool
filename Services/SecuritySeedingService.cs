using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Constants;
using Microsoft.EntityFrameworkCore;

namespace MultiDeptReportingTool.Services
{
    public class SecuritySeedingService
    {
        private readonly ApplicationDbContext _context;

        public SecuritySeedingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedSecurityDataAsync()
        {
            await SeedPermissionsAsync();
            await SeedRolesAsync();
            await SeedRolePermissionsAsync();
            await LinkUsersToRolesAsync(); // Link existing users to new role system
        }

        public async Task SeedBasicDataAsync()
        {
            // Simple basic role seeding for testing
            var adminRole = new Role 
            { 
                Name = "Admin", 
                Description = "System Administrator", 
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            };

            var exists = await _context.Roles.AnyAsync(r => r.Name == adminRole.Name);
            if (!exists)
            {
                _context.Roles.Add(adminRole);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SeedPermissionsAsync()
        {
            var permissions = new List<Permission>
            {
                // User Management
                new Permission { Name = Permissions.USER_VIEW, Resource = Resources.USER, Action = Actions.VIEW, Description = "View user information", IsSystemLevel = true },
                new Permission { Name = Permissions.USER_CREATE, Resource = Resources.USER, Action = Actions.CREATE, Description = "Create new users", IsSystemLevel = true },
                new Permission { Name = Permissions.USER_UPDATE, Resource = Resources.USER, Action = Actions.UPDATE, Description = "Update user information", IsSystemLevel = true },
                new Permission { Name = Permissions.USER_DELETE, Resource = Resources.USER, Action = Actions.DELETE, Description = "Delete users", IsSystemLevel = true },
                new Permission { Name = Permissions.USER_MANAGE_ROLES, Resource = Resources.USER, Action = Actions.MANAGE, Description = "Manage user roles", IsSystemLevel = true },

                // Department Management
                new Permission { Name = Permissions.DEPARTMENT_VIEW, Resource = Resources.DEPARTMENT, Action = Actions.VIEW, Description = "View department information", IsSystemLevel = false },
                new Permission { Name = Permissions.DEPARTMENT_CREATE, Resource = Resources.DEPARTMENT, Action = Actions.CREATE, Description = "Create new departments", IsSystemLevel = true },
                new Permission { Name = Permissions.DEPARTMENT_UPDATE, Resource = Resources.DEPARTMENT, Action = Actions.UPDATE, Description = "Update department information", IsSystemLevel = true },
                new Permission { Name = Permissions.DEPARTMENT_DELETE, Resource = Resources.DEPARTMENT, Action = Actions.DELETE, Description = "Delete departments", IsSystemLevel = true },
                new Permission { Name = Permissions.DEPARTMENT_VIEW_ALL, Resource = Resources.DEPARTMENT, Action = Actions.VIEW, Description = "View all departments", IsSystemLevel = true },

                // Report Management
                new Permission { Name = Permissions.REPORT_VIEW, Resource = Resources.REPORT, Action = Actions.VIEW, Description = "View reports", IsSystemLevel = false },
                new Permission { Name = Permissions.REPORT_CREATE, Resource = Resources.REPORT, Action = Actions.CREATE, Description = "Create new reports", IsSystemLevel = false },
                new Permission { Name = Permissions.REPORT_UPDATE, Resource = Resources.REPORT, Action = Actions.UPDATE, Description = "Update reports", IsSystemLevel = false },
                new Permission { Name = Permissions.REPORT_DELETE, Resource = Resources.REPORT, Action = Actions.DELETE, Description = "Delete reports", IsSystemLevel = false },
                new Permission { Name = Permissions.REPORT_APPROVE, Resource = Resources.REPORT, Action = Actions.APPROVE, Description = "Approve reports", IsSystemLevel = false },
                new Permission { Name = Permissions.REPORT_VIEW_ALL, Resource = Resources.REPORT, Action = Actions.VIEW, Description = "View all reports across departments", IsSystemLevel = true },
                new Permission { Name = Permissions.REPORT_EXPORT, Resource = Resources.REPORT, Action = Actions.EXPORT, Description = "Export reports", IsSystemLevel = false },

                // Analytics
                new Permission { Name = Permissions.ANALYTICS_VIEW, Resource = Resources.ANALYTICS, Action = Actions.VIEW, Description = "View analytics and insights", IsSystemLevel = false },
                new Permission { Name = Permissions.ANALYTICS_VIEW_ALL, Resource = Resources.ANALYTICS, Action = Actions.VIEW, Description = "View analytics across all departments", IsSystemLevel = true },
                new Permission { Name = Permissions.ANALYTICS_EXPORT, Resource = Resources.ANALYTICS, Action = Actions.EXPORT, Description = "Export analytics data", IsSystemLevel = false },

                // System Administration
                new Permission { Name = Permissions.SYSTEM_ADMIN, Resource = Resources.SYSTEM, Action = Actions.ADMIN, Description = "Full system administration", IsSystemLevel = true },
                new Permission { Name = Permissions.SYSTEM_AUDIT_VIEW, Resource = Resources.SYSTEM, Action = Actions.VIEW, Description = "View system audit logs", IsSystemLevel = true },
                new Permission { Name = Permissions.SYSTEM_SETTINGS, Resource = Resources.SYSTEM, Action = Actions.MANAGE, Description = "Manage system settings", IsSystemLevel = true },
                new Permission { Name = Permissions.SYSTEM_BACKUP, Resource = Resources.SYSTEM, Action = Actions.MANAGE, Description = "Manage system backups", IsSystemLevel = true },

                // Security
                new Permission { Name = Permissions.SECURITY_AUDIT_VIEW, Resource = Resources.SECURITY, Action = Actions.VIEW, Description = "View security audit logs", IsSystemLevel = true },
                new Permission { Name = Permissions.SECURITY_ALERTS_VIEW, Resource = Resources.SECURITY, Action = Actions.VIEW, Description = "View security alerts", IsSystemLevel = true },
                new Permission { Name = Permissions.SECURITY_ALERTS_MANAGE, Resource = Resources.SECURITY, Action = Actions.MANAGE, Description = "Manage security alerts", IsSystemLevel = true },
                new Permission { Name = Permissions.SECURITY_PERMISSIONS_MANAGE, Resource = Resources.SECURITY, Action = Actions.MANAGE, Description = "Manage permissions and roles", IsSystemLevel = true },

                // Export Operations
                new Permission { Name = Permissions.EXPORT_CREATE, Resource = Resources.EXPORT, Action = Actions.CREATE, Description = "Create export jobs", IsSystemLevel = false },
                new Permission { Name = Permissions.EXPORT_VIEW, Resource = Resources.EXPORT, Action = Actions.VIEW, Description = "View export history", IsSystemLevel = false },
                new Permission { Name = Permissions.EXPORT_DELETE, Resource = Resources.EXPORT, Action = Actions.DELETE, Description = "Delete export files", IsSystemLevel = false },
                new Permission { Name = Permissions.EXPORT_SCHEDULE, Resource = Resources.EXPORT, Action = Actions.MANAGE, Description = "Schedule automated exports", IsSystemLevel = false }
            };

            foreach (var permission in permissions)
            {
                var exists = await _context.Permissions.AnyAsync(p => p.Name == permission.Name);
                if (!exists)
                {
                    _context.Permissions.Add(permission);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roles = new List<Role>
            {
                new Role { Name = Roles.ADMIN, Description = "System administrator with full access", IsSystemRole = true },
                new Role { Name = Roles.EXECUTIVE, Description = "Executive level access across all departments", IsSystemRole = true },
                new Role { Name = Roles.DEPARTMENT_LEAD, Description = "Department leadership with management capabilities", IsSystemRole = true },
                new Role { Name = Roles.STAFF, Description = "Standard staff member with basic department access", IsSystemRole = true },
                new Role { Name = Roles.VIEWER, Description = "Read-only access to assigned resources", IsSystemRole = true }
            };

            foreach (var role in roles)
            {
                var exists = await _context.Roles.AnyAsync(r => r.Name == role.Name);
                if (!exists)
                {
                    _context.Roles.Add(role);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task SeedRolePermissionsAsync()
        {
            // Admin role - full access
            await AssignPermissionsToRoleAsync(Roles.ADMIN, new[]
            {
                Permissions.USER_VIEW, Permissions.USER_CREATE, Permissions.USER_UPDATE, Permissions.USER_DELETE, Permissions.USER_MANAGE_ROLES,
                Permissions.DEPARTMENT_VIEW, Permissions.DEPARTMENT_CREATE, Permissions.DEPARTMENT_UPDATE, Permissions.DEPARTMENT_DELETE, Permissions.DEPARTMENT_VIEW_ALL,
                Permissions.REPORT_VIEW, Permissions.REPORT_CREATE, Permissions.REPORT_UPDATE, Permissions.REPORT_DELETE, Permissions.REPORT_APPROVE, Permissions.REPORT_VIEW_ALL, Permissions.REPORT_EXPORT,
                Permissions.ANALYTICS_VIEW, Permissions.ANALYTICS_VIEW_ALL, Permissions.ANALYTICS_EXPORT,
                Permissions.SYSTEM_ADMIN, Permissions.SYSTEM_AUDIT_VIEW, Permissions.SYSTEM_SETTINGS, Permissions.SYSTEM_BACKUP,
                Permissions.SECURITY_AUDIT_VIEW, Permissions.SECURITY_ALERTS_VIEW, Permissions.SECURITY_ALERTS_MANAGE, Permissions.SECURITY_PERMISSIONS_MANAGE,
                Permissions.EXPORT_CREATE, Permissions.EXPORT_VIEW, Permissions.EXPORT_DELETE, Permissions.EXPORT_SCHEDULE
            });

            // Executive role - cross-department access
            await AssignPermissionsToRoleAsync(Roles.EXECUTIVE, new[]
            {
                Permissions.USER_VIEW,
                Permissions.DEPARTMENT_VIEW_ALL,
                Permissions.REPORT_VIEW_ALL, Permissions.REPORT_APPROVE, Permissions.REPORT_EXPORT,
                Permissions.ANALYTICS_VIEW_ALL, Permissions.ANALYTICS_EXPORT,
                Permissions.SECURITY_AUDIT_VIEW,
                Permissions.EXPORT_CREATE, Permissions.EXPORT_VIEW, Permissions.EXPORT_SCHEDULE
            });

            // Department Lead role - department management
            await AssignPermissionsToRoleAsync(Roles.DEPARTMENT_LEAD, new[]
            {
                Permissions.USER_VIEW,
                Permissions.DEPARTMENT_VIEW,
                Permissions.REPORT_VIEW, Permissions.REPORT_CREATE, Permissions.REPORT_UPDATE, Permissions.REPORT_DELETE, Permissions.REPORT_APPROVE, Permissions.REPORT_EXPORT,
                Permissions.ANALYTICS_VIEW, Permissions.ANALYTICS_EXPORT,
                Permissions.EXPORT_CREATE, Permissions.EXPORT_VIEW, Permissions.EXPORT_DELETE, Permissions.EXPORT_SCHEDULE
            });

            // Staff role - basic department access
            await AssignPermissionsToRoleAsync(Roles.STAFF, new[]
            {
                Permissions.DEPARTMENT_VIEW,
                Permissions.REPORT_VIEW, Permissions.REPORT_CREATE, Permissions.REPORT_UPDATE, Permissions.REPORT_EXPORT,
                Permissions.ANALYTICS_VIEW,
                Permissions.EXPORT_CREATE, Permissions.EXPORT_VIEW
            });

            // Viewer role - read-only access
            await AssignPermissionsToRoleAsync(Roles.VIEWER, new[]
            {
                Permissions.DEPARTMENT_VIEW,
                Permissions.REPORT_VIEW,
                Permissions.ANALYTICS_VIEW,
                Permissions.EXPORT_VIEW
            });
        }

        private async Task AssignPermissionsToRoleAsync(string roleName, string[] permissionNames)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) return;

            foreach (var permissionName in permissionNames)
            {
                var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permissionName);
                if (permission == null) continue;

                var exists = await _context.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);
                if (!exists)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                        GrantedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task LinkUsersToRolesAsync()
        {
            // Link existing users to new Role entities based on their Role string property
            var users = await _context.Users.Where(u => u.RoleId == null).ToListAsync();
            
            foreach (var user in users)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == user.Role);
                if (role != null)
                {
                    user.RoleId = role.Id;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
