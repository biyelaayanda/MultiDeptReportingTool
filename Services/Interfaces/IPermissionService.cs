using Microsoft.EntityFrameworkCore;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Constants;

namespace MultiDeptReportingTool.Services.Interfaces
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(int userId, string permission, int? departmentId = null);
        Task<bool> HasPermissionAsync(string username, string permission, int? departmentId = null);
        Task<List<string>> GetUserPermissionsAsync(int userId, int? departmentId = null);
        Task<bool> GrantPermissionAsync(int userId, string permission, int grantedByUserId, DateTime? expiresAt = null);
        Task<bool> RevokePermissionAsync(int userId, string permission, int revokedByUserId);
        Task<bool> GrantRolePermissionAsync(string roleName, string permission, int grantedByUserId);
        Task<bool> RevokeRolePermissionAsync(string roleName, string permission, int revokedByUserId);
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<List<Role>> GetAllRolesAsync();
        Task<bool> AssignRoleToUserAsync(int userId, string roleName, int assignedByUserId);
        Task<bool> RemoveRoleFromUserAsync(int userId, string roleName, int removedByUserId);
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<bool> CreatePermissionAsync(string name, string resource, string action, string? description = null);
        Task<bool> CreateRoleAsync(string name, string? description = null);
    }
}
