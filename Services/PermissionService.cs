using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Constants;
using MultiDeptReportingTool.Services.Interfaces;

namespace MultiDeptReportingTool.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(int userId, string permission, int? departmentId = null)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.RoleEntity)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.IsActive)
                return false;

            // Check direct user permissions
            var hasDirectPermission = await _context.UserPermissions
                .Include(up => up.Permission)
                .AnyAsync(up => up.UserId == userId && 
                               up.Permission.Name == permission && 
                               up.IsGranted &&
                               (up.ExpiresAt == null || up.ExpiresAt > DateTime.UtcNow));

            if (hasDirectPermission)
                return true;

            // Check role-based permissions (using new RBAC system)
            if (user.RoleId.HasValue)
            {
                var hasRolePermission = await _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .AnyAsync(rp => rp.RoleId == user.RoleId.Value && 
                                   rp.Permission.Name == permission);

                if (hasRolePermission)
                    return true;
            }

            // Fallback: Check legacy role-based permissions (for users not yet migrated)
            if (!user.RoleId.HasValue && !string.IsNullOrEmpty(user.Role))
            {
                var hasLegacyRolePermission = await _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Include(rp => rp.Role)
                    .AnyAsync(rp => rp.Permission.Name == permission &&
                                   rp.Role.Name == user.Role);

                if (hasLegacyRolePermission)
                    return true;
            }

            // Check department-based permissions
            if (departmentId.HasValue || user.DepartmentId.HasValue)
            {
                var targetDepartmentId = departmentId ?? user.DepartmentId;
                var hasDepartmentPermission = await _context.DepartmentPermissions
                    .Include(dp => dp.Permission)
                    .AnyAsync(dp => dp.DepartmentId == targetDepartmentId &&
                                   dp.Permission.Name == permission &&
                                   dp.IsGranted);

                if (hasDepartmentPermission)
                    return true;
            }

            return false;
        }

        public async Task<bool> HasPermissionAsync(string username, string permission, int? departmentId = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return false;

            return await HasPermissionAsync(user.Id, permission, departmentId);
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId, int? departmentId = null)
        {
            var permissions = new HashSet<string>();

            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.IsActive)
                return new List<string>();

            // Get direct user permissions
            var directPermissions = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId && 
                            up.IsGranted &&
                            (up.ExpiresAt == null || up.ExpiresAt > DateTime.UtcNow))
                .Select(up => up.Permission.Name)
                .ToListAsync();

            foreach (var perm in directPermissions)
                permissions.Add(perm);

            // Get role-based permissions (using new RBAC system)
            if (user.RoleId.HasValue)
            {
                var rolePermissions = await _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == user.RoleId.Value)
                    .Select(rp => rp.Permission.Name)
                    .ToListAsync();

                foreach (var perm in rolePermissions)
                    permissions.Add(perm);
            }

            // Fallback: Get legacy role-based permissions (for users not yet migrated)
            if (!user.RoleId.HasValue && !string.IsNullOrEmpty(user.Role))
            {
                var legacyRolePermissions = await _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Include(rp => rp.Role)
                    .Where(rp => rp.Role.Name == user.Role)
                    .Select(rp => rp.Permission.Name)
                    .ToListAsync();

                foreach (var perm in legacyRolePermissions)
                    permissions.Add(perm);
            }

            // Get department-based permissions
            if (departmentId.HasValue || user.DepartmentId.HasValue)
            {
                var targetDepartmentId = departmentId ?? user.DepartmentId;
                var deptPermissions = await _context.DepartmentPermissions
                    .Include(dp => dp.Permission)
                    .Where(dp => dp.DepartmentId == targetDepartmentId && dp.IsGranted)
                    .Select(dp => dp.Permission.Name)
                    .ToListAsync();

                foreach (var perm in deptPermissions)
                    permissions.Add(perm);
            }

            return permissions.ToList();
        }

        public async Task<bool> GrantPermissionAsync(int userId, string permission, int grantedByUserId, DateTime? expiresAt = null)
        {
            var user = await _context.Users.FindAsync(userId);
            var permissionEntity = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permission);

            if (user == null || permissionEntity == null)
                return false;

            // Check if permission already exists
            var existingPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionEntity.Id);

            if (existingPermission != null)
            {
                existingPermission.IsGranted = true;
                existingPermission.GrantedAt = DateTime.UtcNow;
                existingPermission.GrantedByUserId = grantedByUserId;
                existingPermission.ExpiresAt = expiresAt;
            }
            else
            {
                var userPermission = new UserPermission
                {
                    UserId = userId,
                    PermissionId = permissionEntity.Id,
                    IsGranted = true,
                    GrantedAt = DateTime.UtcNow,
                    GrantedByUserId = grantedByUserId,
                    ExpiresAt = expiresAt
                };

                _context.UserPermissions.Add(userPermission);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokePermissionAsync(int userId, string permission, int revokedByUserId)
        {
            var permissionEntity = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permission);
            if (permissionEntity == null)
                return false;

            var userPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionEntity.Id);

            if (userPermission != null)
            {
                userPermission.IsGranted = false;
                userPermission.GrantedAt = DateTime.UtcNow;
                userPermission.GrantedByUserId = revokedByUserId;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> GrantRolePermissionAsync(string roleName, string permission, int grantedByUserId)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            var permissionEntity = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permission);

            if (role == null || permissionEntity == null)
                return false;

            var existingRolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permissionEntity.Id);

            if (existingRolePermission == null)
            {
                var rolePermission = new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionEntity.Id,
                    GrantedAt = DateTime.UtcNow,
                    GrantedByUserId = grantedByUserId
                };

                _context.RolePermissions.Add(rolePermission);
                await _context.SaveChangesAsync();
                return true;
            }

            return false; // Already exists
        }

        public async Task<bool> RevokeRolePermissionAsync(string roleName, string permission, int revokedByUserId)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            var permissionEntity = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permission);

            if (role == null || permissionEntity == null)
                return false;

            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permissionEntity.Id);

            if (rolePermission != null)
            {
                _context.RolePermissions.Remove(rolePermission);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            return await _context.Permissions.OrderBy(p => p.Resource).ThenBy(p => p.Action).ToListAsync();
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles.OrderBy(r => r.Name).ToListAsync();
        }

        public async Task<bool> AssignRoleToUserAsync(int userId, string roleName, int assignedByUserId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.Role = roleName;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRoleFromUserAsync(int userId, string roleName, int removedByUserId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != roleName)
                return false;

            user.Role = Roles.VIEWER; // Default role
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new List<string>();

            return new List<string> { user.Role };
        }

        public async Task<bool> CreatePermissionAsync(string name, string resource, string action, string? description = null)
        {
            var existingPermission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == name);
            if (existingPermission != null)
                return false;

            var permission = new Permission
            {
                Name = name,
                Resource = resource,
                Action = action,
                Description = description,
                IsSystemLevel = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateRoleAsync(string name, string? description = null)
        {
            var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
            if (existingRole != null)
                return false;

            var role = new Role
            {
                Name = name,
                Description = description,
                IsSystemRole = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
